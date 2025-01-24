using AspireRunner.Core.Extensions;
using AspireRunner.Core.Helpers;
using Medallion.Threading.FileSystem;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text;

namespace AspireRunner.Core;

public partial class AspireDashboard
{
    private StringBuilder? _lastError;
    private DateTimeOffset? _lastErrorTime;
    private Process? _dashboardProcess;
    private bool _stopRequested;

    private readonly string _dllPath;
    private readonly string _runnerFolder;
    private readonly DotnetCli _dotnetCli;
    private readonly ILogger<AspireDashboard> _logger;
    private readonly FileDistributedLock _instanceLock;
    private readonly IDictionary<string, string?> _environmentVariables;

    public Version Version { get; private set; }

    public AspireDashboardOptions Options { get; }

    /// <summary>
    /// Triggered when the Aspire Dashboard has started and the UI is ready.
    /// <br/>
    /// The dashboard URL (including the browser token) is passed to the event handler.
    /// </summary>
    public event Action<string>? DashboardStarted;

    /// <summary>
    /// Triggered when the OTLP endpoint is ready to receive telemetry data.
    /// <br/>
    /// The OTLP endpoint URL and protocol are passed to the event handler.
    /// </summary>
    public event Action<(string Url, string Protocol)>? OtlpEndpointReady;

    public bool HasErrors { get; private set; }

    public bool IsRunning => _dashboardProcess.IsRunning();

    internal AspireDashboard(DotnetCli dotnetCli, Version version, string dllPath, AspireDashboardOptions options, ILogger<AspireDashboard> logger)
    {
        Version = version;
        Options = options;

        _logger = logger;
        _dllPath = dllPath;
        _dotnetCli = dotnetCli;
        _environmentVariables = options.ToEnvironmentVariables();
        _runnerFolder = Path.Combine(_dotnetCli.DataPath, DataFolder);
        _instanceLock = new FileDistributedLock(new DirectoryInfo(_runnerFolder), InstanceLock);
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
                _logger.LogWarning("Failed to start the Aspire Dashboard, retrying in {RetryDelay} seconds...", Options.Runner.RunRetryDelay);
                await Task.Delay(retryDelay, cancellationToken);
            }

            if (await TryStartProcessAsync(cancellationToken))
            {
                return;
            }
        } while (retryCount++ < Options.Runner.RunRetryCount && !cancellationToken.IsCancellationRequested);
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
                    instance.Dashboard!.Kill(true);
                }
                else if (Options.Runner.SingleInstanceHandling is SingleInstanceHandling.WarnAndExit)
                {
                    //  TODO: Listen for it to exit and then start the new instance
                    _logger.LogWarning("Another instance of the Aspire Dashboard is already running, Process Id = {PID}", instance.Dashboard!.Id);
                    return false;
                }
            }

            _dashboardProcess = ProcessHelper.Run(_dotnetCli.Executable, ["exec", Path.Combine(_dllPath, DllName)], _environmentVariables, _dllPath, OutputHandler, ErrorHandler);
            if (_dashboardProcess is null)
            {
                _logger.LogError("Failed to start the Aspire Dashboard");
                return false;
            }

            if (Options.Runner.RestartOnFailure)
            {
                DashboardStarted += RegisterProcessExitHandler;
            }

            PersistInstance();
            return true;
        }
        catch (Exception e)
        {
            _logger.LogError("Failed to start the Aspire Dashboard: {Message}", e.Message);
            return false;
        }
    }

    /// <summary>
    /// Stops the Aspire Dashboard process.
    /// </summary>
    public void Stop()
    {
        if (!IsRunning)
        {
            return;
        }

        try
        {
            _stopRequested = true;
            _logger.LogInformation("Stopping the Aspire Dashboard...");
            _dashboardProcess?.Kill(true);
        }
        catch (InvalidOperationException)
        {
            _logger.LogWarning("The Aspire Dashboard has already been stopped");
        }

        _dashboardProcess = null;
    }

    /// <summary>
    /// Returns a task that completes when the Aspire Dashboard process exits or when the cancellation token is triggered.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token to monitor for cancellation requests.</param>
    public Task WaitForExitAsync(CancellationToken cancellationToken = default)
    {
        if (!IsRunning || cancellationToken.IsCancellationRequested)
        {
            return Task.CompletedTask;
        }

        return _dashboardProcess!.WaitForExitAsync(cancellationToken);
    }

    private void RegisterProcessExitHandler(string _)
    {
        DashboardStarted -= RegisterProcessExitHandler;
        if (_dashboardProcess == null)
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

            _logger.LogWarning("Aspire dashboard exited unexpectedly, Attempting to restart...");
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

        if (DashboardLaunchUrlRegex.Match(output) is { Success: true } match)
        {
            var url = match.Groups["url"].Value;
            if (Options.Runner.LaunchBrowser)
            {
                _ = LaunchBrowserAsync(url);
            }

            DashboardStarted?.Invoke(url);
        }

        if (OtlpEndpointRegex.Match(output) is { Success: true } otlpMatch)
        {
            var url = otlpMatch.Groups["url"].Value;
            var protocol = otlpMatch.Groups["protocol"].Value;

            OtlpEndpointReady?.Invoke((url, protocol));
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
        _logger.LogError("{AspireError}", _lastError.ToString());

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

            _logger.LogError("{AspireError}", _lastError.ToString());
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
                _logger.LogWarning("Failed to find a suitable URL opener");
                return Task.CompletedTask;
            }

            return ProcessHelper.Run(urlOpener.Value.Executable, urlOpener.Value.Arguments)?.WaitForExitAsync()
                ?? throw new ApplicationException("Failed to launch the browser");
        }
        catch
        {
            _logger.LogWarning("Failed to launch the browser");
        }

        return Task.CompletedTask;
    }

    private void PersistInstance()
    {
        if (!IsRunning)
        {
            return;
        }

        var instanceFilePath = Path.Combine(_runnerFolder, InstanceFile);
        File.WriteAllText(instanceFilePath, $"{_dashboardProcess!.Id}:{Environment.ProcessId}");
    }

    private (Process? Dashboard, Process? Runner) TryGetRunningInstance()
    {
        var instanceFilePath = Path.Combine(_runnerFolder, InstanceFile);
        if (!File.Exists(instanceFilePath))
        {
            return default;
        }

        var instanceInfo = File.ReadAllText(instanceFilePath);
        if (string.IsNullOrWhiteSpace(instanceInfo))
        {
            return default;
        }

        var pids = instanceInfo.Split(':', 2);
        _ = int.TryParse(pids[0], out var dashboardPid);
        _ = int.TryParse(pids.ElementAtOrDefault(1), out var runnerPid);

        var runner = ProcessHelper.GetProcessOrDefault(runnerPid);
        var dashboard = ProcessHelper.GetProcessOrDefault(dashboardPid) is { ProcessName: "dotnet" } p ? p : null;

        return (dashboard, runner);
    }
}