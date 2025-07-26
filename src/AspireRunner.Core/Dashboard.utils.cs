using AspireRunner.Core.Models;
using System.Diagnostics;

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
    /// Attempts to retrieve the currently running instances of the Aspire Dashboard and Runner processes.
    /// </summary>
    public static (Process? Dashboard, Process? Runner) TryGetRunningInstance()
    {
        var instanceFilePath = Path.Combine(GetRunnerPath(), InstanceFile);
        if (!File.Exists(instanceFilePath))
        {
            return default;
        }

        var instanceInfo = File.ReadAllText(instanceFilePath);
        if (string.IsNullOrWhiteSpace(instanceInfo))
        {
            return default;
        }

        var pids = instanceInfo.Split(':', 2);
        _ = int.TryParse(pids[0], out var dashboardPid);
        _ = int.TryParse(pids.ElementAtOrDefault(1), out var runnerPid);

        var runner = ProcessHelper.GetProcessOrDefault(runnerPid);
        var dashboard = ProcessHelper.GetProcessOrDefault(dashboardPid) is { ProcessName: "dotnet" } p ? p : null;

        return (dashboard, runner);
    }

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