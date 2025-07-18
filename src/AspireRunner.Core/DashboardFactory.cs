using Microsoft.Extensions.Logging;

namespace AspireRunner.Core;

public partial class DashboardFactory : IDashboardFactory
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<DashboardFactory> _logger;

    public DashboardFactory(ILogger<DashboardFactory> logger, ILoggerFactory loggerFactory)
    {
        _logger = logger;
        _loggerFactory = loggerFactory;
    }

    public async Task<Dashboard?> CreateDashboardAsync(DashboardOptions options)
    {
        var compatibleRuntimes = await Dashboard.GetCompatibleRuntimesAsync();
        LogCompatibleRuntimes(compatibleRuntimes);

        if (compatibleRuntimes.Length == 0)
        {
            throw new ApplicationException($"The dashboard requires version '{Dashboard.MinimumRuntimeVersion}' or newer of the '{Dashboard.RequiredRuntimeName}' runtime");
        }

        var installedDashboards = Dashboard.GetInstalledDashboardsInfo();
        if (installedDashboards.Length is 0)
        {
            return null;
        }

        if (VersionRange.TryParse(options.Runner.PreferredVersion, loose: true, out var preferredVersion))
        {
            var preferredDashboard = installedDashboards.FirstOrDefault(d => preferredVersion.IsSatisfied(d.Version));
            if (preferredDashboard is not null)
            {
                LogInstallationPath(preferredDashboard.Path);
                return new Dashboard(preferredDashboard.Version, preferredDashboard.Path, options, _loggerFactory.CreateLogger<Dashboard>());
            }

            WarnPreferredVersionNotFound(options.Runner.PreferredVersion);
        }

        var latestDashboard = installedDashboards.MaxBy(d => d.Version)!;
        LogInstallationPath(latestDashboard.Path);

        return new Dashboard(latestDashboard.Version, latestDashboard.Path, options, _loggerFactory.CreateLogger<Dashboard>());
    }
}