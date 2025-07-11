using Microsoft.Extensions.Logging;

namespace AspireRunner.Core;

public class DashboardFactory : IDashboardFactory
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
        if (_logger.IsEnabled(LogLevel.Trace))
        {
            _logger.LogTrace("Found compatible runtimes: {CompatibleRuntimes}", string.Join(", ", compatibleRuntimes.Select(v => $"{v}")));
        }

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
                _logger.LogTrace("Aspire Dashboard installation path: '{AspirePath}'", preferredDashboard.Path);
                return new Dashboard(preferredDashboard.Version, preferredDashboard.Path, options, _loggerFactory.CreateLogger<Dashboard>());
            }

            _logger.LogWarning("Preferred Dashboard Version {PreferredVersion} not found, falling back to latest version installed", options.Runner.PreferredVersion);
        }

        var latestDashboard = installedDashboards.MaxBy(d => d.Version)!;
        _logger.LogTrace("Aspire Dashboard installation path: '{AspirePath}'", latestDashboard.Path);

        return new Dashboard(latestDashboard.Version, latestDashboard.Path, options, _loggerFactory.CreateLogger<Dashboard>());
    }
}