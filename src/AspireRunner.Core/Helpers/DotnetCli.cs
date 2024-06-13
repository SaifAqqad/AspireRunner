using Microsoft.Extensions.Logging;

namespace AspireRunner.Core.Helpers;

public partial class DotnetCli(ILogger<DotnetCli> logger)
{
    public const string DataFolderName = ".dotnet";

    public static readonly string Executable = PlatformHelper.AsExecutable("dotnet");

    public string CliPath { get; private set; } = null!;

    public string DataPath { get; private set; } = null!;

    public string? SdkPath { get; private set; }

    public bool Initialized { get; private set; }

    /// <summary>
    /// Initializes the <see cref="DotnetCli"/> instance.
    /// </summary>
    /// <returns>
    /// True if the initialization was successful, false otherwise.
    /// </returns>
    public async Task<bool> InitializeAsync()
    {
        if (Initialized)
        {
            return true;
        }

        try
        {
            CliPath = GetCliPath();
            Initialized = true;

            DataPath = GetOrCreateDataPath();
            SdkPath = await GetSdkPathAsync();
            return Initialized;
        }
        catch (Exception ex)
        {
            logger.LogError("Failed to initialize DotnetCli, {Error}", ex.Message);
            return Initialized = false;
        }
    }

    /// <summary>
    /// Runs the dotnet CLI with the specified arguments, waits for completion and returns the full output.
    /// </summary>
    /// <param name="arguments">The arguments to pass to the dotnet CLI.</param>
    /// <returns>The full output of the dotnet CLI.</returns>
    /// <exception cref="InvalidOperationException">The dotnet CLI process failed to start.</exception>
    public async Task<string> GetAsync(string arguments)
    {
        if (!Initialized)
        {
            throw new InvalidOperationException("The DotnetCli instance has not been initialized.");
        }

        var commandResult = await Cli.Wrap(Path.Combine(CliPath, Executable))
            .WithWorkingDirectory(CliPath)
            .WithArguments(arguments)
            .ExecuteBufferedAsync();

        return commandResult.StandardOutput;
    }

    /// <summary>
    /// Runs the dotnet CLI with the specified arguments.
    /// </summary>
    /// <param name="arguments">The arguments to pass to the dotnet CLI.</param>
    /// <param name="workingDirectory">The working directory for the process.</param>
    /// <param name="environement">The environment variables to set for the process.</param>
    /// <param name="outputHandler">An action that receives the output of the process (stdout).</param>
    /// <param name="errorHandler">An action that receives the error output of the process (stderr).</param>
    /// <returns>The started process.</returns>
    /// <exception cref="InvalidOperationException">The dotnet CLI process failed to start.</exception>
    public CommandTask<CommandResult> RunAsync(string[] arguments, string? workingDirectory = null, IReadOnlyDictionary<string, string?>? environement = null, Action<string>? outputHandler = null, Action<string>? errorHandler = null)
    {
        if (!Initialized)
        {
            throw new InvalidOperationException("The DotnetCli instance has not been initialized.");
        }

        var command = Cli.Wrap(Path.Combine(CliPath, Executable))
            .WithWorkingDirectory(workingDirectory ?? CliPath)
            .WithArguments(arguments, true);

        if (environement is { Count: > 0 })
        {
            command = command.WithEnvironmentVariables(environement);
        }

        if (outputHandler != null)
        {
            command = command.WithStandardOutputPipe(PipeTarget.ToDelegate(outputHandler));
        }

        if (errorHandler != null)
        {
            command = command.WithStandardErrorPipe(PipeTarget.ToDelegate(errorHandler));
        }

        return command.ExecuteAsync();
    }

    /// <summary>
    /// Returns all the dotnet packs folders available.
    /// </summary>
    /// <returns>An array containing the paths of all the dotnet packs folders available.</returns>
    public string[] GetPacksFolders()
    {
        if (!Initialized)
        {
            throw new InvalidOperationException("The DotnetCli instance has not been initialized.");
        }

        var folders = new List<string>();

        if (SdkPath != null)
        {
            var sdkPacksFolder = Path.Combine(SdkPath, "packs");
            if (Directory.Exists(sdkPacksFolder))
            {
                folders.Add(sdkPacksFolder);
            }
        }

        var dataPacksFolder = Path.Combine(DataPath, "packs");
        if (Directory.Exists(dataPacksFolder))
        {
            folders.Add(dataPacksFolder);
        }

        return folders.ToArray();
    }

    /// <summary>
    /// Runs <c>dotnet workload list</c> and parses the output to get all installed workloads.
    /// </summary>
    /// <returns>An array containing all installed workloads.</returns>
    public async Task<string[]> GetInstalledWorkloadsAsync()
    {
        if (!Initialized)
        {
            throw new InvalidOperationException("The DotnetCli instance has not been initialized.");
        }

        var workloadsOutput = await GetAsync("workload list");
        var workloadsMatch = TableContentRegex.Match(workloadsOutput);
        if (!workloadsMatch.Success)
        {
            return [];
        }

        return workloadsMatch.Value
            .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
            .Select(row => TableColumnSeperatorRegex.Split(row, 3)[0])
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .ToArray();
    }

    /// <summary>
    /// Returns all installed runtimes.
    /// </summary>
    /// <returns>A tuple array containing the name and version of each installed runtime</returns>
    public async Task<(string Name, Version Version)[]> GetInstalledRuntimesAsync()
    {
        if (!Initialized)
        {
            throw new InvalidOperationException("The DotnetCli instance has not been initialized.");
        }

        var runtimesOutput = await GetAsync("--list-runtimes");
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
    /// Returns the path of the latest SDK installed.
    /// </summary>
    private async Task<string?> GetSdkPathAsync()
    {
        var sdksOutput = await GetAsync("--list-sdks");
        var sdks = sdksOutput.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => SdkOutputRegex.Match(s))
            .Where(m => m.Success)
            .Select(m => (Version: m.Groups[1].Value, Path: m.Groups[2].Value))
            .ToArray();

        if (sdks.Length == 0)
        {
            return null;
        }

        var latestSdk = sdks.MaxBy(s => new Version(s.Version));
        return Path.GetDirectoryName(Path.TrimEndingDirectorySeparator(latestSdk.Path));
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

        logger.LogTrace("Creating dotnet data folder at {Path}", dataFolderPath);
        Directory.CreateDirectory(dataFolderPath);
        return dataFolderPath;
    }

    /// <summary>
    /// Returns the path to the dotnet CLI.
    /// <br/>
    /// If the <c>DOTNET_HOST_PATH</c> environment variable is set, it will be used, otherwise the system's <c>PATH</c> will be checked for a dotnet CLI.
    /// </summary>
    /// <returns>The path to the dotnet CLI or null if it wasn't found.</returns>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-environment-variables#dotnet_host_path"/>
    private string GetCliPath()
    {
        var dotnetPath = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH");
        if (!string.IsNullOrWhiteSpace(dotnetPath) && File.Exists(dotnetPath))
        {
            logger.LogTrace("Using dotnet CLI from DOTNET_HOST_PATH environment variable");
            return Path.GetDirectoryName(dotnetPath)!;
        }

        var paths = PlatformHelper.GetPaths();
        foreach (var path in paths)
        {
            dotnetPath = Path.Combine(path, Executable);
            if (File.Exists(dotnetPath))
            {
                logger.LogTrace("Using dotnet CLI from PATH environment variable, {Path}", dotnetPath);
                return path;
            }
        }

        throw new ApplicationException("The dotnet CLI was not found in PATH or DOTNET_HOST_PATH environment variables");
    }
}