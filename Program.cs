using CommandLine;
using CommandLine.Text;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace AspireRunner;

internal static partial class Program
{
    private const string AspireSdkName = "Aspire.Dashboard.Sdk";
    private const string AspireDashboardDll = "Aspire.Dashboard.dll";

#if windows
    private const string DotnetExecutable = "dotnet.exe";
#else
    private const string DotnetExecutable = "dotnet";
#endif

    public static int Main(string[] args)
    {
        Console.WriteLine("Aspire Dashboard Runner");

        var argsResult = Parser.Default.ParseArguments<Arguments>(args);
        if (argsResult.Errors.Any() || argsResult.Value is null)
        {
            if (argsResult.Errors.FirstOrDefault()?.Tag != ErrorType.HelpRequestedError)
            {
                Console.Error.WriteLine("ERROR: Invalid arguments");
            }

            Console.WriteLine(HelpText.RenderUsageText(argsResult));
            return -1;
        }

        var arguments = argsResult.Value;
        var dotnetPath = GetDotnetPath();
        if (dotnetPath is null)
        {
            Console.Error.WriteLine("ERROR: Could not find dotnet directory in PATH or DOTNET_HOST_PATH environment variables");
            return -1;
        }

        Console.WriteLine($"Found dotnet directory at \"{dotnetPath}\"");

        var packsFolder = Path.Combine(dotnetPath, "packs");
        var aspireDashboardPack = Directory.GetDirectories(packsFolder)
            .FirstOrDefault(p => p.Contains(AspireSdkName));

        if (aspireDashboardPack is null)
        {
            Console.Error.WriteLine($"""
                                     ERROR: Could not find pack '{AspireSdkName}' in '{packsFolder}'

                                     Please make sure the Aspire workload is installed
                                     Run 'dotnet workload install aspire' to install it.
                                     """);

            return -2;
        }

        var newestVersion = Directory.GetDirectories(aspireDashboardPack)
            .Select(d => new DirectoryInfo(d))
            .MaxBy<DirectoryInfo, Version>(d => ParseVersion(d.Name));

        if (newestVersion is null)
        {
            Console.Error.WriteLine($"ERROR: Could not find any versions of Aspire.Dashboard.Sdk in '{aspireDashboardPack}'");
            return -3;
        }

        Console.WriteLine($"Running Aspire Dashboard {newestVersion.Name}");
        var aspirePath = Path.Combine(newestVersion.FullName, "tools");
        var aspireConfig = new
        {
            OtelPort = arguments.OtlpPort,
            Port = arguments.DashboardPort,
            Protocol = arguments.UseHttps ? "https" : "http",
            AuthMode = arguments.UseAuth ? "BrowserToken" : "Unsecured"
        };

        var process = new Process();
        process.StartInfo.FileName = Path.Combine(dotnetPath, DotnetExecutable);
        process.StartInfo.Arguments = $"exec \"{Path.Combine(aspirePath, AspireDashboardDll)}\"";
        process.StartInfo.WorkingDirectory = aspirePath;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.CreateNoWindow = true;
        process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
        process.StartInfo.Environment["Dashboard__Otlp__AuthMode"] = aspireConfig.AuthMode;
        process.StartInfo.Environment["Dashboard__Frontend__AuthMode"] = aspireConfig.AuthMode;
        process.StartInfo.Environment["Dashboard__Otlp__EndpointUrl"] = $"{aspireConfig.Protocol}://localhost:{aspireConfig.OtelPort}";
        process.StartInfo.Environment["Dashboard__Frontend__EndpointUrls"] = $"{aspireConfig.Protocol}://localhost:{aspireConfig.Port}";

        Console.CancelKeyPress += (_, _) => process.Kill(true);
        process.ErrorDataReceived += (_, e) => Console.Error.WriteLine($"\t{e.Data}");
        process.OutputDataReceived += (_, e) =>
        {
            Console.WriteLine($"\t{e.Data}");
            if (string.IsNullOrWhiteSpace(e.Data))
            {
                return;
            }

            if (arguments.LaunchBrowser && DashboardSuccessMessage().Match(e.Data) is { Success: true } match)
            {
                // Open the dashboard in the default browser
                LaunchBrowser(match.Groups["url"].Value);
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.WaitForExit();
        return 0;
    }

    private static string? GetDotnetPath()
    {
        var dotnetPath = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH");
        if (!string.IsNullOrWhiteSpace(dotnetPath) && File.Exists(dotnetPath))
        {
            return Path.GetDirectoryName(dotnetPath);
        }

        var paths = Environment.GetEnvironmentVariable("PATH")?.Split(Path.PathSeparator) ?? [];
        foreach (var path in paths)
        {
            dotnetPath = Path.Combine(path, DotnetExecutable);
            if (File.Exists(dotnetPath))
            {
                return path;
            }
        }

        return null;
    }

    private static void LaunchBrowser(string url)
    {
#if windows
        Process.Start(new ProcessStartInfo
        {
            UseShellExecute = true,
            FileName = url
        });
#elif linux
        Process.Start(new ProcessStartInfo
        {
            FileName = "xdg-open",
            UseShellExecute = true,
            Arguments = $"\"{url}\""
        });
#elif macos
        Process.Start(new ProcessStartInfo
        {
            FileName = "open",
            UseShellExecute = true,
            Arguments = $"\"{url}\""
        });
#endif
    }

    private static Version ParseVersion(string name)
    {
        var parts = BuildTypeRegex()
            .Replace(name, string.Empty)
            .Split('.', 4, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return new Version(
            int.Parse(parts.ElementAtOrDefault(0) ?? "0"),
            int.Parse(parts.ElementAtOrDefault(1) ?? "0"),
            int.Parse(parts.ElementAtOrDefault(2) ?? "0"),
            int.Parse(parts.ElementAtOrDefault(3)?.Replace(".", string.Empty) ?? "0")
        );
    }

    [GeneratedRegex(@"\-\w+", RegexOptions.Compiled)]
    private static partial Regex BuildTypeRegex();

    [GeneratedRegex("Now listening on: (?<url>.+)", RegexOptions.Compiled)]
    private static partial Regex DashboardSuccessMessage();
}