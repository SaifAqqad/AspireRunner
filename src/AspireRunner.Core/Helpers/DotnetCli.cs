using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace AspireRunner.Core.Helpers;

public partial class DotnetCli
{
    public const string DataFolderName = ".dotnet";

    public static readonly string ExecutableName = PlatformHelper.AsExecutable("dotnet");

    private readonly ILogger<DotnetCli> _logger;

    public string CliPath { get; }

    public string DataPath { get; }

    public string Executable { get; }

    public DotnetCli(ILogger<DotnetCli> logger)
    {
        _logger = logger;
        CliPath = GetCliPath();
        DataPath = GetOrCreateDataPath();
        Executable = Path.Combine(CliPath, ExecutableName);
    }

    /// <summary>
    /// Returns all installed runtimes.
    /// </summary>
    /// <returns>A tuple array containing the name and version of each installed runtime</returns>
    public async Task<(string Name, Version Version)[]> GetInstalledRuntimesAsync()
    {
        var (runtimesOutput, _) = await ProcessHelper.GetAsync(Executable, ["--list-runtimes"], workingDir: CliPath);
        if (string.IsNullOrWhiteSpace(runtimesOutput))
        {
            return [];
        }

        return runtimesOutput.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => RuntimeOutputRegex.Match(s))
            .Where(m => m.Success)
            .Select(m => (Name: m.Groups[1].Value, Version: new Version(m.Groups[2].Value)))
            .ToArray();
    }

    /// <summary>
    /// Ensures the existence of the dotnet data folder (<c>~/.dotnet</c>.) and returns its path.
    /// </summary>
    /// <returns>The path to the dotnet data folder.</returns>
    private string GetOrCreateDataPath()
    {
        var dataFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), DataFolderName);
        if (Directory.Exists(dataFolderPath))
        {
            return dataFolderPath;
        }

        _logger.LogTrace("Creating dotnet data folder at {Path}", dataFolderPath);
        Directory.CreateDirectory(dataFolderPath);
        return dataFolderPath;
    }

    /// <summary>
    /// Returns the path to the dotnet CLI.
    /// <br/>
    /// If the <c>DOTNET_HOST_PATH</c> environment variable is set, it will be used, otherwise the system's <c>PATH</c> will be checked for a dotnet CLI.
    /// </summary>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-environment-variables#dotnet_host_path"/>
    private string GetCliPath()
    {
        var dotnetPath = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH");
        if (!string.IsNullOrWhiteSpace(dotnetPath) && File.Exists(dotnetPath))
        {
            _logger.LogTrace("Using dotnet CLI from DOTNET_HOST_PATH environment variable");
            return Path.GetDirectoryName(dotnetPath)!;
        }

        var paths = PlatformHelper.GetPaths();
        foreach (var path in paths)
        {
            dotnetPath = Path.Combine(path, ExecutableName);
            if (File.Exists(dotnetPath))
            {
                _logger.LogTrace("Using dotnet CLI from PATH environment variable, {Path}", dotnetPath);
                return path;
            }
        }

        throw new ApplicationException("The dotnet CLI was not found in PATH or DOTNET_HOST_PATH environment variables");
    }

    #region Regex

#if NET7_0_OR_GREATER
    private static readonly Regex RuntimeOutputRegex = BuildRuntimeOutputRegex();

    [GeneratedRegex(@"(.+?) ([\d\-_.\w]+?) \[(.+)\]", RegexOptions.Compiled)]
    private static partial Regex BuildRuntimeOutputRegex();
#else
    private static readonly Regex RuntimeOutputRegex = new(@"(.+?) ([\d\-_.\w]+?) \[(.+)\]", RegexOptions.Compiled);
#endif

    #endregion
}