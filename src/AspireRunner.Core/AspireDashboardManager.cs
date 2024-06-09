using AspireRunner.Core.Extensions;
using AspireRunner.Core.Helpers;
using Microsoft.Extensions.Logging;

namespace AspireRunner.Core;

public class AspireDashboardManager
{
    private readonly string _runnerFolder;
    private readonly string _nugetPackageName;

    private readonly DotnetCli _dotnetCli;
    private readonly NugetHelper _nugetHelper;
    private readonly ILogger<AspireDashboardManager> _logger;
    

    public AspireDashboardManager(DotnetCli dotnetCli, NugetHelper nugetHelper, ILogger<AspireDashboardManager> logger)
    {
        _dotnetCli = dotnetCli;
        _nugetHelper = nugetHelper;
        _logger = logger;

        _runnerFolder = Path.Combine(_dotnetCli.DataPath, AspireDashboard.DataFolder);
        if (!Directory.Exists(_runnerFolder))
        {
            Directory.CreateDirectory(_runnerFolder);
        }

        _nugetPackageName = $"{AspireDashboard.SdkName}.{RuntimeIdentification.Rid}";
    }


    private bool IsInstalled(out bool isWorkload)
    {
        if (_dotnetCli.SdkPath != null && _dotnetCli.GetInstalledWorkloads().Contains(AspireDashboard.WorkloadId))
        {
            _logger.LogTrace("Using the Aspire Dashboard workload");
            return isWorkload = true;
        }

        isWorkload = false;
        var downloadsFolder = Path.Combine(_runnerFolder, AspireDashboard.DownloadFolder);

        return Directory.Exists(downloadsFolder) && Directory.EnumerateDirectories(downloadsFolder, "*.*").Any();
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

            var downloadSucceesful = await _nugetHelper.DownloadPackageAsync(_nugetPackageName, versionToDownload, Path.Combine(_runnerFolder, AspireDashboard.DownloadFolder, versionToDownload.ToString()));
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

            var installedVersions = Directory.GetDirectories(Path.Combine(_runnerFolder, AspireDashboard.DownloadFolder))
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
        }
        catch
        {
            _logger.LogError("Failed to update the Aspire Dashboard, falling back to the installed version");
        }
    }

    private (Version Version, string Path)? GetInstallationInfo(bool workload, Version? preferredVersion)
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
                    return (Version: new Version(dirInfo.Name), Path: dirInfo.FullName);
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

    public Version[] GetCompatibleRuntimes()
    {
        return _dotnetCli.GetInstalledRuntimes()
            .Where(r => r.Name is AspireDashboard.AspRuntimeName && r.Version >= AspireDashboard.MinimumRuntimeVersion)
            .Select(r => r.Version)
            .ToArray();
    }
}