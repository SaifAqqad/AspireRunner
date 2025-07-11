using AspireRunner.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NuGet.Packaging;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using NullLogger = NuGet.Common.NullLogger;

namespace AspireRunner.Installer;

public class AspireDashboardInstaller
{
    private const string SdkName = "Aspire.Dashboard.Sdk";
    private const string DefaultRepoUrl = "https://api.nuget.org/v3/index.json";

    private readonly string _runnerPath;
    private readonly string _nugetPackageName;
    private readonly SourceCacheContext _nugetCache;
    private readonly SourceRepository _nugetRepository;
    private readonly ILogger<AspireDashboardInstaller> _logger;

    public AspireDashboardInstaller(ILogger<AspireDashboardInstaller>? logger)
    {
        _nugetCache = new SourceCacheContext();
        _runnerPath = AspireDashboard.GetRunnerPath();
        _nugetPackageName = $"{SdkName}.{PlatformHelper.Rid}";
        _logger = logger ?? NullLogger<AspireDashboardInstaller>.Instance;

        var repoUrl = EnvironmentVariables.NugetRepoUrl;
        if (string.IsNullOrWhiteSpace(repoUrl))
        {
            repoUrl = DefaultRepoUrl;
        }

        _nugetRepository = Repository.Factory.GetCoreV3(repoUrl);
    }

    /// <summary>
    /// Checks if the latest version of the dashboard is installed, and if not, installs it to the runner's path.
    /// </summary>
    /// <returns>A tuple containing a success flag, the latest version available, and the currently installed version (if any).</returns>
    /// <exception cref="ApplicationException">Thrown If no versions are fetched from the nuget repo, could be caused by a network issue or the repo simply not having any versions of the package available</exception>
    public async Task<(bool Success, Version Latest, Version? Installed)> EnsureLatestAsync(CancellationToken cancellationToken = default)
    {
        var latestRuntimeVersion = (await AspireDashboard.GetCompatibleRuntimesAsync()).Max();

        var availableVersions = await GetAvailableVersionsAsync(cancellationToken: cancellationToken);
        if (availableVersions.Length == 0)
        {
            throw new ApplicationException("No versions of the Aspire Dashboard are available");
        }

        var latestCompatible = availableVersions
                .Where(v => IsRuntimeCompatible(v, latestRuntimeVersion!))
                .Max()
            ?? availableVersions.First(); // Fallback to the latest version

        var latestInstalled = AspireDashboard.GetInstalledVersions().Max();
        if (latestInstalled == latestCompatible)
        {
            // Dashboard is up to date
            return (true, latestCompatible, latestInstalled);
        }

        var success = await InstallAsync(latestCompatible, cancellationToken);
        return (success, latestCompatible, latestInstalled);
    }

    /// <summary>
    /// Downloads and installs the specified dashboard version to the runner's path.
    /// </summary>
    /// <param name="version">The dashboard version to install</param>
    /// <returns><c>true</c> if the dashboard was installed successfully, <c>false</c> otherwise</returns>
    public async Task<bool> InstallAsync(Version version, CancellationToken cancellationToken = default)
    {
        try
        {
            using var packageStream = new MemoryStream();
            var resource = await _nugetRepository.GetResourceAsync<FindPackageByIdResource>(cancellationToken);

            var destinationPath = Path.Combine(_runnerPath, version.ToString());
            _logger.LogTrace("Downloading {PackageName} {Version} to {DestinationPath}", _nugetPackageName, version, destinationPath);

            var success = await resource.CopyNupkgToStreamAsync(
                _nugetPackageName,
                new NuGetVersion(version.ToString()),
                packageStream,
                _nugetCache,
                NullLogger.Instance,
                CancellationToken.None
            );

            if (!success || cancellationToken.IsCancellationRequested)
            {
                return false;
            }

            using var packageReader = new PackageArchiveReader(packageStream);
            await packageReader.CopyFilesAsync(destinationPath, packageReader.GetFiles(), ExtractFile, NullLogger.Instance, cancellationToken);

            return true;
        }
        catch (Exception e)
        {
            _logger.LogError("Failed to download {PackageName} {Version} to {Path}, {Exception}", _nugetPackageName, version, _runnerPath, e.Message);
            return false;
        }
    }

    /// <summary>
    /// Removes the specified dashboard version from the runner's path if it's installed.
    /// </summary>
    /// <param name="version">The dashboard version to remove</param>
    /// <returns><c>true</c> If the version is removed successfully, <c>false</c> otherwise</returns>
    public async Task<bool> RemoveAsync(Version version, CancellationToken cancellationToken)
    {
        var installDirectory = Path.Combine(_runnerPath, version.ToString());
        if (!Directory.Exists(installDirectory))
        {
            return false;
        }

        try
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return false;
            }

            await Task.Factory.StartNew(p => Directory.Delete((string)p!, recursive: true), installDirectory, cancellationToken);
            _logger.LogTrace("Removed {PackageName} {Version} from {Path}", _nugetPackageName, version, _runnerPath);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove {PackageName} {Version} from {Path}", _nugetPackageName, version, _runnerPath);
            return false;
        }
    }

    /// <summary>
    /// Returns the available dashboard versions in descending order (newest to oldest).
    /// </summary>
    /// <param name="includePreRelease">Whether to include pre-release versions, defaults to false</param>
    public async Task<Version[]> GetAvailableVersionsAsync(bool includePreRelease = false, CancellationToken cancellationToken = default)
    {
        try
        {
            var resource = await _nugetRepository.GetResourceAsync<FindPackageByIdResource>(cancellationToken);
            var metadata = await resource.GetAllVersionsAsync(_nugetPackageName, _nugetCache, NullLogger.Instance, CancellationToken.None);

            return metadata
                .Select(v => new Version(v.ToFullString(), true))
                .Where(v => includePreRelease || !v.IsPreRelease)
                .OrderByDescending(v => v)
                .ToArray();
        }
        catch
        {
            return [];
        }
    }

    private string ExtractFile(string sourceFile, string targetPath, Stream fileStream)
    {
        try
        {
            _logger.LogTrace("Extracting {SourceFile} to {TargetPath}", sourceFile, targetPath);

            // Ensure the directory exists
            Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);

            using var file = File.Create(targetPath);
            fileStream.CopyTo(file);

            return targetPath;
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to extract {SourceFile} to {TargetPath}, {Exception}", sourceFile, targetPath, ex.Message);
            throw;
        }
    }

    private static bool IsRuntimeCompatible(Version version, Version runtimeVersion)
    {
        return runtimeVersion >= AspireDashboard.MinimumRuntimeVersion && version.Major >= runtimeVersion.Major;
    }
}