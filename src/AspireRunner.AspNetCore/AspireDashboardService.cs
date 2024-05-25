using AspireRunner.Core;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AspireRunner.AspNetCore;

public class AspireDashboardService : BackgroundService
{
    private readonly AspireDashboard? _aspireDashboard;
    private readonly ILogger<AspireDashboardService> _logger;

    public AspireDashboardService(ILogger<AspireDashboardService> logger, AspireDashboard? aspireDashboard = null)
    {
        _logger = logger;
        _aspireDashboard = aspireDashboard;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Avoid blocking the startup process
        await Task.Yield();

        if (_aspireDashboard == null)
        {
            _logger.LogError("Failed to start Aspire Dashboard Service, Aspire Dashboard is unavailable");
            return;
        }

        stoppingToken.Register(() =>
        {
            _logger.LogInformation("Stopping Aspire Dashboard Service");
            _aspireDashboard.StopAsync();
        });

        _logger.LogInformation("Starting Aspire Dashboard Service");
        await _aspireDashboard.StartAsync();

        await _aspireDashboard.WaitForExitAsync(stoppingToken);
    }
}