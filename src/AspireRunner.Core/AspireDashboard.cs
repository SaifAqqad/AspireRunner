using AspireRunner.Core.Extensions;
using AspireRunner.Core.Helpers;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text;

namespace AspireRunner.Core;

// TODO: rewrite the whole thing :/
public partial class AspireDashboard
{
    private string _runnerFolder = null!;
    private readonly string _nugetPackageName;

    private readonly DotnetCli _dotnetCli;
    private readonly NugetHelper _nugetHelper;
    private readonly AspireDashboardOptions _options;
    private readonly ILogger<AspireDashboard> _logger;

    private StringBuilder? _lastError;
    private DateTimeOffset? _lastErrorTime;

    private Process? _dashboardProcess;
    private CommandTask<CommandResult>? _dashboardCommand;

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
    public bool IsRunning => _dashboardCommand is { ProcessId: > 0 } && _dashboardProcess is { HasExited: false };

    public AspireDashboard(DotnetCli dotnetCli, NugetHelper nugetHelper, AspireDashboardOptions options, ILogger<AspireDashboard> logger)
    {
        _logger = logger;
        _dotnetCli = dotnetCli;
        _options = options;
        _nugetHelper = nugetHelper;
        _nugetPackageName = $"{SdkName}.{PlatformHelper.Rid}";
    }

    public async Task<bool> InitializeAsync()
    {
        if (!await _dotnetCli.InitializeAsync())
        {
            return false;
        }

        _runnerFolder = Path.Combine(_dotnetCli.DataPath, DataFolder);
        if (!Directory.Exists(_runnerFolder))
        {
            Directory.CreateDirectory(_runnerFolder);
        }

        return true;
    }

    /// <summary>
    /// Starts the Aspire Dashboard process.
    /// </summary>
    /// <exception cref="ApplicationException">
    /// Thrown when the Aspire Dashboard is already running.
    /// </exception>
    public async Task StartAsync()
    {
        if (!await InitializeAsync())
        {
            throw new ApplicationException("Could not find the dotnet CLI, make sure it is installed and available in the PATH");
        }

        if (IsRunning)
        {
            return;
        }

        var installedRuntimes = (await _dotnetCli.GetInstalledRuntimesAsync())
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

        var (isInstalled, isWorkload) = await GetInstallationPropertiesAsync();
        if (!isInstalled)
        {
            if (!_options.Runner.AutoDownload)
            {
                throw new ApplicationException("The Aspire Dashboard is not installed");
            }

            _logger.LogWarning("The Aspire Dashboard is not installed, download1ing the latest compatible version...");
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
            _dashboardCommand = _dotnetCli.RunAsync(["exec", Path.Combine(path, DllName)], path, _options.ToEnvironmentVariables(), OutputHandler, ErrorHandler);
            _dashboardProcess = Process.GetProcessById(_dashboardCommand.ProcessId);
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
    public async Task<bool> IsInstalledAsync() => (await GetInstallationPropertiesAsync()).Installed;

    private async Task<(bool Installed, bool Workload)> GetInstallationPropertiesAsync()
    {
        if (_dotnetCli.SdkPath != null)
        {
            var workloads = await _dotnetCli.GetInstalledWorkloadsAsync();
            if (workloads.Contains(WorkloadId))
            {
                _logger.LogTrace("Using the Aspire Dashboard workload");
                return (true, true);
            }
        }

        var downloadsFolder = Path.Combine(_runnerFolder, DownloadFolder);
        return (
            Installed: Directory.Exists(downloadsFolder) && Directory.EnumerateDirectories(downloadsFolder, "*.*").Any(),
            Workload: false
        );
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
    /// Stops the Aspire Dashboard process asynchronously (using Task.Run).
    /// </summary>
    public async Task StopAsync()
    {
        if (_dashboardCommand == null)
        {
            return;
        }

        await Task.Run(Stop);
    }

    /// <summary>
    /// Waits for the Aspire Dashboard process to exit asynchronously or until the cancellation token is triggered.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token to monitor for cancellation requests.</param>
    /// <exception cref="TaskCanceledException"> thrown when the cancellation token is triggered.</exception>
    public async Task WaitForExitAsync(CancellationToken cancellationToken = default)
    {
        if (!IsRunning || cancellationToken.IsCancellationRequested)
        {
            return;
        }

        if (cancellationToken == default || cancellationToken == CancellationToken.None)
        {
            await _dashboardCommand!.Task;
            return;
        }

        await Task.WhenAny(_dashboardCommand!.Task, Task.Delay(Timeout.Infinite, cancellationToken));
    }

    private async Task<(bool Downloaded, Version? Version)> TryDownloadAsync(Version? preferredVersion, Version[] installedRuntimes)
    {
        try
        {
            var availableVersions = await _nugetHelper.GetPackageVersionsAsync(_nugetPackageName);

            var latestRuntimeVersion = preferredVersion != null
                ? installedRuntimes.Where(v => v.IsCompatibleWith(preferredVersion)).Max()
                : installedRuntimes.Max();

            var versionToDownload = (
                preferredVersion != null
                    ? availableVersions.Where(preferredVersion.IsCompatibleWith).Max()
                    : availableVersions.Where(v => v.Major == latestRuntimeVersion!.Major).Max()
            ) ?? availableVersions.First(); // Fallback to the latest version

            var downloadSucceesful = await _nugetHelper.DownloadPackageAsync(_nugetPackageName, versionToDownload, Path.Combine(_runnerFolder, DownloadFolder, versionToDownload.ToString()));
            return (downloadSucceesful, versionToDownload);
        }
        catch
        {
            return (false, null);
        }
    }

    private async Task TryUpdateAsync(Version? preferredVersion)
    {
        try
        {
            var availableVersions = await _nugetHelper.GetPackageVersionsAsync(_nugetPackageName);

            var installedVersions = Directory.GetDirectories(Path.Combine(_runnerFolder, DownloadFolder))
                .Select(d => new Version(new DirectoryInfo(d).Name))
                .ToArray();

            var latestInstalledVersion = preferredVersion != null
                ? installedVersions.Where(v => v.IsCompatibleWith(preferredVersion)).Max()
                : installedVersions.Max();

            var latestAvailableVersion = preferredVersion != null
                ? availableVersions.Where(preferredVersion.IsCompatibleWith).Max()
                : availableVersions.Where(v => v.Major == latestInstalledVersion!.Major).Max();

            if (latestAvailableVersion > latestInstalledVersion)
            {
                _logger.LogWarning("A newer version of the Aspire Dashboard is available, downloading version {Version}", latestAvailableVersion);

                var newVersionFolder = Path.Combine(_runnerFolder, DownloadFolder, latestAvailableVersion!.ToString());
                var downloadSuccessful = await _nugetHelper.DownloadPackageAsync(_nugetPackageName, latestAvailableVersion, newVersionFolder);
                if (downloadSuccessful)
                {
                    _logger.LogInformation("Successfully updated the Aspire Dashboard to version {Version}", latestAvailableVersion);
                }
                else
                {
                    _logger.LogError("Failed to update the Aspire Dashboard, falling back to the installed version");
                    Directory.Delete(newVersionFolder, true);
                }
            }
        }
        catch
        {
            _logger.LogError("Failed to update the Aspire Dashboard, falling back to the installed version");
        }
    }

    private (Version Version, string Path)? GetInstallationInfo(bool workload)
    {
        try
        {
            var dashboardsFolder = workload ?
                _dotnetCli.GetPacksFolders()
                    .SelectMany(Directory.GetDirectories)
                    .First(dir => dir.Contains(SdkName))
                : Path.Combine(_runnerFolder, DownloadFolder);

            var installedVersions = Directory.GetDirectories(dashboardsFolder)
                .Select(d =>
                {
                    var dirInfo = new DirectoryInfo(d);
                    return (Version: new Version(dirInfo.Name), Path: dirInfo.FullName);
                })
                .OrderByDescending(v => v.Version)
                .ToArray();

            if (Version.TryParse(_options.Runner.RuntimeVersion, out var preferredVersion))
            {
                var preferredDashboard = installedVersions.FirstOrDefault(v => v.Version.IsCompatibleWith(preferredVersion));
                if (preferredDashboard.Path != null)
                {
                    return (preferredDashboard.Version, Path.Combine(preferredDashboard.Path, "tools"));
                }
            }

            // If a version is already installed, we probably already have a compatible runtime (no need to check)
            var newestVersion = installedVersions
                .MaxBy(d => d.Version);

            return newestVersion.Path == null ? null : (newestVersion.Version, Path.Combine(newestVersion.Path, "tools"));
        }
        catch
        {
            return null;
        }
    }

    private void PersistProcessId()
    {
        File.WriteAllText(Path.Combine(_runnerFolder, InstanceFile), _dashboardProcess!.Id.ToString());
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
            var (urlOpener, args) = PlatformHelper.GetUrlOpener(url);

            return Cli.Wrap(urlOpener)
                .WithArguments(args)
                .ExecuteAsync();
        }
        catch
        {
            _logger.LogWarning("Failed to launch the browser");
        }

        return Task.CompletedTask;
    }
}