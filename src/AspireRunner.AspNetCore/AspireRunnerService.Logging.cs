using Microsoft.Extensions.Logging;

namespace AspireRunner.AspNetCore;

public partial class AspireRunnerService
{
    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "An error occurred while starting the Aspire Runner Service, {Error}"
    )]
    public partial void LogServiceError(string error);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Stopping the Aspire Runner Service"
    )]
    public partial void LogServiceStop();

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Starting the Aspire Runner Service"
    )]
    public partial void LogServiceStart();

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "The Aspire Dashboard is not installed, Add and set up the Installer nuget package or use the dotnet tool to install the dashboard"
    )]
    public partial void WarnNotInstalled();

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Found Aspire Dashboard version {Version}"
    )]
    public partial void LogVersionFound(Version version);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Aspire Dashboard Started, {Url}"
    )]
    public partial void LogDashboardStarted(string url);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Successfully installed dashboard version {Version}"
    )]
    public partial void LogSuccessfulInstallation(Version version);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Successfully updated Aspire Dashboard version to {Version}"
    )]
    public partial void LogSuccessfulUpdate(Version version);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Aspire Dashboard Installer failed to install the dashboard"
    )]
    public partial void WarnInstallationFailure();

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Aspire Dashboard Installer failed to update the dashboard"
    )]
    public partial void WarnUpdateFailure();
}