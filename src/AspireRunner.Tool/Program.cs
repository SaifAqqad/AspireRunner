using AspireRunner.Core;
using AspireRunner.Tool;
using CommandLine;
using CommandLine.Text;
using Microsoft.Extensions.Logging;

var logger = new ConsoleLogger<AspireDashboard>();

logger.LogInformation(Bold().Green("Aspire Dashboard Runner"));

var argsResult = Parser.Default.ParseArguments<Arguments>(args);
if (argsResult.Errors.Any() || argsResult.Value is null)
{
    if (argsResult.Errors.FirstOrDefault() is not (HelpRequestedError or VersionRequestedError))
    {
        logger.LogError("Invalid arguments");
    }

    logger.LogInformation("{Usage}", HelpText.RenderUsageText(argsResult));
    return ReturnCodes.InvalidArguments;
}

var arguments = argsResult.Value;
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
        SingleInstanceHandling = arguments.AllowMultipleInstances ? SingleInstanceHandling.Ignore : SingleInstanceHandling.ReplaceExisting
    }
};

var aspireDashboard = new AspireDashboard(dotnet, dashboardOptions, logger);
aspireDashboard.DashboardStarted += url => logger.LogInformation(Green("The Aspire Dashboard is ready at {Url}"), url);

if (!aspireDashboard.IsInstalled())
{
    logger.LogError($"""
                     Failed to locate the Aspire Dashboard installation.

                     Please make sure the Aspire workload is installed
                     Run '{Bold("dotnet workload install aspire")}' to install it.
                     """);

    return ReturnCodes.AspireInstallationError;
}

aspireDashboard.Start();
aspireDashboard.WaitForExit();

return aspireDashboard.HasErrors ? ReturnCodes.AspireDashboardError : ReturnCodes.Success;