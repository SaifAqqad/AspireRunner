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

    private readonly string _runnerFolder;
    private readonly string _nugetPackageName;

    public AspireDashboardManager(DotnetCli dotnetCli, NugetHelper nugetHelper, ILogger<AspireDashboardManager> logger)
    {
        _logger = logger;
        _dotnetCli = dotnetCli;
        _nugetHelper = nugetHelper;
        _nugetPackageName = $"{AspireDashboard.SdkName}.{PlatformHelper.Rid()}";
        _runnerFolder = Path.Combine(_dotnetCli.DataPath, AspireDashboard.DataFolder);

        if (!Directory.Exists(_runnerFolder))
        {
            Directory.CreateDirectory(_runnerFolder);
        }
    }

    public async Task<AspireDashboard> GetDashboardAsync(AspireDashboardOptions options, ILogger<AspireDashboard>? logger = null)
    {
        var installedRuntimes = (await _dotnetCli.GetInstalledRuntimesAsync())
            .Where(r => r.Name is AspireDashboard.AspRuntimeName && r.Version >= AspireDashboard.MinimumRuntimeVersion)
            .Select(r => r.Version)
            .ToArray();

        _logger.LogTrace("Installed runtimes: {InstalledRuntimes}", string.Join(", ", installedRuntimes.Select(v => v.ToString())));
        if (installedRuntimes.Length == 0)
        {
            throw new ApplicationException($"The dashboard requires version '{AspireDashboard.MinimumRuntimeVersion}' or newer of the '{AspireDashboard.AspRuntimeName}' runtime");
        }

        if (!IsInstalled())
        {
            if (!options.Runner.AutoDownload)
            {
                throw new ApplicationException("The Aspire Dashboard is not installed");
            }

            _logger.LogWarning("The Aspire Dashboard is not installed, downloading the latest compatible version...");
            var latestVersion = await FetchLatestVersionAsync(installedRuntimes);

            var downloadedVersion = await InstallAsync(installedRuntimes, latestVersion);
            if (downloadedVersion == null)
            {
                throw new ApplicationException("Failed to download the Aspire Dashboard");
            }

            _logger.LogInformation("Successfully downloaded the Aspire Dashboard (version {Version})", downloadedVersion);
        }
        else if (options.Runner.AutoDownload)
        {
            // We are using the runner-managed dashboards, so we can try to update
            await TryUpdateAsync(installedRuntimes);
        }

        var installedDashboard = GetLatestInstalledVersion();
        if (installedDashboard == null)
        {
            throw new ApplicationException("Failed to locate the Aspire Dashboard installation path");
        }

        var (version, path) = installedDashboard.Value;
        _logger.LogTrace("Aspire Dashboard installation path: {AspirePath}", path);

        return new AspireDashboard(_dotnetCli, version, path, options, logger ?? new NullLogger<AspireDashboard>());
    }

    public bool IsInstalled()
    {
        var downloadsFolder = Path.Combine(_runnerFolder, AspireDashboard.DownloadFolder);
        return Directory.Exists(downloadsFolder) && Directory.EnumerateDirectories(downloadsFolder, "*.*").Any();
    }

    public async Task<Version?> InstallAsync(Version[] installedRuntimes, Version? version = null)
    {
        if (version == null)
        {
            version = await FetchLatestVersionAsync(installedRuntimes);
        }

        var downloadSucceesful =
            await _nugetHelper.DownloadPackageAsync(_nugetPackageName, version, Path.Combine(_runnerFolder, AspireDashboard.DownloadFolder, version.ToString()));

        return downloadSucceesful ? version : null;
    }

    public async Task TryUpdateAsync(Version[] installedRuntimes, Version? preferredVersion = null)
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

            var latestRuntimeVersion = preferredVersion != null
                ? installedRuntimes.Where(v => v.IsCompatibleWith(preferredVersion)).Max()
                : installedRuntimes.Max();

            var latestAvailableVersion = availableVersions
                    .Where(preferredVersion != null ? preferredVersion.IsCompatibleWith : v => v.Major == latestRuntimeVersion!.Major)
                    .DefaultIfEmpty()
                    .Max()
                ?? availableVersions.First(); // Fallback to the latest version

            if (latestAvailableVersion > latestInstalledVersion)
            {
                _logger.LogWarning("A newer version of the Aspire Dashboard is available, downloading version {Version}", latestAvailableVersion);

                var newVersionFolder = Path.Combine(_runnerFolder, AspireDashboard.DownloadFolder, latestAvailableVersion.ToString());
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

    private (Version Version, string Path)? GetLatestInstalledVersion()
    {
        var dashboardsFolder = Path.Combine(_runnerFolder, AspireDashboard.DownloadFolder);
        try
        {
            var installedVersions = Directory.GetDirectories(dashboardsFolder)
                .Select(d =>
                {
                    var dirInfo = new DirectoryInfo(d);
                    return (Version: new Version(dirInfo.Name, true), Path: dirInfo.FullName);
                })
                .OrderByDescending(v => v.Version)
                .ToArray();

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

    private async Task<Version> FetchLatestVersionAsync(Version[] installedRuntimes)
    {
        var availableVersions = await _nugetHelper.GetPackageVersionsAsync(_nugetPackageName);
        if (availableVersions.Length == 0)
        {
            throw new ApplicationException("No versions of the Aspire Dashboard are available");
        }

        var latestRuntimeVersion = installedRuntimes.Max();
        var versionToDownload = availableVersions
                .Where(v => v.Major == latestRuntimeVersion!.Major)
                .DefaultIfEmpty()
                .Max()
            ?? availableVersions.First(); // Fallback to the latest version

        return versionToDownload;
    }
}