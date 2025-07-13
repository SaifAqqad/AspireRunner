using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NuGet.Packaging;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using NullLogger = NuGet.Common.NullLogger;

namespace AspireRunner.Installer;

public class DashboardInstaller : IDashboardInstaller
{
    private const string SdkName = "Aspire.Dashboard.Sdk";
    private const string DefaultRepoUrl = "https://api.nuget.org/v3/index.json";

    private readonly string _downloadPath;
    private readonly string _nugetPackageName;
    private readonly SourceCacheContext _nugetCache;
    private readonly SourceRepository _nugetRepository;
    private readonly ILogger<DashboardInstaller> _logger;

    public DashboardInstaller(ILogger<DashboardInstaller>? logger)
    {
        _nugetCache = new SourceCacheContext();
        _nugetPackageName = $"{SdkName}.{PlatformHelper.Rid}";
        _logger = logger ?? NullLogger<DashboardInstaller>.Instance;
        _downloadPath = Path.Combine(Dashboard.GetRunnerPath(), Dashboard.DownloadFolder);

        var repoUrl = EnvironmentVariables.NugetRepoUrl;
        if (string.IsNullOrWhiteSpace(repoUrl))
        {
            repoUrl = DefaultRepoUrl;
        }

        _nugetRepository = Repository.Factory.GetCoreV3(repoUrl);
    }

    public async Task<(bool Success, Version Latest, Version? Installed)> EnsureLatestAsync(CancellationToken cancellationToken = default)
    {
        var latestRuntimeVersion = (await Dashboard.GetCompatibleRuntimesAsync()).Max();
        _logger.LogTrace("Found AspNetCore Runtime version {Version}", latestRuntimeVersion);

        var availableVersions = await GetAvailableVersionsAsync(cancellationToken: cancellationToken);
        if (availableVersions.Length == 0)
        {
            throw new ApplicationException("No versions of the Aspire Dashboard are available");
        }

        if (_logger.IsEnabled(LogLevel.Trace))
        {
            _logger.LogTrace("Fetched package {Package} versions from nuget: {Versions}", _nugetPackageName, string.Join(", ", [..availableVersions]));
        }

        var latestCompatible = availableVersions
                .Where(v => IsRuntimeCompatible(v, latestRuntimeVersion!))
                .Max()
            ?? availableVersions.First(); // Fallback to the latest version

        var installedVersions = Dashboard.GetInstalledVersions();
        if (_logger.IsEnabled(LogLevel.Trace))
        {
            _logger.LogTrace("Found package {Package} installed versions {Versions}", _nugetPackageName, string.Join(", ", [..installedVersions]));
        }

        var latestInstalled = installedVersions.Max();
        if (latestInstalled == latestCompatible)
        {
            _logger.LogInformation("Latest dashboard version {Version} is already installed", latestCompatible);

            // Dashboard is up to date
            return (true, latestCompatible, latestInstalled);
        }

        _logger.LogInformation("Attempting to install dashboard version {Version}", latestCompatible);
        var success = await InstallAsync(latestCompatible, cancellationToken);
        return (success, latestCompatible, latestInstalled);
    }

    public async Task<bool> InstallAsync(Version version, CancellationToken cancellationToken = default)
    {
        try
        {
            using var packageStream = new MemoryStream();
            var destinationPath = Path.Combine(_downloadPath, version.ToString());
            var resource = await _nugetRepository.GetResourceAsync<FindPackageByIdResource>(cancellationToken);

            var success = await resource.CopyNupkgToStreamAsync(
                id: _nugetPackageName,
                version: new NuGetVersion(version.ToString()),
                destination: packageStream,
                cacheContext: _nugetCache,
                logger: NullLogger.Instance,
                cancellationToken: cancellationToken
            );

            if (!success || cancellationToken.IsCancellationRequested)
            {
                return false;
            }

            using var packageReader = new PackageArchiveReader(packageStream);
            await packageReader.CopyFilesAsync(destinationPath, packageReader.GetFiles(), ExtractFile, NullLogger.Instance, cancellationToken);
            _logger.LogInformation("Installed {PackageName} version {Version} to {Path}", _nugetPackageName, version, destinationPath);

            return true;
        }
        catch (Exception e)
        {
            _logger.LogError("Failed to download {PackageName} {Version} to {Path}, {Exception}", _nugetPackageName, version, _downloadPath, e.Message);
            return false;
        }
    }

    public async Task<bool> RemoveAsync(Version version, CancellationToken cancellationToken)
    {
        var installDirectory = Path.Combine(_downloadPath, version.ToString());
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
            _logger.LogInformation("Removed {PackageName} version {Version} from {Path}", _nugetPackageName, version, _downloadPath);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove {PackageName} {Version} from {Path}", _nugetPackageName, version, _downloadPath);
            return false;
        }
    }

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
        return runtimeVersion >= Dashboard.MinimumRuntimeVersion && version.Major >= runtimeVersion.Major;
    }
}