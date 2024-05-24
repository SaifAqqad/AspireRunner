using Microsoft.Extensions.Logging;
using NuGet.Common;
using NuGet.Packaging;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace AspireRunner.Core.Helpers;

public class NugetHelper(ILogger<NugetHelper> logger)
{
    private const string RepoUrl = "https://api.nuget.org/v3/index.json";

    private readonly SourceCacheContext _cache = new();
    private readonly SourceRepository _repository = Repository.Factory.GetCoreV3(RepoUrl);

    public async Task<Version[]> GetPackageVersionsAsync(string packageName)
    {
        var resource = await _repository.GetResourceAsync<FindPackageByIdResource>();
        var metadata = await resource.GetAllVersionsAsync(packageName, _cache, NullLogger.Instance, CancellationToken.None);

        return metadata.Select(v => new Version(v.ToFullString(), true)).OrderDescending().ToArray();
    }

    public async Task<bool> DownloadPackageAsync(string packageName, Version version, string destinationPath)
    {
        using var packageStream = new MemoryStream();
        var resource = await _repository.GetResourceAsync<FindPackageByIdResource>();

        logger.LogTrace("Downloading {PackageName} {Version} to {DestinationPath}", packageName, version, destinationPath);

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

    private string ExtractFile(string sourcefile, string targetpath, Stream filestream)
    {
        logger.LogTrace("Extracting {SourceFile} to {TargetPath}", sourcefile, targetpath);

        using var file = File.Create(targetpath);
        filestream.CopyTo(file);

        return targetpath;
    }
}