using AspireRunner.CommandLine;
using CommandLine;
using CommandLine.Text;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace AspireRunner;

internal static partial class Program
{
    private const string AspireSdkName = "Aspire.Dashboard.Sdk";
    private const string AspireDashboardDll = "Aspire.Dashboard.dll";
    private const string AspireDashboardInstanceFile = "aspire-dashboard.pid";

    public static int Main(string[] args)
    {
        Console.WriteLine("Aspire Dashboard Runner");

        var argsResult = Parser.Default.ParseArguments<Arguments>(args);
        if (argsResult.Errors.Any() || argsResult.Value is null)
        {
            if (argsResult.Errors.FirstOrDefault() is not (HelpRequestedError or VersionRequestedError))
            {
                Console.Error.WriteLine("ERROR: Invalid arguments");
            }

            Console.WriteLine(HelpText.RenderUsageText(argsResult));
            return ReturnCodes.InvalidArguments;
        }

        var arguments = argsResult.Value;
        var dotnet = DotnetCli.TryCreate();
        if (dotnet is null)
        {
            Console.Error.WriteLine("ERROR: Could not find the dotnet CLI, make sure it is installed and available in the PATH");
            return ReturnCodes.DotnetCliError;
        }

        if (dotnet.SdkPath is null)
        {
            Console.Error.WriteLine("ERROR: Could not find the dotnet SDK installation directory");
            return ReturnCodes.DotnetCliError;
        }

        Console.WriteLine($"Found dotnet installation directory at \"{dotnet.SdkPath}\"");

        // Get the first packs folder that contains the Aspire workload
        var packsFolder = dotnet.GetPacksFolders()
            .SelectMany(Directory.GetDirectories)
            .FirstOrDefault(dir => dir.Contains(AspireSdkName));

        if (packsFolder is null)
        {
            Console.Error.WriteLine("""
                                    ERROR: Could not find a dotnet packs folder with the Aspire workload installed

                                    Please make sure the Aspire workload is installed
                                    Run 'dotnet workload install aspire' to install it.
                                    """);

            return ReturnCodes.AspireInstallationError;
        }

        Console.WriteLine($"Found Aspire workload at \"{packsFolder}\"");

        var newestVersion = Directory.GetDirectories(packsFolder)
            .Select(d => new DirectoryInfo(d))
            .MaxBy(d => new Version(d.Name));

        if (newestVersion is null)
        {
            Console.Error.WriteLine($"ERROR: Could not find any versions of Aspire.Dashboard.Sdk in '{packsFolder}'");
            return ReturnCodes.AspireInstallationError;
        }

        if (!arguments.AllowMultipleInstances)
        {
            var previousInstance = GetPreviousInstance(dotnet.DataPath);
            if (previousInstance is not null)
            {
                Console.WriteLine($"Killing existing Aspire Dashboard instance with PID {previousInstance.Id}");
                previousInstance.Kill(true);
            }
        }

        var protocol = arguments.UseHttps ? "https" : "http";
        var aspirePath = Path.Combine(newestVersion.FullName, "tools");
        var aspireConfig = new Dictionary<string, string>
        {
            ["Dashboard__Frontend__AuthMode"] = arguments.UseAuth ? "BrowserToken" : "Unsecured",
            ["Dashboard__Frontend__EndpointUrls"] = $"{protocol}://localhost:{arguments.DashboardPort}",
            ["Dashboard__Otlp__AuthMode"] = string.IsNullOrWhiteSpace(arguments.OtlpKey) ? "Unsecured" : "ApiKey",
            ["Dashboard__Otlp__EndpointUrl"] = $"{protocol}://localhost:{arguments.OtlpPort}",
            ["Dashboard__Otlp__PrimaryApiKey"] = arguments.OtlpKey ?? string.Empty,
        };

        Console.WriteLine($"Running Aspire Dashboard {newestVersion.Name}");
        var process = dotnet.Run(
            arguments: ["exec", Path.Combine(aspirePath, AspireDashboardDll)],
            workingDirectory: aspirePath,
            environement: aspireConfig,
            outputHandler: line => DashboardOutputHandler(line, arguments),
            errorHandler: error => Console.Error.WriteLine($"\t{error}")
        );

        PersistInstance(process, dotnet.DataPath);
        process.WaitForExit();

        return ReturnCodes.Success;
    }

    private static void DashboardOutputHandler(string line, Arguments args)
    {
        Console.WriteLine($"\t{line}");
        if (string.IsNullOrWhiteSpace(line) || !args.LaunchBrowser)
        {
            return;
        }

        // Wait for the authentication token to be printed
        if (args.UseAuth && line.Contains("Now listening on:"))
        {
            return;
        }

        if (UrlRegex().Match(line) is { Success: true } match)
        {
            // Open the dashboard in the default browser
            try
            {
                LaunchBrowser(match.Groups["url"].Value);
            }
            catch
            {
                Console.Error.WriteLine("ERROR: Failed to open the dashboard in the default browser");
            }
        }
    }

    private static void PersistInstance(Process process, string path)
    {
        Console.WriteLine($"Process ID: {process.Id}");
        File.WriteAllText(Path.Combine(path, AspireDashboardInstanceFile), process.Id.ToString());
    }

    private static Process? GetPreviousInstance(string path)
    {
        var instanceFile = Path.Combine(path, AspireDashboardInstanceFile);
        if (!File.Exists(instanceFile) || !int.TryParse(File.ReadAllText(instanceFile), out var pid))
        {
            return null;
        }

        try
        {
            return Process.GetProcessById(pid) is { ProcessName: "dotnet" } p ? p : null;
        }
        catch
        {
            return null;
        }
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

    [GeneratedRegex(@"((?:Login to the dashboard at)|(?:Now listening on:)) +(?<url>https?:\/\/[^\s]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex UrlRegex();
}