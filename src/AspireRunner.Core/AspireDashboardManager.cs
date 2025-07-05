using AspireRunner.Core.Helpers;
using Microsoft.Extensions.Logging;

namespace AspireRunner.Core;

public class AspireDashboardManager
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<AspireDashboardManager> _logger;

    private record DashboardInstallationInfo(Version Version, string Path);

    public AspireDashboardManager(ILogger<AspireDashboardManager> logger, ILoggerFactory loggerFactory)
    {
        _logger = logger;
        _loggerFactory = loggerFactory;
    }

    public async Task<AspireDashboard?> GetDashboardAsync(AspireDashboardOptions options)
    {
        var compatibleRuntimes = await GetCompatibleRuntimesAsync();
        if (_logger.IsEnabled(LogLevel.Trace))
        {
            _logger.LogTrace("Found compatible runtimes: {CompatibleRuntimes}", string.Join(", ", compatibleRuntimes.Select(v => $"{v}")));
        }

        if (compatibleRuntimes.Length == 0)
        {
            throw new ApplicationException($"The dashboard requires version '{AspireDashboard.MinimumRuntimeVersion}' or newer of the '{AspireDashboard.RequiredRuntimeName}' runtime");
        }

        var installedDashboards = GetInstalledDashboards();
        if (installedDashboards.Length is 0)
        {
            return null;
        }

        var runnerPath = GetRunnerPath();
        if (!Directory.Exists(runnerPath))
        {
            Directory.CreateDirectory(runnerPath);
        }

        if (VersionRange.TryParse(options.Runner.PreferredVersion, loose: true, out var preferredVersion))
        {
            var preferredDashboard = installedDashboards.FirstOrDefault(d => preferredVersion.IsSatisfied(d.Version));
            if (preferredDashboard is not null)
            {
                _logger.LogTrace("Aspire Dashboard installation path: '{AspirePath}'", preferredDashboard.Path);
                return new AspireDashboard(runnerPath, preferredDashboard.Version, preferredDashboard.Path, options, _loggerFactory.CreateLogger<AspireDashboard>());
            }

            _logger.LogWarning("Preferred Dashboard Version {PreferredVersion} not found, falling back to latest version installed", options.Runner.PreferredVersion);
        }

        var latestDashboard = installedDashboards.MaxBy(d => d.Version)!;
        _logger.LogTrace("Aspire Dashboard installation path: '{AspirePath}'", latestDashboard.Path);

        return new AspireDashboard(runnerPath, latestDashboard.Version, latestDashboard.Path, options, _loggerFactory.CreateLogger<AspireDashboard>());
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

    public static Version[] GetInstalledVersions()
    {
        return [..GetInstalledDashboards().Select(d => d.Version)];
    }

    private static DashboardInstallationInfo[] GetInstalledDashboards()
    {
        var dashboardsFolder = Path.Combine(GetRunnerPath(), AspireDashboard.DownloadFolder);
        if (!Directory.Exists(dashboardsFolder))
        {
            return [];
        }

        return
        [
            ..Directory.GetDirectories(dashboardsFolder, "*.*")
                .Select(TryParseInstallationInfo)
                .OfType<DashboardInstallationInfo>()
                .OrderByDescending(d => d.Version)
        ];
    }

    private static async Task<Version[]> GetCompatibleRuntimesAsync()
    {
        return (await DotnetCli.GetInstalledRuntimesAsync())
            .Where(r => r.Name is AspireDashboard.RequiredRuntimeName && r.Version >= AspireDashboard.MinimumRuntimeVersion)
            .Select(r => r.Version)
            .ToArray();
    }

    private static DashboardInstallationInfo? TryParseInstallationInfo(string directoryPath)
    {
        try
        {
            var dirInfo = new DirectoryInfo(directoryPath);
            var version = new Version(dirInfo.Name, true);
            return new DashboardInstallationInfo(version, Path.Combine(dirInfo.FullName, "tools"));
        }
        catch
        {
            return null;
        }
    }
}