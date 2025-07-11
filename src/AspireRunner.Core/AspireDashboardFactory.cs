using Microsoft.Extensions.Logging;

namespace AspireRunner.Core;

public class AspireDashboardFactory
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<AspireDashboardFactory> _logger;

    public AspireDashboardFactory(ILogger<AspireDashboardFactory> logger, ILoggerFactory loggerFactory)
    {
        _logger = logger;
        _loggerFactory = loggerFactory;
    }

    public async Task<AspireDashboard?> CreateDashboardAsync(AspireDashboardOptions options)
    {
        var compatibleRuntimes = await AspireDashboard.GetCompatibleRuntimesAsync();
        if (_logger.IsEnabled(LogLevel.Trace))
        {
            _logger.LogTrace("Found compatible runtimes: {CompatibleRuntimes}", string.Join(", ", compatibleRuntimes.Select(v => $"{v}")));
        }

        if (compatibleRuntimes.Length == 0)
        {
            throw new ApplicationException($"The dashboard requires version '{AspireDashboard.MinimumRuntimeVersion}' or newer of the '{AspireDashboard.RequiredRuntimeName}' runtime");
        }

        var installedDashboards = AspireDashboard.GetInstalledDashboardsInfo();
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
                return new AspireDashboard(preferredDashboard.Version, preferredDashboard.Path, options, _loggerFactory.CreateLogger<AspireDashboard>());
            }

            _logger.LogWarning("Preferred Dashboard Version {PreferredVersion} not found, falling back to latest version installed", options.Runner.PreferredVersion);
        }

        var latestDashboard = installedDashboards.MaxBy(d => d.Version)!;
        _logger.LogTrace("Aspire Dashboard installation path: '{AspirePath}'", latestDashboard.Path);

        return new AspireDashboard(latestDashboard.Version, latestDashboard.Path, options, _loggerFactory.CreateLogger<AspireDashboard>());
    }
}