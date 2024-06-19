using AspireRunner.Core.Extensions;
using AspireRunner.Core.Helpers;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text;

namespace AspireRunner.Core;

public partial class AspireDashboard
{
    private StringBuilder? _lastError;
    private DateTimeOffset? _lastErrorTime;

    private Process? _dashboardProcess;
    private CommandTask<CommandResult>? _dashboardCommand;

    private readonly string _dllPath;
    private readonly string _runnerFolder;
    private readonly DotnetCli _dotnetCli;
    private readonly ILogger<AspireDashboard> _logger;
    private readonly IReadOnlyDictionary<string, string?> _environmentVariables;

    public Version Version { get; private set; }

    public AspireDashboardOptions Options { get; private set; }

    /// <summary>
    /// Triggered when the Aspire Dashboard has started and the UI is ready.
    /// <br/>
    /// The dashboard URL (including the browser token) is passed to the event handler.
    /// </summary>
    public event Action<string>? DashboardStarted;

    /// <summary>
    /// Whether the Aspire Dashboard process has encountered any errors.
    /// </summary>
    public bool HasErrors { get; private set; }

    /// <summary>
    /// Whether the Aspire Dashboard process is currently running.
    /// </summary>
    public bool IsRunning => _dashboardCommand?.ProcessId > 0 && _dashboardProcess?.HasExited is false;

    internal AspireDashboard(DotnetCli dotnetCli, Version version, string dllPath, AspireDashboardOptions options, ILogger<AspireDashboard> logger)
    {
        Version = version;
        Options = options;

        _logger = logger;
        _dllPath = dllPath;
        _dotnetCli = dotnetCli;
        _environmentVariables = options.ToEnvironmentVariables();
        _runnerFolder = Path.Combine(_dotnetCli.DataPath, DataFolder);
    }

    public void Start()
    {
        if (IsRunning)
        {
            return;
        }

        switch (Options.Runner.SingleInstanceHandling)
        {
            case SingleInstanceHandling.ReplaceExisting:
            {
                TryGetRunningInstance()?.Kill(true);
                break;
            }
            case SingleInstanceHandling.WarnAndExit:
            {
                var runningInstance = TryGetRunningInstance();
                if (runningInstance != null)
                {
                    _logger.LogWarning("Another instance of the Aspire Dashboard is already running, Process Id = {PID}", runningInstance.Id);
                    return;
                }

                break;
            }
        }

        try
        {
            _dashboardCommand = _dotnetCli.RunAsync(["exec", Path.Combine(_dllPath, DllName)], _dllPath, _environmentVariables, OutputHandler, ErrorHandler);
            _dashboardProcess = Process.GetProcessById(_dashboardCommand.ProcessId);
        }
        catch (Exception e)
        {
            _logger.LogError("Failed to start the Aspire Dashboard: {Message}", e.Message);
        }

        PersistInstance();
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
            _logger.LogInformation("Stopping the Aspire Dashboard...");
            _dashboardProcess?.Kill(true);
        }
        catch (InvalidOperationException)
        {
            _logger.LogWarning("The Aspire Dashboard has already been stopped");
        }

        _dashboardCommand = null;
        _dashboardProcess = null;
    }

    /// <summary>
    /// Waits for the Aspire Dashboard process to exit asynchronously or until the cancellation token is triggered.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token to monitor for cancellation requests.</param>
    /// <exception cref="TaskCanceledException"> thrown when the cancellation token is triggered.</exception>
    public Task WaitForExitAsync(CancellationToken cancellationToken = default)
    {
        if (!IsRunning || cancellationToken.IsCancellationRequested)
        {
            return Task.CompletedTask;
        }

        if (cancellationToken == default || cancellationToken == CancellationToken.None)
        {
            return _dashboardCommand!.Task;
        }

        return Task.WhenAny(_dashboardCommand!.Task, Task.Delay(Timeout.Infinite, cancellationToken));
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

        if (LaunchUrlRegex.Match(output) is { Success: true } match)
        {
            var url = match.Groups["url"].Value;
            if (Options.Runner.LaunchBrowser)
            {
                _ = LaunchBrowserAsync(url);
            }

            DashboardStarted?.Invoke(url);
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

            return Cli.Wrap(urlOpener.Value.Executable)
                .WithArguments(urlOpener.Value.Arguments)
                .ExecuteAsync();
        }
        catch
        {
            _logger.LogWarning("Failed to launch the browser");
        }

        return Task.CompletedTask;
    }

    private void PersistInstance()
    {
        File.WriteAllText(Path.Combine(_runnerFolder, InstanceFile), _dashboardProcess!.Id.ToString());
    }

    private Process? TryGetRunningInstance()
    {
        var instanceFile = Path.Combine(_runnerFolder, InstanceFile);
        if (!File.Exists(instanceFile) || !int.TryParse(File.ReadAllText(instanceFile), out var pid))
        {
            return null;
        }

        try
        {
            return Process.GetProcessById(pid) is { ProcessName: "dotnet" } p ? p : null;
        }
        catch
        {
            return null;
        }
    }
}