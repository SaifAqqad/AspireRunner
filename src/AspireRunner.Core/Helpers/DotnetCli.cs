using System.Text.RegularExpressions;
using SystemPath = System.IO.Path;

namespace AspireRunner.Core.Helpers;

public static partial class DotnetCli
{
    public static readonly string DataPath = SystemPath.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".dotnet");

    public static readonly string ExecutableName = PlatformHelper.AsExecutable("dotnet");

    public static readonly string Path = GetCliPath();

    public static readonly string Executable = SystemPath.Combine(Path, ExecutableName);

    /// <summary>
    /// Returns all installed runtimes.
    /// </summary>
    /// <returns>A tuple array containing the name and version of each installed runtime</returns>
    public static async Task<(string Name, Version Version)[]> GetInstalledRuntimesAsync()
    {
        var (runtimesOutput, _) = await ProcessHelper.GetAsync(Executable, ["--list-runtimes"], workingDir: Path);
        if (string.IsNullOrWhiteSpace(runtimesOutput))
        {
            return [];
        }

        return runtimesOutput.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => RuntimeOutputRegex.Match(s))
            .Where(m => m.Success)
            .Select(m => (Name: m.Groups[1].Value, Version: new Version(m.Groups[2].Value, true)))
            .ToArray();
    }

    /// <summary>
    /// Returns the path to the dotnet CLI.
    /// <br/>
    /// If the <c>DOTNET_HOST_PATH</c> environment variable is set, it will be used, otherwise the system's <c>PATH</c> will be checked for a dotnet CLI.
    /// </summary>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-environment-variables#dotnet_host_path"/>
    private static string GetCliPath()
    {
        var dotnetPath = EnvironmentVariables.DotnetHostPath;
        if (!string.IsNullOrWhiteSpace(dotnetPath) && File.Exists(dotnetPath))
        {
            return SystemPath.GetDirectoryName(dotnetPath)!;
        }

        foreach (var path in EnvironmentVariables.Paths)
        {
            dotnetPath = SystemPath.Combine(path, ExecutableName);
            if (File.Exists(dotnetPath))
            {
                return path;
            }
        }

        throw new ApplicationException("The dotnet CLI was not found in PATH or DOTNET_HOST_PATH environment variables");
    }

    #region Regex

    private static readonly Regex RuntimeOutputRegex = BuildRuntimeOutputRegex();

    [GeneratedRegex(@"(.+?) ([\d\-_.\w]+?) \[(.+)\]", RegexOptions.Compiled)]
    private static partial Regex BuildRuntimeOutputRegex();

    #endregion
}