using AspireRunner.Core.Extensions;
using Medallion.Threading.FileSystem;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text;

namespace AspireRunner.Core;

public partial class Dashboard : IDashboard
{
    private StringBuilder? _lastError;
    private DateTimeOffset? _lastErrorTime;
    private Process? _dashboardProcess;
    private bool _stopRequested;

    private readonly string _runnerPath;
    private readonly ILogger<Dashboard> _logger;
    private readonly FileDistributedLock _instanceLock;
    private readonly IDictionary<string, string?> _environmentVariables;

    public Version Version { get; }

    public DashboardOptions Options { get; }

    public string InstallationPath { get; }

    public string? Url { get; private set; }

    public IReadOnlyList<(string Url, string Protocol)>? OtlpEndpoints { get; private set; }

    public bool HasErrors { get; private set; }

    public bool IsRunning => _dashboardProcess.IsRunning();

    public int? Pid => _dashboardProcess?.Id;

    public event Action<string>? DashboardStarted;

    public event Action<(string Url, string Protocol)>? OtlpEndpointReady;

    internal Dashboard(Version version, string dllPath, DashboardOptions options, ILogger<Dashboard> logger)
    {
        Version = version;
        Options = options;
        InstallationPath = dllPath;

        _logger = logger;
        _runnerPath = GetRunnerPath();
        _environmentVariables = options.ToEnvironmentVariables();
        _instanceLock = new FileDistributedLock(new DirectoryInfo(_runnerPath), InstanceLock);
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (IsRunning || cancellationToken.IsCancellationRequested)
        {
            return;
        }

        var retryCount = 0;
        var retryDelay = TimeSpan.FromSeconds(Options.Runner.RunRetryDelay);

        do
        {
            if (retryCount > 0)
            {
                WarnFailedToStartDashboardWithRetry(Options.Runner.RunRetryDelay);
                await Task.Delay(retryDelay, cancellationToken);
            }

            if (await TryStartProcessAsync(cancellationToken))
            {
                return;
            }
        } while (retryCount++ < Options.Runner.RunRetryCount && !cancellationToken.IsCancellationRequested);
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (!IsRunning || cancellationToken.IsCancellationRequested)
        {
            return;
        }

        try
        {
            _stopRequested = true;
            await Task.Run(() => _dashboardProcess?.Kill(true), cancellationToken);
        }
        catch (InvalidOperationException)
        {
            WarnDashboardAlreadyStopped();
        }

        _dashboardProcess = null;
    }

    public Task WaitForExitAsync(CancellationToken cancellationToken = default)
    {
        if (!IsRunning || cancellationToken.IsCancellationRequested)
        {
            return Task.CompletedTask;
        }

        return _dashboardProcess!.WaitForExitAsync(cancellationToken);
    }

    private async Task<bool> TryStartProcessAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using var lockHandle = await _instanceLock.AcquireAsync(timeout: TimeSpan.FromSeconds(InstanceLockTimeout), cancellationToken: cancellationToken);
            var instance = TryGetRunningInstance();

            if (instance.Dashboard.IsRunning())
            {
                if (Options.Runner.SingleInstanceHandling is SingleInstanceHandling.ReplaceExisting || !instance.Runner.IsRunning())
                {
                    instance.Dashboard.Kill(true);
                }
                else if (Options.Runner.SingleInstanceHandling is SingleInstanceHandling.WarnAndExit)
                {
                    WarnExistingInstance(instance.Dashboard.Id);
                    return false;
                }
            }

            // Reset instance state
            Url = null;
            HasErrors = false;
            OtlpEndpoints = null;
            _lastError = null;
            _lastErrorTime = null;

            _dashboardProcess = ProcessHelper.Run(DotnetCli.Executable, ["exec", Path.Combine(InstallationPath, DllName)], _environmentVariables, InstallationPath, OutputHandler, ErrorHandler);
            if (_dashboardProcess is null)
            {
                LogFailedToStartDashboardProcess();
                return false;
            }

            if (Options.Runner.RestartOnFailure)
            {
                DashboardStarted += RegisterProcessExitHandler;
            }

            PersistInstance();
            return true;
        }
        catch (Exception ex)
        {
            LogFailedToStartDashboard(ex);
            return false;
        }
    }

    private void RegisterProcessExitHandler(string? _ = null)
    {
        DashboardStarted -= RegisterProcessExitHandler;
        if (!_dashboardProcess.IsRunning())
        {
            return;
        }

        _dashboardProcess.EnableRaisingEvents = true;
        _dashboardProcess.Exited += async (_, _) =>
        {
            if (_stopRequested)
            {
                return;
            }

            WarnDashboardExitedUnexpectedly();
            await StartAsync();
        };
    }

    private void OutputHandler(string output)
    {
        if (Options.Runner.PipeOutput)
        {
            _logger.LogInformation(output);
        }

        if (Options.Frontend.AuthMode is FrontendAuthMode.BrowserToken && output.Contains(DashboardStartedConsoleMessage, StringComparison.OrdinalIgnoreCase))
        {
            // Wait for the authentication token to be printed
            return;
        }

        if (DashboardLaunchUrlRegex().Match(output) is { Success: true } match)
        {
            Url = UrlHelper.ReplaceDefaultRoute(match.Groups["url"].Value);
            if (Options.Runner.LaunchBrowser)
            {
                _ = LaunchBrowserAsync(Url);
            }

            DashboardStarted?.Invoke(Url);
        }

        if (OtlpEndpointRegex().Match(output) is { Success: true } otlpMatch)
        {
            var endpoint = (UrlHelper.ReplaceDefaultRoute(otlpMatch.Groups["url"].Value), otlpMatch.Groups["protocol"].Value);
            var endpoints = (List<(string Url, string Protocol)>)(OtlpEndpoints ??= new List<(string Url, string Protocol)>());
            endpoints.Add(endpoint);

            OtlpEndpointReady?.Invoke(endpoint);
        }
    }

    private void ErrorHandler(string error)
    {
        // To avoid spamming the otel logs with partial errors, we need to combine the error lines into a single error message and then log it
        // This approach will combine the error lines as they're piped from the process, and then log them after a delay
        // If the error line starts with a space, it's considered a continuation of the previous error line
        // otherwise the previous error is logged and the new error will start to be collected

        HasErrors = true;
        _lastError ??= new StringBuilder();

        if (error.StartsWith(' ') || error.Length == 0)
        {
            _lastError.AppendLine(error);
            ResetErrorLogDelay();
            return;
        }

        // Log the previous error before collecting the new one
        LogDashboardError(_lastError.ToString());

        _lastError.Clear();
        _lastError.AppendLine(error);
        ResetErrorLogDelay();
    }

    private void ResetErrorLogDelay()
    {
        var currentTime = _lastErrorTime = DateTimeOffset.Now;
        Task.Delay(DefaultErrorLogDelay).ContinueWith(_ =>
        {
            if (_lastError is null or { Length: 0 } || _lastErrorTime == null || _lastErrorTime != currentTime)
            {
                return;
            }

            LogDashboardError(_lastError.ToString());
            _lastError.Clear();
        });
    }

    private Task LaunchBrowserAsync(string url)
    {
        try
        {
            var urlOpener = PlatformHelper.GetUrlOpener(url);
            if (urlOpener is null)
            {
                WarnFailedToFindUrlOpener();
                return Task.CompletedTask;
            }

            return ProcessHelper.Run(urlOpener.Value.Executable, urlOpener.Value.Arguments)?.WaitForExitAsync()
                ?? throw new ApplicationException("Failed to launch the browser");
        }
        catch
        {
            WarnFailedToLaunchBrowser();
        }

        return Task.CompletedTask;
    }

    private void PersistInstance()
    {
        if (!IsRunning)
        {
            return;
        }

        var instanceFilePath = Path.Combine(_runnerPath, InstanceFile);
        File.WriteAllText(instanceFilePath, $"{_dashboardProcess!.Id}:{Environment.ProcessId}");
    }
}