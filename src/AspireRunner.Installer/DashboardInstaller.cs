using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NuGet.Packaging;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using NullLogger = NuGet.Common.NullLogger;

namespace AspireRunner.Installer;

public partial class DashboardInstaller : IDashboardInstaller
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
        TraceFoundRuntimeVersion(latestRuntimeVersion);

        var availableVersions = await GetAvailableVersionsAsync(cancellationToken: cancellationToken);
        TraceFetchedPackageVersions(_nugetPackageName, availableVersions);

        if (availableVersions.Length == 0)
        {
            throw new ApplicationException("No versions of the Aspire Dashboard are available");
        }

        var latestCompatible = availableVersions
                .Where(v => IsRuntimeCompatible(v, latestRuntimeVersion!))
                .Max()
            ?? availableVersions.First(); // Fallback to the latest version

        var installedVersions = Dashboard.GetInstalledVersions();
        TraceFoundInstalledVersions(_nugetPackageName, installedVersions);

        var latestInstalled = installedVersions.Max();
        if (latestInstalled == latestCompatible)
        {
            // Dashboard is up to date
            LogLatestVersionIsAlreadyInstalled(latestCompatible);
            return (true, latestCompatible, latestInstalled);
        }

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
            LogInstalledPackageVersionToPath(_nugetPackageName, version, destinationPath);

            return true;
        }
        catch (Exception e)
        {
            LogFailedToDownloadPackage(_nugetPackageName, version, _downloadPath, e.Message);
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
            LogRemovedPackageVersion(_nugetPackageName, version, _downloadPath);

            return true;
        }
        catch (Exception ex)
        {
            LogFailedToRemovePackage(ex, _nugetPackageName, version, _downloadPath);
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
            TraceExtractingFileToPath(sourceFile, targetPath);

            // Ensure the directory exists
            Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);

            using var file = File.Create(targetPath);
            fileStream.CopyTo(file);

            return targetPath;
        }
        catch (Exception ex)
        {
            LogFailedToExtractFile(ex, sourceFile, targetPath);
            throw;
        }
    }

    private static bool IsRuntimeCompatible(Version version, Version runtimeVersion)
    {
        return runtimeVersion >= Dashboard.MinimumRuntimeVersion && version.Major >= runtimeVersion.Major;
    }
}