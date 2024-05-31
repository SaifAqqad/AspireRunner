using AspireRunner.Core;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AspireRunner.AspNetCore;

public class AspireDashboardService(ILogger<AspireDashboardService> logger, AspireDashboard? aspireDashboard = null) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // Avoid blocking the startup process
        await Task.Yield();

        if (aspireDashboard == null)
        {
            logger.LogError("The Aspire Dashboard is unavailable");
            return;
        }

        logger.LogInformation("Starting the Aspire Dashboard Service");
        await aspireDashboard.StartAsync();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (aspireDashboard == null)
        {
            return;
        }

        logger.LogInformation("Stopping the Aspire Dashboard Service");
        await aspireDashboard.StopAsync();
    }
}