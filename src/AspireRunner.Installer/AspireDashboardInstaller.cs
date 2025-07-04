using Microsoft.Extensions.Logging;
using NuGet.Common;
using NuGet.Packaging;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace AspireRunner.Installer;

public class AspireDashboardInstaller
{
    private const string SdkName = "Aspire.Dashboard.Sdk";
    private const string DefaultRepoUrl = "https://api.nuget.org/v3/index.json";

    private readonly string _nugetPackageName;
    private readonly SourceCacheContext _cache;
    private readonly SourceRepository _repository;
    private readonly ILogger<AspireDashboardInstaller> _logger;

    public AspireDashboardInstaller(ILogger<AspireDashboardInstaller> logger)
    {
        _logger = logger;
        _cache = new SourceCacheContext();
        _nugetPackageName = $"{SdkName}.{PlatformHelper.Rid()}";

        var repoUrl = EnvironmentVariables.NugetRepoUrl;
        if (string.IsNullOrWhiteSpace(repoUrl))
        {
            repoUrl = DefaultRepoUrl;
        }

        _repository = Repository.Factory.GetCoreV3(repoUrl);
    }


    /// <summary>
    /// Returns the available dashboard versions in descending order (newest to oldest).
    /// </summary>
    /// <param name="includePreRelease">Whether to include pre-release versions, defaults to false</param>
    public async Task<Version[]> GetAvailableVersionsAsync(bool includePreRelease = false)
    {
        var resource = await _repository.GetResourceAsync<FindPackageByIdResource>();
        var metadata = await resource.GetAllVersionsAsync(_nugetPackageName, _cache, NullLogger.Instance, CancellationToken.None);

        return metadata
            .Select(v => new Version(v.ToFullString(), true))
            .Where(v => includePreRelease || !v.IsPreRelease)
            .OrderByDescending(v => v)
            .ToArray();
    }

    /// <summary>
    /// Downloads and installs the dashboard to the specified destination path.
    /// </summary>
    /// <param name="version">The version of the dashboard</param>
    /// <param name="destinationPath">The destination path to extract the package contents in</param>
    /// <returns><c>true</c> if the dashboard was installed successfully, <c>false</c> otherwise</returns>
    public async Task<bool> InstallAsync(Version version, string destinationPath)
    {
        try
        {
            using var packageStream = new MemoryStream();
            var resource = await _repository.GetResourceAsync<FindPackageByIdResource>();

            _logger.LogTrace("Downloading {PackageName} {Version} to {DestinationPath}", _nugetPackageName, version, destinationPath);

            var success = await resource.CopyNupkgToStreamAsync(
                _nugetPackageName,
                new NuGetVersion(version.ToString()),
                packageStream,
                _cache,
                NullLogger.Instance,
                CancellationToken.None
            );

            if (!success)
            {
                return false;
            }

            using var packageReader = new PackageArchiveReader(packageStream);
            await packageReader.CopyFilesAsync(destinationPath, packageReader.GetFiles(), ExtractFile, NullLogger.Instance, CancellationToken.None);

            return true;
        }
        catch (Exception e)
        {
            _logger.LogError("Failed to download {PackageName} {Version} to {DestinationPath}, {Exception}", _nugetPackageName, version, destinationPath, e.Message);
            return false;
        }
    }

    // TODO: Rewrite all of this
    // public async Task<Version?> InstallAsync(Version[] installedRuntimes, Version? version = null)
    // {
    //     if (version == null)
    //     {
    //         version = await FetchLatestVersionAsync(installedRuntimes);
    //     }
    //
    //     var installationPath = Path.Combine(_runnerPath, AspireDashboard.DownloadFolder, version.ToString());
    //     return await _nugetHelper.DownloadPackageAsync(_nugetPackageName, version, installationPath) ? version : null;
    // }
    //
    // public async Task TryUpdateAsync(Version[] installedRuntimes)
    // {
    //     try
    //     {
    //         var availableVersions = await _nugetHelper.GetPackageVersionsAsync(_nugetPackageName);
    //
    //         var installedVersions = Directory.GetDirectories(Path.Combine(_runnerPath, AspireDashboard.DownloadFolder))
    //             .Select(d => new Version(new DirectoryInfo(d).Name, true))
    //             .ToArray();
    //
    //         var latestRuntimeVersion = installedRuntimes.Max();
    //         var latestInstalledVersion = installedVersions.Max();
    //
    //         var latestAvailableVersion = availableVersions
    //                 .Where(v => IsRuntimeCompatible(v, latestRuntimeVersion!))
    //                 .DefaultIfEmpty()
    //                 .Max()
    //             ?? availableVersions.First(); // Fallback to the latest version
    //
    //         if (latestAvailableVersion > latestInstalledVersion)
    //         {
    //             _logger.LogWarning("A newer version of the Aspire Dashboard is available, downloading version {Version}", latestAvailableVersion);
    //
    //             var newVersionFolder = Path.Combine(_runnerPath, AspireDashboard.DownloadFolder, latestAvailableVersion.ToString());
    //             var downloadSuccessful = await _nugetHelper.DownloadPackageAsync(_nugetPackageName, latestAvailableVersion, newVersionFolder);
    //             if (downloadSuccessful)
    //             {
    //                 _logger.LogInformation("Successfully updated the Aspire Dashboard to version {Version}", latestAvailableVersion);
    //             }
    //             else
    //             {
    //                 _logger.LogError("Failed to update the Aspire Dashboard, falling back to the installed version");
    //                 Directory.Delete(newVersionFolder, true);
    //             }
    //         }
    //         else
    //         {
    //             _logger.LogInformation("The Aspire Dashboard is up to date");
    //         }
    //     }
    //     catch
    //     {
    //         _logger.LogError("Failed to update the Aspire Dashboard, falling back to the installed version");
    //     }
    // }
    //
    // private async Task<Version> FetchLatestVersionAsync(Version[] installedRuntimes)
    // {
    //     var availableVersions = await _nugetHelper.GetPackageVersionsAsync(_nugetPackageName);
    //     if (availableVersions.Length == 0)
    //     {
    //         throw new ApplicationException("No versions of the Aspire Dashboard are available");
    //     }
    //
    //     var latestRuntimeVersion = installedRuntimes.Max();
    //     var versionToDownload = availableVersions
    //             .Where(v => IsRuntimeCompatible(v, latestRuntimeVersion!))
    //             .DefaultIfEmpty()
    //             .Max()
    //         ?? availableVersions.First(); // Fallback to the latest version
    //
    //     return versionToDownload;
    // }

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