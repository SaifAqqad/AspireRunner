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

    private record DashboardInstallationInfo(Version Version, string Path);

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

        var dashboardInstallation = await TryGetPreferredDashboard(options.Runner.PreferredVersion, installedRuntimes);
        if (dashboardInstallation is null)
        {
            if (!IsInstalled())
            {
                _logger.LogWarning("The Aspire Dashboard is not installed, downloading the latest version from nuget...");
                var latestVersion = await FetchLatestVersionAsync(installedRuntimes);

                var downloadedVersion = await InstallAsync(installedRuntimes, latestVersion);
                if (downloadedVersion == null)
                {
                    throw new ApplicationException("Failed to download the Aspire Dashboard");
                }

                _logger.LogInformation("Successfully downloaded the Aspire Dashboard (version {Version})", downloadedVersion);
            }
            else if (options.Runner.AutoUpdate)
            {
                // We are using the runner-managed dashboards, so we can try to update
                await TryUpdateAsync(installedRuntimes);
            }

            dashboardInstallation = GetInstalledDashboards().DefaultIfEmpty().MaxBy(d => d?.Version);
            if (dashboardInstallation is null)
            {
                throw new ApplicationException("Failed to locate the Aspire Dashboard installation path");
            }
        }

        _logger.LogTrace("Aspire Dashboard installation path: '{AspirePath}'", dashboardInstallation.Path);
        return new AspireDashboard(_dotnetCli, dashboardInstallation.Version, dashboardInstallation.Path, options, logger ?? new NullLogger<AspireDashboard>());
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

        var installationPath = Path.Combine(_runnerFolder, AspireDashboard.DownloadFolder, version.ToString());
        return await _nugetHelper.DownloadPackageAsync(_nugetPackageName, version, installationPath) ? version : null;
    }

    public async Task TryUpdateAsync(Version[] installedRuntimes)
    {
        try
        {
            var availableVersions = await _nugetHelper.GetPackageVersionsAsync(_nugetPackageName);

            var installedVersions = Directory.GetDirectories(Path.Combine(_runnerFolder, AspireDashboard.DownloadFolder))
                .Select(d => new Version(new DirectoryInfo(d).Name))
                .ToArray();

            var latestRuntimeVersion = installedRuntimes.Max();
            var latestInstalledVersion = installedVersions.Max();

            var latestAvailableVersion = availableVersions
                    .Where(v => v.Major == latestRuntimeVersion!.Major)
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

    private async Task<DashboardInstallationInfo?> TryGetPreferredDashboard(string? preferredVersion, Version[] installedRuntimes)
    {
        DashboardInstallationInfo? dashboardInstallation = null;
        if (!string.IsNullOrWhiteSpace(preferredVersion) && Version.TryParse(preferredVersion, true, out var version))
        {
            dashboardInstallation = GetInstalledDashboard(version);
            if (dashboardInstallation is null)
            {
                _logger.LogInformation("Version '{Version}' of the Aspire Dashboard is not installed, downloading it from nuget...", preferredVersion);

                var downloadedVersion = await InstallAsync(installedRuntimes, version);
                dashboardInstallation = downloadedVersion is not null ? GetInstalledDashboard(downloadedVersion) : null;

                if (dashboardInstallation is null)
                {
                    _logger.LogWarning("Failed to download version '{Version}' of the Aspire Dashboard, falling back to the latest version", preferredVersion);
                }
                else
                {
                    _logger.LogInformation("Successfully downloaded version '{Version}' of the Aspire Dashboard", preferredVersion);
                }
            }
        }

        return dashboardInstallation;
    }

    private DashboardInstallationInfo[] GetInstalledDashboards()
    {
        var dashboardsFolder = Path.Combine(_runnerFolder, AspireDashboard.DownloadFolder);
        try
        {
            var installedVersions = Directory.GetDirectories(dashboardsFolder)
                .Select(d =>
                {
                    var dirInfo = new DirectoryInfo(d);
                    var version = new Version(dirInfo.Name, true);
                    return new DashboardInstallationInfo(version, Path.Combine(dirInfo.FullName, "tools"));
                })
                .OrderByDescending(v => v.Version)
                .ToArray();

            return installedVersions;
        }
        catch
        {
            return [];
        }
    }

    private DashboardInstallationInfo? GetInstalledDashboard(Version version)
    {
        var dashboardPath = $"{Path.Combine(_runnerFolder, AspireDashboard.DownloadFolder)}/{version}";

        return Directory.Exists(dashboardPath) ? new DashboardInstallationInfo(version, $"{dashboardPath}/tools") : null;
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