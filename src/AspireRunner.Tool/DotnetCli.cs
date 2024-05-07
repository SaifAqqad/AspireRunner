using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace AspireRunner.Tool;

public partial class DotnetCli
{
    public const string DataFolderName = ".dotnet";
#if windows
    public const string Executable = "dotnet.exe";
#else
    public const string Executable = "dotnet";
#endif

    public string CliPath { get; }

    public string DataPath { get; }

    public string? SdkPath { get; private set; }

    private DotnetCli(string path)
    {
        CliPath = path;
        
        var dataFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), DataFolderName);
        if (!Directory.Exists(dataFolderPath))
        {
            Directory.CreateDirectory(dataFolderPath);
        }

        DataPath = dataFolderPath;
    }

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

    public string? GetSdkPath()
    {
        var sdksOutput = Run("--list-sdks");
        var sdks = sdksOutput.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

        var sdkPath = sdks.Select(s => SdkOutputRegex().Match(s))
            .Where(m => m.Success)
            .Select(m => (Version: m.Groups[1].Value, Path: m.Groups[2].Value))
            .MaxBy(s => new Version(s.Version)).Path;

        return SdkPath = Path.GetDirectoryName(Path.TrimEndingDirectorySeparator(sdkPath));
    }

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

    public static DotnetCli? TryCreate()
    {
        var cliPath = GetCliPath();
        if (cliPath == null)
        {
            return null;
        }

        var cli = new DotnetCli(cliPath);
        cli.GetSdkPath();

        return cli;
    }

    private static string? GetCliPath()
    {
        var dotnetPath = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH");
        if (!string.IsNullOrWhiteSpace(dotnetPath) && File.Exists(dotnetPath))
        {
            return Path.GetDirectoryName(dotnetPath);
        }

        var paths = GetEnvPath();
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

    private static string[] GetEnvPath()
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

    private static bool IsRunningWsl()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
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

        return output.Contains("Microsoft");
    }

    [GeneratedRegex(@"([\d\.]+)\s+(?:\[(.+)\])?", RegexOptions.Compiled)]
    private static partial Regex SdkOutputRegex();
}