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
    private readonly SourceCacheContext _cache;
    private readonly SourceRepository _repository;
    private readonly ILogger<AspireDashboardInstaller> _logger;

    public AspireDashboardInstaller(ILogger<AspireDashboardInstaller>? logger)
    {
        _cache = new SourceCacheContext();
        _runnerPath = AspireDashboardManager.GetRunnerPath();
        _nugetPackageName = $"{SdkName}.{PlatformHelper.Rid()}";
        _logger = logger ?? NullLogger<AspireDashboardInstaller>.Instance;

        var repoUrl = EnvironmentVariables.NugetRepoUrl;
        if (string.IsNullOrWhiteSpace(repoUrl))
        {
            repoUrl = DefaultRepoUrl;
        }

        _repository = Repository.Factory.GetCoreV3(repoUrl);
    }

    public async Task<(bool Success, Version? Installed, Version Latest)> EnsureLatestAsync(CancellationToken cancellationToken = default)
    {
        var latestRuntimeVersion = (await GetCompatibleRuntimesAsync()).Max();

        var availableVersions = await GetAvailableVersionsAsync(cancellationToken: cancellationToken);
        if (availableVersions.Length == 0)
        {
            throw new ApplicationException("No versions of the Aspire Dashboard are available");
        }

        var latestCompatible = availableVersions
                .Where(v => IsRuntimeCompatible(v, latestRuntimeVersion!))
                .Max()
            ?? availableVersions.First(); // Fallback to the latest version

        var latestInstalled = AspireDashboardManager.GetInstalledVersions().Max();
        if (latestInstalled == latestCompatible)
        {
            // Dashboard is up-to-date
            return (true, latestInstalled, latestCompatible);
        }

        var success = await InstallAsync(latestCompatible, cancellationToken);
        return (success, latestInstalled, latestCompatible);
    }

    /// <summary>
    /// Downloads and installs the dashboard to the specified destination path.
    /// </summary>
    /// <param name="version">The version of the dashboard</param>
    /// <param name="destinationPath">The destination path to extract the package contents in</param>
    /// <returns><c>true</c> if the dashboard was installed successfully, <c>false</c> otherwise</returns>
    public async Task<bool> InstallAsync(Version version, CancellationToken cancellationToken = default)
    {
        try
        {
            using var packageStream = new MemoryStream();
            var resource = await _repository.GetResourceAsync<FindPackageByIdResource>(cancellationToken);

            var destinationPath = Path.Combine(_runnerPath, version.ToString());
            _logger.LogTrace("Downloading {PackageName} {Version} to {DestinationPath}", _nugetPackageName, version, destinationPath);

            var success = await resource.CopyNupkgToStreamAsync(
                _nugetPackageName,
                new NuGetVersion(version.ToString()),
                packageStream,
                _cache,
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
    /// Returns the available dashboard versions in descending order (newest to oldest).
    /// </summary>
    /// <param name="includePreRelease">Whether to include pre-release versions, defaults to false</param>
    public async Task<Version[]> GetAvailableVersionsAsync(bool includePreRelease = false, CancellationToken cancellationToken = default)
    {
        var resource = await _repository.GetResourceAsync<FindPackageByIdResource>(cancellationToken);
        var metadata = await resource.GetAllVersionsAsync(_nugetPackageName, _cache, NullLogger.Instance, CancellationToken.None);

        return metadata
            .Select(v => new Version(v.ToFullString(), true))
            .Where(v => includePreRelease || !v.IsPreRelease)
            .OrderByDescending(v => v)
            .ToArray();
    }

    private static bool IsRuntimeCompatible(Version version, Version runtimeVersion)
    {
        return runtimeVersion >= AspireDashboard.MinimumRuntimeVersion && version.Major >= runtimeVersion.Major;
    }

    private static async Task<Version[]> GetCompatibleRuntimesAsync()
    {
        return (await DotnetCli.GetInstalledRuntimesAsync())
            .Where(r => r.Name is AspireDashboard.RequiredRuntimeName && r.Version >= AspireDashboard.MinimumRuntimeVersion)
            .Select(r => r.Version)
            .ToArray();
    }

    private string ExtractFile(string sourcefile, string targetpath, Stream filestream)
    {
        try
        {
            _logger.LogTrace("Extracting {SourceFile} to {TargetPath}", sourcefile, targetpath);

            // Ensure the directory exists
            Directory.CreateDirectory(Path.GetDirectoryName(targetpath)!);

            using var file = File.Create(targetpath);
            filestream.CopyTo(file);

            return targetpath;
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to extract {SourceFile} to {TargetPath}, {Exception}", sourcefile, targetpath, ex.Message);
            throw;
        }
    }
}