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
    private static string? _rid;
    private static string? _osIdentifier;
    private static string? _linuxUrlOpener;

    public static string Rid()
    {
        if (_rid is not null)
        {
            return _rid;
        }

        return _rid = $"{OsIdentifier()}-{RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant()}";
    }

    public static string OsIdentifier()
    {
        if (_osIdentifier is not null)
        {
            return _osIdentifier;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return _osIdentifier = "win";
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return _osIdentifier = "linux";
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return _osIdentifier = "osx";
        }

        return _osIdentifier = "unknown";
    }

    public static string AsExecutable(string fileName)
    {
        return OsIdentifier() is "win" ? $"{fileName}.exe" : fileName;
    }

    public static bool IsWsl()
    {
        if (_isWsl.HasValue)
        {
            return _isWsl.Value;
        }

        if (OsIdentifier() is not "linux")
        {
            return (_isWsl = false).Value;
        }

        var uname = ProcessHelper.Get("uname", ["-r"]);
        if (string.IsNullOrWhiteSpace(uname.Output))
        {
            return false;
        }

        return (_isWsl = uname.Output.Contains("microsoft-standard-WSL2", StringComparison.OrdinalIgnoreCase)).Value;
    }

    /// <summary>
    /// Returns the platform-specific executable and arguments to open a URL.
    /// </summary>
    /// <remarks>
    /// On Linux, the method will try to find a suitable URL opener from the system's <c>PATH</c>.
    /// </remarks>
    public static (string Executable, string[] Arguments)? GetUrlOpener(string url)
    {
        var osIdentifier = OsIdentifier();
        if (osIdentifier is "win" || IsWsl())
        {
            var cmdEscapedUrl = url.Replace("&", "^&").Replace(@"""", @"""""");
            return ("cmd.exe", ["/c", $"start {cmdEscapedUrl}"]);
        }

        if (osIdentifier is "osx")
        {
            return ("open", [url]);
        }

        if (_linuxUrlOpener is not null)
        {
            return ("setsid", [_linuxUrlOpener, url]);
        }

        var envPaths = EnvironmentVariables.Paths;
        _linuxUrlOpener = LinuxUrlOpeners
            .SelectMany(o => envPaths.Select(p => (Name: o, Path: Path.Combine(p, o))))
            .FirstOrDefault(p => File.Exists(p.Path))
            .Name;

        if (_linuxUrlOpener is null)
        {
            return null;
        }

        // setsid is used to detach the launched process from the runner
        return ("setsid", [_linuxUrlOpener, url]);
    }
}