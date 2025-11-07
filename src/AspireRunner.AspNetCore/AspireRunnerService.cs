using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AspireRunner.AspNetCore;

public partial class AspireRunnerService(
    IDashboardFactory factory,
    IOptions<DashboardOptions> options,
    ILogger<AspireRunnerService> logger,
    IDashboardInstaller? installer = null) : IHostedService
{
    private Dashboard? _aspireDashboard;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Avoid blocking the startup process
        _ = Task.Factory.StartNew(async () =>
        {
            try
            {
                await InitializeDashboard(cancellationToken);
            }
            catch (Exception e)
            {
                LogServiceError(e.Message);
            }
        }, TaskCreationOptions.LongRunning);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        if (_aspireDashboard is null || !_aspireDashboard.IsRunning)
        {
            return Task.CompletedTask;
        }

        LogServiceStop();
        return _aspireDashboard.StopAsync(CancellationToken.None);
    }

    private async Task InitializeDashboard(CancellationToken cancellationToken)
    {
        LogServiceStart();
        await RunInstallerAsync(cancellationToken);

        _aspireDashboard = await factory.CreateDashboardAsync(options.Value);
        if (_aspireDashboard is null)
        {
            WarnNotInstalled();
            return;
        }

        LogVersionFound(_aspireDashboard.Version);
        _aspireDashboard.DashboardStarted += LogDashboardStarted;

        await _aspireDashboard.StartAsync(cancellationToken);
    }

    private async Task RunInstallerAsync(CancellationToken cancellationToken)
    {
        if (installer is null)
        {
            return;
        }

        var runnerOptions = options.Value.Runner;
        var installedVersions = Dashboard.GetInstalledVersions();

        if (runnerOptions.PreferredVersion is { } pv && VersionRange.TryParse(pv, out var preferredRange))
        {
            var availableVersions = await installer.GetAvailableVersionsAsync(cancellationToken: cancellationToken);
            var version = preferredRange.MaxSatisfying(availableVersions);

            if (installedVersions.Any(v => v == version))
            {
                // Preferred version is already installed
                return;
            }

            if (await installer.InstallAsync(version, cancellationToken))
            {
                LogSuccessfulInstallation(version);
                return;
            }
        }

        if (installedVersions.Length is 0)
        {
            var availableVersions = await installer.GetAvailableVersionsAsync(cancellationToken: cancellationToken);
            var latestVersion = availableVersions.First();

            if (await installer.InstallAsync(latestVersion, cancellationToken))
            {
                LogSuccessfulInstallation(latestVersion);
                return;
            }

            WarnInstallationFailure();
            return;
        }

        if (runnerOptions.AutoUpdate)
        {
            var (success, latest, installed) = await installer.EnsureLatestAsync(cancellationToken);

            if (!success)
            {
                WarnUpdateFailure();
                return;
            }

            if (latest != installed)
            {
                LogSuccessfulUpdate(latest);
            }
        }
    }
}