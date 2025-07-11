using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AspireRunner.AspNetCore;

public class AspireDashboardService(
    IDashboardFactory factory,
    IOptions<DashboardOptions> options,
    ILogger<AspireDashboardService> logger,
    IDashboardInstaller? installer = null) : IHostedService
{
    private Dashboard? _aspireDashboard;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Avoid blocking the startup process
        _ = Task.Delay(10, cancellationToken).ContinueWith(_ => InitializeDashboard(cancellationToken), cancellationToken);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        if (_aspireDashboard is null || !_aspireDashboard.IsRunning)
        {
            return Task.CompletedTask;
        }

        logger.LogInformation("Stopping the Aspire Dashboard Service");
        return _aspireDashboard.StopAsync(CancellationToken.None);
    }

    private async Task InitializeDashboard(CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Starting the Aspire Dashboard Service");

            if (installer is not null)
            {
                var runnerOptions = options.Value.Runner;
                var installedVersions = Dashboard.GetInstalledVersions();

                Version? runnableVersion = null;
                if (runnerOptions.PreferredVersion is { } pv && VersionRange.TryParse(pv, out var preferredRange))
                {
                    var availableVersions = await installer.GetAvailableVersionsAsync(cancellationToken: cancellationToken);
                    var version = preferredRange.MaxSatisfying(availableVersions);

                    if (installedVersions.Any(v => v == version))
                    {
                        runnableVersion = version;
                    }
                    else
                    {
                        logger.LogInformation("Attempting to install preferred dashboard version {Version}", version);
                        runnableVersion = await installer.InstallAsync(version, cancellationToken) ? version : null;
                    }
                }

                if (runnableVersion is null && runnerOptions.AutoUpdate)
                {
                    var (success, latest, installed) = await installer.EnsureLatestAsync(cancellationToken);

                    if (!success)
                    {
                        logger.LogWarning("Aspire Dashboard Installer failed to install the dashboard");
                    }
                    else if (latest == installed)
                    {
                        logger.LogInformation("Aspire Dashboard is up to date");
                    }
                    else
                    {
                        logger.LogInformation("Successfully installed Aspire Dashboard version {Version}", latest);
                    }
                }
            }

            _aspireDashboard = await factory.CreateDashboardAsync(options.Value);
            if (_aspireDashboard is null)
            {
                logger.LogWarning("The Aspire Dashboard is not installed, Add the Installer nuget package or use the dotnet tool to install the dashboard.");
                return;
            }

            logger.LogInformation("Found Aspire Dashboard version {Version}", _aspireDashboard.Version);
            _aspireDashboard.DashboardStarted += url => logger.LogInformation("Aspire Dashboard Started, {Url}", url);

            await _aspireDashboard.StartAsync(cancellationToken);
        }
        catch (Exception e)
        {
            logger.LogError("An error occurred while starting the Aspire Dashboard Service, {Error}", e.Message);
        }
    }
}