using AspireRunner.Core;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AspireRunner.AspNetCore;

public class AspireDashboardService(ILogger<AspireDashboardService> logger, AspireDashboardManager dashboardManager, IOptions<AspireDashboardOptions> options, ILoggerFactory loggerFactory) : IHostedService
{
    private AspireDashboard? _aspireDashboard;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Avoid blocking the startup process
        _ = Task.Run(async () =>
        {
            try
            {
                logger.LogInformation("Starting the Aspire Dashboard Service");

                if (!await dashboardManager.InitializeAsync())
                {
                    logger.LogError("Failed to initialize the Aspire Dashboard Manager");
                    return;
                }

                _aspireDashboard = await dashboardManager.GetDashboardAsync(options.Value, loggerFactory.CreateLogger<AspireDashboard>());
                logger.LogInformation("Found Aspire Dashboard version {Version}", _aspireDashboard.Version);

                await _aspireDashboard.StartAsync();
            }
            catch (Exception e)
            {
                logger.LogError("An error occurred while starting the Aspire Dashboard Service, {Error}", e.Message);
            }
        }, cancellationToken);

        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_aspireDashboard is null || !_aspireDashboard.IsRunning)
        {
            return;
        }

        logger.LogInformation("Stopping the Aspire Dashboard Service");
        await _aspireDashboard.StopAsync();
    }
}