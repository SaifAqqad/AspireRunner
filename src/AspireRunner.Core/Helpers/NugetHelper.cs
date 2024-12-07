using Microsoft.Extensions.Logging;
using NuGet.Common;
using NuGet.Packaging;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace AspireRunner.Core.Helpers;

public class NugetHelper
{
    private const string DefaultRepoUrl = "https://api.nuget.org/v3/index.json";

    private readonly SourceCacheContext _cache;
    private readonly SourceRepository _repository;
    private readonly ILogger<NugetHelper> _logger;

    public NugetHelper(ILogger<NugetHelper> logger)
    {
        _logger = logger;
        _cache = new SourceCacheContext();

        var repoUrl = Environment.GetEnvironmentVariable("NUGET_REPO_URL");
        if (string.IsNullOrWhiteSpace(repoUrl))
        {
            repoUrl = DefaultRepoUrl;
        }

        _repository = Repository.Factory.GetCoreV3(repoUrl);
    }

    /// <summary>
    /// Returns the available versions of a package in descending order (latest to oldest).
    /// </summary>
    /// <param name="packageName">The name of the package</param>
    /// <param name="includePreRelease">Whether to include pre-release versions, defaults to false</param>
    public async Task<Version[]> GetPackageVersionsAsync(string packageName, bool includePreRelease = false)
    {
        var resource = await _repository.GetResourceAsync<FindPackageByIdResource>();
        var metadata = await resource.GetAllVersionsAsync(packageName, _cache, NullLogger.Instance, CancellationToken.None);

        return metadata
            .Select(v => new Version(v.ToFullString(), true))
            .Where(v => includePreRelease || !v.IsPreRelease)
            .OrderByDescending(v => v)
            .ToArray();
    }

    /// <summary>
    /// Downloads a package to the specified destination path.
    /// </summary>
    /// <param name="packageName">The name of the package</param>
    /// <param name="version">The version of the package</param>
    /// <param name="destinationPath">The destination path to extract the package contents</param>
    /// <returns>True if the package was downloaded successfully, otherwise false</returns>
    public async Task<bool> DownloadPackageAsync(string packageName, Version version, string destinationPath)
    {
        try
        {
            using var packageStream = new MemoryStream();
            var resource = await _repository.GetResourceAsync<FindPackageByIdResource>();

            _logger.LogTrace("Downloading {PackageName} {Version} to {DestinationPath}", packageName, version, destinationPath);

            var success = await resource.CopyNupkgToStreamAsync(
                packageName,
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
            _logger.LogError("Failed to download {PackageName} {Version} to {DestinationPath}, {Exception}", packageName, version, destinationPath, e.Message);
            return false;
        }
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