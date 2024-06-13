using System.Runtime.InteropServices;

namespace AspireRunner.Core.Helpers;

public static class PlatformHelper
{
    private static readonly string[] LinuxUrlOpeners =
    [
        "sensible-browser",
        "x-www-browser",
        "xdg-open",
        "gnome-open",
        "kde-open",
        "exo-open",
        "open"
    ];

    private static bool? _isWsl;

    public static string Rid { get; }

    public static string OsIdentifier { get; }

    static PlatformHelper()
    {
        OsIdentifier = GetOsIdentifier();
        Rid = $"{OsIdentifier}-{RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant()}";
    }

    public static string AsExecutable(string fileName)
    {
        return OsIdentifier is "win" ? $"{fileName}.exe" : fileName;
    }

    public static bool IsWsl()
    {
        if (_isWsl.HasValue)
        {
            return _isWsl.Value;
        }

        if (OsIdentifier is not "linux")
        {
            return false;
        }

        var command = Cli.Wrap("uname")
            .WithArguments(["-r"])
            .WithValidation(CommandResultValidation.None)
            .ExecuteBufferedAsync()
            .ConfigureAwait(false)
            .GetAwaiter()
            .GetResult();

        if (command is not { ExitCode: 0 })
        {
            return false;
        }

        var isWsl = command.StandardOutput.Contains("microsoft-standard-WSL2", StringComparison.OrdinalIgnoreCase);
        _isWsl = isWsl;

        return isWsl;
    }

    public static (string Executable, string[] Arguments) GetUrlOpener(string url)
    {
        if (OsIdentifier is "win" || IsWsl())
        {
            var cmdEscapedUrl = url.Replace("&", "^&").Replace(@"""", @"""""");
            return ("cmd.exe", ["/c", $"start {cmdEscapedUrl}"]);
        }

        if (OsIdentifier is "osx")
        {
            return ("open", [url]);
        }

        var availableOpeners = new Dictionary<string, string>();
        foreach (var path in GetPaths())
        {
            foreach (var opener in LinuxUrlOpeners)
            {
                var fullPath = Path.Combine(path, opener);
                if (File.Exists(fullPath))
                {
                    availableOpeners[opener] = fullPath;
                }
            }
        }

        if (availableOpeners.Count == 0)
        {
            throw new InvalidOperationException("No suitable URL opener found");
        }

        var openerName = LinuxUrlOpeners.First(o => availableOpeners.ContainsKey(o));

        // setsid is used to detach the launched process from the runner
        return ("setsid", [openerName, url]);
    }

    /// <summary>
    /// Returns all paths in the system's <c>PATH</c> environment variable.
    /// </summary>
    /// <returns>A string array containing all paths in the system's <c>PATH</c> environment variable.</returns>
    /// <remarks>When running inside WSL, windows paths (like <c>/mnt/c/*</c>) will be excluded to avoid conflicts with windows executables</remarks>
    public static string[] GetPaths()
    {
        var pathEnv = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrWhiteSpace(pathEnv))
        {
            return [];
        }

        var paths = pathEnv.Split(Path.PathSeparator);
        if (IsWsl())
        {
            // exclude wsl paths to avoid conflicts
            paths = paths.Where(p => !p.Contains("/mnt/c/")).ToArray();
        }

        return paths;
    }

    private static string GetOsIdentifier()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return "win";
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return "linux";
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return "osx";
        }

        return "unknown";
    }
}