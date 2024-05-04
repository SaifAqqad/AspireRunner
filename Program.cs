using AspireRunner.CommandLine;
using AspireRunner.Extensions;
using CommandLine;
using CommandLine.Text;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace AspireRunner;

internal static partial class Program
{
    private const string AspireSdkName = "Aspire.Dashboard.Sdk";
    private const string AspireDashboardDll = "Aspire.Dashboard.dll";

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

        var packsFolder = dotnet.GetPacksFolders()
            .FirstOrDefault(packs => Directory
                .GetDirectories(packs)
                .Any(p => p.Contains(AspireSdkName))
            );

        if (packsFolder is null)
        {
            Console.Error.WriteLine("ERROR: Could not find a dotnet packs folder with the Aspire workload installed");
            return ReturnCodes.AspireInstallationError;
        }

        Console.WriteLine($"Found Aspire workload at \"{packsFolder}\"");

        var aspireDashboardPack = Directory.GetDirectories(packsFolder)
            .FirstOrDefault(p => p.Contains(AspireSdkName));

        if (aspireDashboardPack is null)
        {
            Console.Error.WriteLine($"""
                                     ERROR: Could not find pack '{AspireSdkName}' in '{packsFolder}'

                                     Please make sure the Aspire workload is installed
                                     Run 'dotnet workload install aspire' to install it.
                                     """);

            return ReturnCodes.AspireInstallationError;
        }

        var newestVersion = Directory.GetDirectories(aspireDashboardPack)
            .Select(d => new DirectoryInfo(d))
            .MaxBy<DirectoryInfo, Version>(d => d.Name.ParseVersion());

        if (newestVersion is null)
        {
            Console.Error.WriteLine($"ERROR: Could not find any versions of Aspire.Dashboard.Sdk in '{aspireDashboardPack}'");
            return ReturnCodes.AspireInstallationError;
        }

        Console.WriteLine($"Running Aspire Dashboard {newestVersion.Name}");
        var aspirePath = Path.Combine(newestVersion.FullName, "tools");
        var protocol = arguments.UseHttps ? "https" : "http";
        var authType = arguments.UseAuth ? "BrowserToken" : "Unsecured";

        var aspireConfig = new Dictionary<string, string>
        {
            ["Dashboard__Otlp__AuthMode"] = authType,
            ["Dashboard__Frontend__AuthMode"] = authType,
            ["Dashboard__Otlp__EndpointUrl"] = $"{protocol}://localhost:{arguments.OtlpPort}",
            ["Dashboard__Frontend__EndpointUrls"] = $"{protocol}://localhost:{arguments.DashboardPort}"
        };

        var process = dotnet.Run(
            arguments: ["exec", Path.Combine(aspirePath, AspireDashboardDll)],
            workingDirectory: aspirePath,
            environement: aspireConfig,
            outputHandler: line => DashboardOutputHandler(line, arguments.LaunchBrowser),
            errorHandler: error => Console.Error.WriteLine($"\t{error}")
        );

        Console.WriteLine($"Process ID: {process.Id}"); 
        process.WaitForExit();

        return ReturnCodes.Success;
    }

    private static void DashboardOutputHandler(string line, bool launchBrowser)
    {
        Console.WriteLine($"\t{line}");
        if (string.IsNullOrWhiteSpace(line))
        {
            return;
        }

        if (launchBrowser && DashboardSuccessMessage().Match(line) is { Success: true } match)
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

    [GeneratedRegex("Now listening on: (?<url>.+)", RegexOptions.Compiled)]
    private static partial Regex DashboardSuccessMessage();
}