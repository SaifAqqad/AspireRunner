using AspireRunner.Core.Extensions;
using AspireRunner.Core.Helpers;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text;

namespace AspireRunner.Core;

public partial class AspireDashboard
{
    private readonly string _runnerFolder;
    private readonly string _nugetPackageName;

    private readonly AspireDashboardOptions _options;
    private readonly ILogger<AspireDashboard> _logger;

    private Process? _process;
    private StringBuilder? _lastError;
    private DateTimeOffset? _lastErrorTime;

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
    public bool IsRunning => _process?.HasExited is false;

    public AspireDashboard(DotnetCli dotnetCli, NugetHelper nugetHelper, AspireDashboardOptions options, ILogger<AspireDashboard> logger)
    {
        _logger = logger;
        _dotnetCli = dotnetCli;
        _options = options;
        _nugetHelper = nugetHelper;

        _runnerFolder = Path.Combine(_dotnetCli.DataPath, DataFolder);
        if (!Directory.Exists(_runnerFolder))
        {
            Directory.CreateDirectory(_runnerFolder);
        }

        _nugetPackageName = $"{SdkName}.{RuntimeIdentification.Rid}";
    }

    /// <summary>
    /// Starts the Aspire Dashboard process.
    /// </summary>
    /// <exception cref="ApplicationException">
    /// Thrown when the Aspire Dashboard is already running.
    /// </exception>
    public async ValueTask StartAsync()
    {
        if (_process != null)
        {
            throw new ApplicationException("The Aspire Dashboard is already running");
        }

        var installedRuntimes = _dotnetCli.GetInstalledRuntimes()
            .Where(r => r.Name is AspRuntimeName && r.Version >= MinimumRuntimeVersion)
            .Select(r => r.Version)
            .ToArray();

        _logger.LogTrace("Installed runtimes: {InstalledRuntimes}", string.Join(", ", installedRuntimes.Select(v => v.ToString())));
        if (installedRuntimes.Length == 0)
        {
            throw new ApplicationException($"The dashboard requires version '{MinimumRuntimeVersion}' or newer of the '{AspRuntimeName}' runtime");
        }

        var preferredVersion = Version.TryParse(_options.Runner.RuntimeVersion, out var rv) ? rv : null;
        if (preferredVersion != null && !installedRuntimes.Any(v => v.IsCompatibleWith(preferredVersion)))
        {
            _logger.LogWarning("The specified runtime version '{RuntimeVersion}' is either not installed or incompatible with the dashboard, falling back to the latest installed version", preferredVersion);
            preferredVersion = null;
        }

        var isInstalled = IsInstalled(out var isWorkload);
        if (!isInstalled)
        {
            if (!_options.Runner.AutoDownload)
            {
                throw new ApplicationException("The Aspire Dashboard is not installed");
            }

            _logger.LogWarning("The Aspire Dashboard is not installed, downloading the latest compatible version...");
            var (downloadSuccessful, downloadedVersion) = await TryDownloadAsync(preferredVersion, installedRuntimes);
            if (!downloadSuccessful)
            {
                throw new ApplicationException("Failed to download the Aspire Dashboard");
            }

            _logger.LogInformation("Successfully downloaded the Aspire Dashboard (version {Version})", downloadedVersion);
        }
        else if (!isWorkload && _options.Runner.AutoDownload)
        {
            // We are using the runner-managed dashboards, so we can try to update
            await TryUpdateAsync(preferredVersion);
        }

        switch (_options.Runner.SingleInstanceHandling)
        {
            case SingleInstanceHandling.ReplaceExisting:
            {
                TryGetRunningProcess()?.Kill(true);
                break;
            }
            case SingleInstanceHandling.WarnAndExit:
            {
                var runningInstance = TryGetRunningProcess();
                if (runningInstance != null)
                {
                    _logger.LogWarning("Another instance of the Aspire Dashboard is already running, Process Id = {PID}", runningInstance.Id);
                    return;
                }

                break;
            }
        }

        var installationInfo = GetInstallationInfo(isWorkload);
        if (installationInfo == null)
        {
            throw new ApplicationException("Failed to locate the Aspire Dashboard installation path");
        }

        var (version, path) = installationInfo.Value;
        _logger.LogTrace("Aspire Dashboard installation path: {AspirePath}, Workload = {Workload}", path, isWorkload);

        try
        {
            _logger.LogInformation("Starting Aspire Dashboard {Version}", version);
            _process = _dotnetCli.Run(["exec", Path.Combine(path, DllName)], path, _options.ToEnvironmentVariables(), OutputHandler, ErrorHandler);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to start the Aspire Dashboard");
            return;
        }

        PersistProcessId();
    }

    /// <summary>
    /// Checks if the Aspire Dashboard is installed.
    /// </summary>
    public bool IsInstalled() => IsInstalled(out _);

    /// <summary>
    /// Stops the Aspire Dashboard process.
    /// </summary>
    public void Stop()
    {
        if (_process == null)
        {
            return;
        }

        try
        {
            _logger.LogInformation("Stopping the Aspire Dashboard...");
            _process.Kill(true);
        }
        catch (InvalidOperationException)
        {
            _logger.LogWarning("The Aspire Dashboard has already been stopped");
        }

        _process = null;
    }

    /// <summary>
    /// Stops the Aspire Dashboard process asynchronously (using Task.Run).
    /// </summary>
    public async ValueTask StopAsync()
    {
        if (_process == null)
        {
            return;
        }

        await Task.Run(Stop);
    }

    /// <summary>
    /// Waits for the Aspire Dashboard process to exit.
    /// </summary>
    public void WaitForExit() => _process?.WaitForExit();

    /// <summary>
    /// Waits for the Aspire Dashboard process to exit asynchronously or until the cancellation token is triggered.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token to monitor for cancellation requests.</param>
    public async ValueTask WaitForExitAsync(CancellationToken cancellationToken = default)
    {
        if (_process == null || cancellationToken.IsCancellationRequested)
        {
            return;
        }

        await _process.WaitForExitAsync(cancellationToken);
    }

    private void PersistProcessId()
    {
        File.WriteAllText(Path.Combine(_runnerFolder, InstanceFile), _process!.Id.ToString());
    }

    private Process? TryGetRunningProcess()
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

    private void OutputHandler(string output)
    {
        if (_options.Runner.PipeOutput)
        {
            _logger.LogInformation(output);
        }

        if (_options.Frontend.AuthMode is FrontendAuthMode.BrowserToken && output.Contains(DashboardStartedConsoleMessage, StringComparison.OrdinalIgnoreCase))
        {
            // Wait for the authentication token to be printed
            return;
        }

        if (LaunchUrlRegex.Match(output) is { Success: true } match)
        {
            var url = match.Groups["url"].Value;
            if (_options.Runner.LaunchBrowser)
            {
                try
                {
                    LaunchBrowser(url);
                }
                catch
                {
                    _logger.LogWarning("Failed to launch the browser");
                }
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

    private static void LaunchBrowser(string url)
    {
        if (OperatingSystem.IsWindows())
        {
            Process.Start(new ProcessStartInfo
            {
                UseShellExecute = true,
                FileName = url
            });
        }
        else if (OperatingSystem.IsLinux())
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "xdg-open",
                UseShellExecute = true,
                Arguments = $"\"{url}\""
            });
        }
        else if (OperatingSystem.IsMacOS())
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "open",
                UseShellExecute = true,
                Arguments = $"\"{url}\""
            });
        }
    }
}