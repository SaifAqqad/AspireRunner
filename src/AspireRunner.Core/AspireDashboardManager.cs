using AspireRunner.Core.Extensions;
using AspireRunner.Core.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AspireRunner.Core;

public class AspireDashboardManager
{
    private readonly DotnetCli _dotnetCli;
    private readonly NugetHelper _nugetHelper;
    private readonly ILogger<AspireDashboardManager> _logger;

    private bool _initialized;
    private string _runnerFolder = null!;
    private string _nugetPackageName = null!;

    public AspireDashboardManager(DotnetCli dotnetCli, NugetHelper nugetHelper, ILogger<AspireDashboardManager> logger)
    {
        _logger = logger;
        _dotnetCli = dotnetCli;
        _nugetHelper = nugetHelper;
    }

    public async Task<bool> InitializeAsync()
    {
        if (_initialized)
        {
            return true;
        }

        if (!await _dotnetCli.InitializeAsync())
        {
            return false;
        }

        _nugetPackageName = $"{AspireDashboard.SdkName}.{PlatformHelper.Rid}";
        _runnerFolder = Path.Combine(_dotnetCli.DataPath, AspireDashboard.DataFolder);
        if (!Directory.Exists(_runnerFolder))
        {
            Directory.CreateDirectory(_runnerFolder);
        }

        return _initialized = true;
    }

    public async Task<AspireDashboard> GetDashboardAsync(AspireDashboardOptions options, ILogger<AspireDashboard>? logger = null)
    {
        if (!_initialized)
        {
            throw new InvalidOperationException($"{nameof(AspireDashboardManager)} must be initialized before calling this method.");
        }

        var installedRuntimes = (await _dotnetCli.GetInstalledRuntimesAsync())
            .Where(r => r.Name is AspireDashboard.AspRuntimeName && r.Version >= AspireDashboard.MinimumRuntimeVersion)
            .Select(r => r.Version)
            .ToArray();

        _logger.LogTrace("Installed runtimes: {InstalledRuntimes}", string.Join(", ", installedRuntimes.Select(v => v.ToString())));
        if (installedRuntimes.Length == 0)
        {
            throw new ApplicationException($"The dashboard requires version '{AspireDashboard.MinimumRuntimeVersion}' or newer of the '{AspireDashboard.AspRuntimeName}' runtime");
        }

        var preferredVersion = Version.TryParse(options.Runner.RuntimeVersion, out var rv) ? rv : null;
        if (preferredVersion != null && !installedRuntimes.Any(v => v.IsCompatibleWith(preferredVersion)))
        {
            _logger.LogWarning("The specified runtime version '{RuntimeVersion}' is either not installed or incompatible with the dashboard, falling back to the latest installed version", preferredVersion);
            preferredVersion = null;
        }

        var (isInstalled, isWorkload) = await IsInstalledAsync();
        if (!isInstalled)
        {
            if (!options.Runner.AutoDownload)
            {
                throw new ApplicationException("The Aspire Dashboard is not installed");
            }

            _logger.LogWarning("The Aspire Dashboard is not installed, download1ing the latest compatible version...");
            var latestVersion = await FetchLatestVersionAsync(installedRuntimes, preferredVersion);

            var downloadedVersion = await InstallAsync(installedRuntimes, latestVersion);
            if (downloadedVersion == null)
            {
                throw new ApplicationException("Failed to download the Aspire Dashboard");
            }

            _logger.LogInformation("Successfully downloaded the Aspire Dashboard (version {Version})", downloadedVersion);
        }
        else if (!isWorkload && options.Runner.AutoDownload)
        {
            // We are using the runner-managed dashboards, so we can try to update
            await TryUpdateAsync(installedRuntimes, preferredVersion);
        }

        var installedDashboard = GetLatestInstalledVersion(isWorkload, preferredVersion);
        if (installedDashboard == null)
        {
            throw new ApplicationException("Failed to locate the Aspire Dashboard installation path");
        }

        var (version, path) = installedDashboard.Value;
        _logger.LogTrace("Aspire Dashboard installation path: {AspirePath}, Workload = {Workload}", path, isWorkload);

        return new AspireDashboard(_dotnetCli, version, path, options, logger ?? new NullLogger<AspireDashboard>());
    }

    public async Task<(bool Installed, bool Workload)> IsInstalledAsync()
    {
        if (!_initialized)
        {
            throw new InvalidOperationException($"{nameof(AspireDashboardManager)} must be initialized before calling this method.");
        }

        if (_dotnetCli.SdkPath != null)
        {
            var workloads = await _dotnetCli.GetInstalledWorkloadsAsync();
            if (workloads.Contains(AspireDashboard.WorkloadId))
            {
                _logger.LogTrace("Using the Aspire Dashboard workload");
                return (true, true);
            }
        }

        var downloadsFolder = Path.Combine(_runnerFolder, AspireDashboard.DownloadFolder);
        return (
            Installed: Directory.Exists(downloadsFolder) && Directory.EnumerateDirectories(downloadsFolder, "*.*").Any(),
            Workload: false
        );
    }

    public async Task<Version?> InstallAsync(Version[] installedRuntimes, Version? version = null)
    {
        if (!_initialized)
        {
            throw new InvalidOperationException($"{nameof(AspireDashboardManager)} must be initialized before calling this method.");
        }

        if (version == null)
        {
            version = await FetchLatestVersionAsync(installedRuntimes, null);
        }

        var downloadSucceesful = await _nugetHelper.DownloadPackageAsync(_nugetPackageName, version, Path.Combine(_runnerFolder, AspireDashboard.DownloadFolder, version.ToString()));
        return downloadSucceesful ? version : null;
    }

    public async Task TryUpdateAsync(Version[] installedRuntimes, Version? preferredVersion = null)
    {
        if (!_initialized)
        {
            throw new InvalidOperationException($"{nameof(AspireDashboardManager)} must be initialized before calling this method.");
        }

        try
        {
            var availableVersions = await _nugetHelper.GetPackageVersionsAsync(_nugetPackageName);

            var installedVersions = Directory.GetDirectories(Path.Combine(_runnerFolder, AspireDashboard.DownloadFolder))
                .Select(d => new Version(new DirectoryInfo(d).Name))
                .ToArray();

            var latestInstalledVersion = preferredVersion != null
                ? installedVersions.Where(v => v.IsCompatibleWith(preferredVersion)).Max()
                : installedVersions.Max();

            var latestRuntimeVersion = preferredVersion != null
                ? installedRuntimes.Where(v => v.IsCompatibleWith(preferredVersion)).Max()
                : installedRuntimes.Max();

            var latestAvailableVersion = preferredVersion != null
                ? availableVersions.Where(preferredVersion.IsCompatibleWith).Max()
                : availableVersions.Where(v => v.Major == latestRuntimeVersion!.Major).Max();

            if (latestAvailableVersion > latestInstalledVersion)
            {
                _logger.LogWarning("A newer version of the Aspire Dashboard is available, downloading version {Version}", latestAvailableVersion);

                var newVersionFolder = Path.Combine(_runnerFolder, AspireDashboard.DownloadFolder, latestAvailableVersion!.ToString());
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
            else
            {
                _logger.LogInformation("The Aspire Dashboard is up to date");
            }
        }
        catch
        {
            _logger.LogError("Failed to update the Aspire Dashboard, falling back to the installed version");
        }
    }

    private (Version Version, string Path)? GetLatestInstalledVersion(bool workload, Version? preferredVersion)
    {
        try
        {
            var dashboardsFolder = workload ?
                _dotnetCli.GetPacksFolders()
                    .SelectMany(Directory.GetDirectories)
                    .First(dir => dir.Contains(AspireDashboard.SdkName))
                : Path.Combine(_runnerFolder, AspireDashboard.DownloadFolder);

            var installedVersions = Directory.GetDirectories(dashboardsFolder)
                .Select(d =>
                {
                    var dirInfo = new DirectoryInfo(d);
                    return (Version: new Version(dirInfo.Name, true), Path: dirInfo.FullName);
                })
                .OrderByDescending(v => v.Version)
                .ToArray();

            if (preferredVersion != null)
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

    private async Task<Version> FetchLatestVersionAsync(Version[] installedRuntimes, Version? preferredVersion)
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

        return versionToDownload;
    }
}