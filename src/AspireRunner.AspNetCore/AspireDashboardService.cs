using AspireRunner.Core;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AspireRunner.AspNetCore;

public class AspireDashboardService(
    ILoggerFactory loggerFactory,
    ILogger<AspireDashboardService> logger,
    IOptions<AspireDashboardOptions> options,
    AspireDashboardManager dashboardManager) : IHostedService
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

                _aspireDashboard = await dashboardManager.GetDashboardAsync(options.Value, loggerFactory.CreateLogger<AspireDashboard>());
                logger.LogInformation("Found Aspire Dashboard version {Version}", _aspireDashboard.Version);

                _aspireDashboard.Start();
            }
            catch (Exception e)
            {
                logger.LogError("An error occurred while starting the Aspire Dashboard Service, {Error}", e.Message);
            }
        }, cancellationToken);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        if (_aspireDashboard is null || !_aspireDashboard.IsRunning)
        {
            return Task.CompletedTask;
        }

        logger.LogInformation("Stopping the Aspire Dashboard Service");
        return Task.Run(_aspireDashboard.Stop, CancellationToken.None);
    }
}