using AspireRunner.Core;
using AspireRunner.Core.Helpers;
using AspireRunner.Tool;
using CommandLine;
using Microsoft.Extensions.Logging;
using System.Text.Json;

var logger = new ConsoleLogger<AspireDashboard>();
logger.LogInformation(Bold().Magenta("Aspire Runner"));

var returnCode = Parser.Default
    .ParseArguments<RunArguments, InstallArguments, UninstallArguments>(args)
    .MapResult(
        (RunArguments arguments) => Launch(arguments, logger),
        (InstallArguments arguments) => Install(arguments, logger),
        (UninstallArguments arguments) => Uninstall(arguments, logger),
        errors =>
        {
            switch (errors.FirstOrDefault())
            {
                case HelpRequestedError or HelpVerbRequestedError or VersionRequestedError:
                    return Task.FromResult(ReturnCodes.Success);
                default:
                    logger.LogError("Invalid arguments");
                    return Task.FromResult(ReturnCodes.InvalidArguments);
            }
        }
    );

return await returnCode;

static async Task<int> Launch(RunArguments arguments, ConsoleLogger<AspireDashboard> logger)
{
    logger.Verbose = arguments.Verbose;
    logger.LogDebug("Arguments: {@Arguments}", arguments);

    var nugetHelper = new NugetHelper(new ConsoleLogger<NugetHelper> { Verbose = arguments.Verbose });
    var dotnet = DotnetCli.TryCreate();
    if (dotnet is null)
    {
        logger.LogError("Could not find the dotnet CLI, make sure it is installed and available in the PATH");
        return ReturnCodes.DotnetCliError;
    }

    var protocol = arguments.UseHttps ? "https" : "http";
    var dashboardOptions = new AspireDashboardOptions
    {
        Frontend = new FrontendOptions
        {
            EndpointUrls = $"{protocol}://localhost:{arguments.DashboardPort}",
            AuthMode = arguments.UseAuth ? FrontendAuthMode.BrowserToken : FrontendAuthMode.Unsecured
        },
        Otlp = new OtlpOptions
        {
            EndpointUrl = $"{protocol}://localhost:{arguments.OtlpPort}",
            AuthMode = string.IsNullOrWhiteSpace(arguments.OtlpKey) ? OtlpAuthMode.Unsecured : OtlpAuthMode.ApiKey,
            PrimaryApiKey = arguments.OtlpKey
        },
        Runner = new RunnerOptions
        {
            PipeOutput = false,
            LaunchBrowser = arguments.LaunchBrowser,
            RuntimeVersion = arguments.RuntimeVersion,
            SingleInstanceHandling = arguments.AllowMultipleInstances ? SingleInstanceHandling.Ignore : SingleInstanceHandling.ReplaceExisting
        }
    };

    logger.LogDebug("Dashboard options: {@DashboardOptions}", JsonSerializer.Serialize(dashboardOptions));

    var aspireDashboard = new AspireDashboard(dotnet, nugetHelper, dashboardOptions, logger);

    Console.CancelKeyPress += (_, _) => aspireDashboard.Stop();
    aspireDashboard.DashboardStarted += url => logger.LogInformation(Green("The Aspire Dashboard is ready at {Url}"), url);

    if (!aspireDashboard.IsInstalled())
    {
        logger.LogError($"""
                         The Aspire Dashboard is not installed.

                         Run '{Bold("aspire-dashboard install")}' to download and install the Aspire Dashboard.
                         Alternatively, Run '{Bold("dotnet workload install aspire")}' to install the aspire workload through the dotnet CLI.
                         """);

        return ReturnCodes.AspireInstallationError;
    }

    await aspireDashboard.StartAsync();
    await aspireDashboard.WaitForExitAsync();

    logger.LogDebug("Aspire Dashboard exited, {HasErrors}", aspireDashboard.HasErrors);
    return aspireDashboard.HasErrors ? ReturnCodes.AspireDashboardError : ReturnCodes.Success;
}

static async Task<int> Install(InstallArguments arguments, ILogger<AspireDashboard> logger)
{
    return ReturnCodes.Success;
}

static async Task<int> Uninstall(UninstallArguments arguments, ILogger<AspireDashboard> logger)
{
    return ReturnCodes.Success;
}