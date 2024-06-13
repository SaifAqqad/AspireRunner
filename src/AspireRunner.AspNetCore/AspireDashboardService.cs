using AspireRunner.Core;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AspireRunner.AspNetCore;

public class AspireDashboardService(ILogger<AspireDashboardService> logger, AspireDashboard aspireDashboard) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Avoid blocking the startup process
        _ = Task.Run(async () =>
        {
            try
            {
                logger.LogInformation("Starting the Aspire Dashboard Service");
                await aspireDashboard.StartAsync();
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
        if (!aspireDashboard.IsRunning)
        {
            return;
        }

        logger.LogInformation("Stopping the Aspire Dashboard Service");
        await aspireDashboard.StopAsync();
    }
}