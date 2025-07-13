using Microsoft.Extensions.Logging;

namespace AspireRunner.Core;

public partial class DashboardFactory
{
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message = "Found compatible runtimes: {CompatibleRuntimes}"
    )]
    public partial void LogCompatibleRuntimes(string compatibleRuntimes);

    [LoggerMessage(
        Level = LogLevel.Trace,
        Message = "Aspire Dashboard installation path: '{Path}'"
    )]
    public partial void LogInstallationPath(string path);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Preferred Dashboard Version {PreferredVersion} not found, falling back to latest version installed"
    )]
    public partial void WarnPreferredVersionNotFound(string? preferredVersion);

    public void LogCompatibleRuntimes(Version[] compatibleRuntimes)
    {
        if (!_logger.IsEnabled(LogLevel.Trace))
        {
            return;
        }

        LogCompatibleRuntimes(string.Join(", ", compatibleRuntimes.Select(v => v.ToString())));
    }
}