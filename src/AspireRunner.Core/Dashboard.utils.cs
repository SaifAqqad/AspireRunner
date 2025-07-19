using AspireRunner.Core.Models;

namespace AspireRunner.Core;

// Only static methods should go here :)
public partial class Dashboard
{
    /// <summary>
    /// Returns the path to the Aspire Runner folder.
    /// </summary>
    /// <remarks>By default, the <c>.dotnet</c> folder inside the user's profile is used. This can be overridden using the environment variable <c>ASPIRE_RUNNER_PATH</c>.</remarks>
    public static string GetRunnerPath()
    {
        var runnerPath = EnvironmentVariables.RunnerPath;
        if (string.IsNullOrWhiteSpace(runnerPath))
        {
            runnerPath = Path.Combine(DotnetCli.DataPath, RunnerFolder);
        }

        return runnerPath;
    }

    /// <summary>
    /// Returns the installed ASP.NET Core runtimes that are compatible with the dashboard.
    /// </summary>
    public static async Task<Version[]> GetCompatibleRuntimesAsync()
    {
        return (await DotnetCli.GetInstalledRuntimesAsync())
            .Where(r => r.Name is RequiredRuntimeName && r.Version >= MinimumRuntimeVersion)
            .Select(r => r.Version)
            .ToArray();
    }

    /// <summary>
    /// Returns the installed dashboard versions in descending order (newest to oldest).
    /// </summary>
    public static Version[] GetInstalledVersions() => [..GetInstalledDashboardsInfo().Select(d => d.Version)];

    /// <summary>
    /// Returns the installed dashboards (version and path) in descending order (newest to oldest).
    /// </summary>
    internal static DashboardInstallationInfo[] GetInstalledDashboardsInfo()
    {
        var dashboardsFolder = Path.Combine(GetRunnerPath(), DownloadFolder);
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

        static DashboardInstallationInfo? TryParseInstallationInfo(string directoryPath)
        {
            try
            {
                var dirInfo = new DirectoryInfo(directoryPath);
                var version = new Version(dirInfo.Name, true);
                return new DashboardInstallationInfo { Version = version, Path = Path.Combine(dirInfo.FullName, "tools") };
            }
            catch
            {
                return null;
            }
        }
    }
}