using System.Diagnostics;

namespace AspireRunner.Core;

public partial class DotnetCli
{
    public const string DataFolderName = ".dotnet";

    public static readonly string Executable = OperatingSystem.IsWindows() ? "dotnet.exe" : "dotnet";

    public string CliPath { get; private init; } = null!;

    public string DataPath { get; private init; } = null!;

    public string? SdkPath { get; private set; }

    private DotnetCli() { }

    /// <summary>
    /// Creates a new instance of the <see cref="AspireRunner.Core.DotnetCli"/> class.
    /// </summary>
    /// <returns>
    /// A new instance of the <see cref="AspireRunner.Core.DotnetCli"/> class or null if the dotnet CLI wasn't found.
    /// </returns>
    public static DotnetCli? TryCreate()
    {
        var cliPath = GetCliPath();
        if (cliPath == null)
        {
            return null;
        }

        var cli = new DotnetCli
        {
            CliPath = cliPath,
            DataPath = GetOrCreateDataPath()
        };

        cli.SdkPath = cli.GetSdkPath();
        return cli;
    }

    /// <summary>
    /// Runs the dotnet CLI with the specified arguments, waits for completion and returns the full output.
    /// </summary>
    /// <param name="arguments">The arguments to pass to the dotnet CLI.</param>
    /// <returns>The full output of the dotnet CLI.</returns>
    /// <exception cref="InvalidOperationException">The dotnet CLI process failed to start.</exception>
    public string Run(string arguments)
    {
        var process = Process.Start(new ProcessStartInfo(Path.Combine(CliPath, Executable), arguments)
        {
            RedirectStandardOutput = true,
            UseShellExecute = false
        });

        if (process == null)
        {
            throw new InvalidOperationException("Failed to start dotnet process");
        }

        process.WaitForExit();
        return process.StandardOutput.ReadToEnd();
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
    public Process Run(string[] arguments, string? workingDirectory = null, IDictionary<string, string>? environement = null, Action<string>? outputHandler = null, Action<string>? errorHandler = null)
    {
        var processStartInfo = new ProcessStartInfo(Path.Combine(CliPath, Executable), arguments)
        {
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            WorkingDirectory = workingDirectory,
            WindowStyle = ProcessWindowStyle.Hidden
        };

        if (environement != null)
        {
            foreach (var (key, value) in environement)
            {
                processStartInfo.Environment[key] = value;
            }
        }

        var process = Process.Start(processStartInfo);
        if (process == null)
        {
            throw new InvalidOperationException("Failed to start dotnet process");
        }

        Console.CancelKeyPress += (_, _) => process.Kill(true);
        if (outputHandler != null)
        {
            process.OutputDataReceived += (_, e) =>
            {
                if (e.Data != null)
                {
                    outputHandler(e.Data);
                }
            };

            process.BeginOutputReadLine();
        }

        if (errorHandler != null)
        {
            process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data != null)
                {
                    errorHandler(e.Data);
                }
            };

            process.BeginErrorReadLine();
        }

        return process;
    }

    /// <summary>
    /// Returns all the dotnet packs folders available.
    /// </summary>
    /// <returns>An array containing the paths of all the dotnet packs folders available.</returns>
    public string[] GetPacksFolders()
    {
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
    public string[] GetInstalledWorkloads()
    {
        var workloadsOutput = Run("workload list");
        var workloadsMatch = TableContentRegex().Match(workloadsOutput);
        if (!workloadsMatch.Success)
        {
            return [];
        }

        return workloadsMatch.Value
            .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
            .Select(row => TableColumnSeperatorRegex().Split(row, 3)[0])
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .ToArray();
    }

    /// <summary>
    /// Returns the path of the latest SDK installed.
    /// </summary>
    /// <returns>The path of the latest SDK installed or null if no SDK was found.</returns>
    private string? GetSdkPath()
    {
        var sdksOutput = Run("--list-sdks");
        var sdks = sdksOutput.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => SdkOutputRegex().Match(s))
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
    private static string GetOrCreateDataPath()
    {
        var dataFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), DataFolderName);
        if (!Directory.Exists(dataFolderPath))
        {
            Directory.CreateDirectory(dataFolderPath);
        }

        return dataFolderPath;
    }

    /// <summary>
    /// Returns the path to the dotnet CLI.
    /// <br/>
    /// If the <c>DOTNET_HOST_PATH</c> environment variable is set, it will be used, otherwise the system's <c>PATH</c> will be checked for a dotnet CLI.
    /// </summary>
    /// <returns>The path to the dotnet CLI or null if it wasn't found.</returns>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-environment-variables#dotnet_host_path"/>
    private static string? GetCliPath()
    {
        var dotnetPath = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH");
        if (!string.IsNullOrWhiteSpace(dotnetPath) && File.Exists(dotnetPath))
        {
            return Path.GetDirectoryName(dotnetPath);
        }

        var paths = GetEnvPaths();
        foreach (var path in paths)
        {
            dotnetPath = Path.Combine(path, Executable);
            if (File.Exists(dotnetPath))
            {
                return path;
            }
        }

        return null;
    }

    /// <summary>
    /// Returns all paths in the system's <c>PATH</c> environment variable.
    /// </summary>
    /// <returns>A string array containing all paths in the system's <c>PATH</c> environment variable.</returns>
    /// <remarks>When running inside WSL, windows paths (like <c>/mnt/c/*</c>) will be excluded to avoid conflicts with windows dotnet installations</remarks>
    private static string[] GetEnvPaths()
    {
        var pathEnv = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrWhiteSpace(pathEnv))
        {
            return [];
        }

        var paths = pathEnv.Split(Path.PathSeparator);
        if (IsRunningWsl())
        {
            // exclude wsl paths to avoid conflicts
            paths = paths.Where(p => !p.Contains("/mnt/c/")).ToArray();
        }

        return paths;
    }


    /// <summary>
    /// Checks if the current process is running inside WSL.
    /// </summary>
    /// <returns>True if the current process is running inside WSL, false otherwise.</returns>
    private static bool IsRunningWsl()
    {
        if (!OperatingSystem.IsLinux())
        {
            return false;
        }

        var process = Process.Start(new ProcessStartInfo("uname", "-r")
        {
            RedirectStandardOutput = true,
            UseShellExecute = false
        });

        if (process == null)
        {
            return false;
        }

        process.WaitForExit();
        var output = process.StandardOutput.ReadToEnd();

        return output.Contains("microsoft-standard-WSL2", StringComparison.OrdinalIgnoreCase);
    }
}