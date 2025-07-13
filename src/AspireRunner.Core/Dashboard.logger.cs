using Microsoft.Extensions.Logging;

namespace AspireRunner.Core;

public partial class Dashboard
{
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Failed to start Dashboard, retrying in {RetryDelay} seconds..."
    )]
    private partial void WarnFailedToStartDashboardWithRetry(int retryDelay);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Dashboard has already been stopped"
    )]
    private partial void WarnDashboardAlreadyStopped();

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Another instance of the Aspire Dashboard is already running, Process Id = {PID}"
    )]
    private partial void WarnExistingInstance(int pid);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Failed to start Dashboard process"
    )]
    private partial void LogFailedToStartDashboardProcess();

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Failed to start Dashboard"
    )]
    private partial void LogFailedToStartDashboard(Exception exception);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Dashboard exited unexpectedly, Attempting to restart..."
    )]
    private partial void WarnDashboardExitedUnexpectedly();

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Failed to find a suitable URL opener"
    )]
    private partial void WarnFailedToFindUrlOpener();

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Failed to launch the default browser"
    )]
    private partial void WarnFailedToLaunchBrowser();

    private void LogDashboardError(string error) => _logger.LogError(error);
}