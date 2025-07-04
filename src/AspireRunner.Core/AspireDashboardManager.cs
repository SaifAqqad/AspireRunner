using AspireRunner.Core.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AspireRunner.Core;

public class AspireDashboardManager
{
    private const string AspRuntimeName = "Microsoft.AspNetCore.App";

    private readonly string _runnerPath;
    private readonly ILogger<AspireDashboardManager> _logger;

    private record DashboardInstallationInfo(Version Version, string Path);

    public AspireDashboardManager(ILogger<AspireDashboardManager> logger)
    {
        _logger = logger;
        _runnerPath = GetRunnerPath();
        if (!Directory.Exists(_runnerPath))
        {
            Directory.CreateDirectory(_runnerPath);
        }
    }

    public async Task<AspireDashboard> GetDashboardAsync(AspireDashboardOptions options, ILogger<AspireDashboard>? logger = null)
    {
        var compatibleRuntimes = await GetCompatibleRuntimes();
        if (_logger.IsEnabled(LogLevel.Trace))
        {
            _logger.LogTrace("Found compatible runtimes: {CompatibleRuntimes}", string.Join(", ", compatibleRuntimes.Select(v => $"{v}")));
        }

        if (compatibleRuntimes.Length == 0)
        {
            throw new ApplicationException($"The dashboard requires version '{AspireDashboard.MinimumRuntimeVersion}' or newer of the '{AspRuntimeName}' runtime");
        }

        var preferredDashboard = TryGetPreferredDashboard(options.Runner.PreferredVersion, compatibleRuntimes);
        if (preferredDashboard is not null)
        {
            _logger.LogTrace("Aspire Dashboard installation path: '{AspirePath}'", preferredDashboard.Path);
            return new AspireDashboard(_runnerPath, preferredDashboard.Version, preferredDashboard.Path, options, logger ?? new NullLogger<AspireDashboard>());
        }

        // TODO: Move this out of core
        // if (!IsInstalled())
        // {
        //     _logger.LogWarning("The Aspire Dashboard is not installed, downloading the latest version from nuget...");
        //     var latestVersion = await FetchLatestVersionAsync(compatibleRuntimes);
        //
        //     var downloadedVersion = await InstallAsync(compatibleRuntimes, latestVersion);
        //     if (downloadedVersion == null)
        //     {
        //         throw new ApplicationException("Failed to download the Aspire Dashboard");
        //     }
        //
        //     _logger.LogInformation("Successfully downloaded the Aspire Dashboard (version {Version})", downloadedVersion);
        // }
        // else if (options.Runner.AutoUpdate)
        // {
        //     await TryUpdateAsync(compatibleRuntimes);
        // }

        if (!IsInstalled())
        {
            throw new ApplicationException("The Aspire Dashboard is not installed, use the Installer nuget package or dotnet tool to install the dashboard.");
        }

        var installedDashboard = GetInstalledDashboards().DefaultIfEmpty().MaxBy(d => d?.Version);
        if (installedDashboard is null)
        {
            throw new ApplicationException("Failed to locate the Aspire Dashboard installation path");
        }

        _logger.LogTrace("Aspire Dashboard installation path: '{AspirePath}'", installedDashboard.Path);
        return new AspireDashboard(_runnerPath, installedDashboard.Version, installedDashboard.Path, options, logger ?? new NullLogger<AspireDashboard>());
    }

    private static async Task<Version[]> GetCompatibleRuntimes()
    {
        return (await DotnetCli.GetInstalledRuntimesAsync())
            .Where(r => r.Name is AspRuntimeName && r.Version >= AspireDashboard.MinimumRuntimeVersion)
            .Select(r => r.Version)
            .ToArray();
    }

    public bool IsInstalled()
    {
        var downloadsFolder = Path.Combine(_runnerPath, AspireDashboard.DownloadFolder);
        return Directory.Exists(downloadsFolder) && Directory.EnumerateDirectories(downloadsFolder, "*.*").Any();
    }

    private DashboardInstallationInfo? TryGetPreferredDashboard(string? preferredVersion, Version[] installedRuntimes)
    {
        if (string.IsNullOrWhiteSpace(preferredVersion) || !Version.TryParse(preferredVersion, true, out var version))
        {
            return null;
        }

        return GetInstalledDashboard(version);

        // TODO: Move this somewhere else
        // _logger.LogInformation("Version '{Version}' of the Aspire Dashboard is not installed, downloading it from nuget...", preferredVersion);
        // var downloadedVersion = await InstallAsync(installedRuntimes, version);
        // dashboardInstallation = downloadedVersion is not null ? GetInstalledDashboard(downloadedVersion) : null;
        //
        // if (dashboardInstallation is null)
        // {
        //     _logger.LogWarning("Failed to download version '{Version}' of the Aspire Dashboard, falling back to the latest version", preferredVersion);
        // }
        // else
        // {
        //     _logger.LogInformation("Successfully downloaded version '{Version}' of the Aspire Dashboard", preferredVersion);
        // }
        //
        // return dashboardInstallation;
    }

    private DashboardInstallationInfo[] GetInstalledDashboards()
    {
        var dashboardsFolder = Path.Combine(_runnerPath, AspireDashboard.DownloadFolder);
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
        var dashboardPath = Path.Combine(_runnerPath, AspireDashboard.DownloadFolder, version.ToString());
        return Directory.Exists(dashboardPath) ? new DashboardInstallationInfo(version, $"{dashboardPath}/tools") : null;
    }

    public static string GetRunnerPath()
    {
        var runnerPath = EnvironmentVariables.RunnerPath;
        if (string.IsNullOrWhiteSpace(runnerPath))
        {
            runnerPath = Path.Combine(DotnetCli.DataPath, AspireDashboard.RunnerFolder);
        }

        return runnerPath;
    }

    private static bool IsRuntimeCompatible(Version version, Version runtimeVersion)
    {
        return runtimeVersion >= AspireDashboard.MinimumRuntimeVersion && version.Major >= runtimeVersion.Major;
    }
}