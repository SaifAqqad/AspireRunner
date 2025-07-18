using Microsoft.Extensions.Logging;

namespace AspireRunner.Installer;

public partial class DashboardInstaller
{
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message = "Found AspNetCore Runtime v{version}"
    )]
    private partial void TraceFoundRuntimeVersion(Version? version);

    [LoggerMessage(
        Level = LogLevel.Trace,
        Message = "Fetched package {packageName} versions from nuget: {versions}"
    )]
    private partial void TraceFetchedPackageVersions(string packageName, string versions);

    [LoggerMessage(
        Level = LogLevel.Trace,
        Message = "Found package {packageName} installed versions {versions}"
    )]
    private partial void TraceFoundInstalledVersions(string packageName, string versions);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Latest dashboard v{version} is already installed"
    )]
    private partial void LogLatestVersionIsAlreadyInstalled(Version version);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Installed {packageName} v{version} to {path}"
    )]
    private partial void LogInstalledPackageVersionToPath(string packageName, Version version, string path);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Failed to download {packageName} {version} to {path}, {exception}"
    )]
    private partial void LogFailedToDownloadPackage(string packageName, Version version, string path, string exception);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Removed {packageName} v{version} from {path}"
    )]
    private partial void LogRemovedPackageVersion(string packageName, Version version, string path);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Failed to remove {packageName} {version} from {path}"
    )]
    private partial void LogFailedToRemovePackage(Exception exception, string packageName, Version version, string path);

    [LoggerMessage(
        Level = LogLevel.Trace,
        Message = "Extracting {sourceFile} to {targetPath}"
    )]
    private partial void TraceExtractingFileToPath(string sourceFile, string targetPath);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Failed to extract {sourceFile} to {targetPath}"
    )]
    private partial void LogFailedToExtractFile(Exception exception, string sourceFile, string targetPath);

    private void TraceFetchedPackageVersions(string packageName, Version[] availableVersions)
    {
        if (!_logger.IsEnabled(LogLevel.Trace))
        {
            return;
        }

        TraceFetchedPackageVersions(packageName, string.Join(", ", availableVersions.Select(v => v.ToString())));
    }

    private void TraceFoundInstalledVersions(string packageName, Version[] installedVersions)
    {
        if (!_logger.IsEnabled(LogLevel.Trace))
        {
            return;
        }

        TraceFoundInstalledVersions(packageName, string.Join(", ", installedVersions.Select(v => v.ToString())));
    }
}