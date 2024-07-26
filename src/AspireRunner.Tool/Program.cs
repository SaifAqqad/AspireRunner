using AspireRunner.Core;
using AspireRunner.Core.Helpers;
using AspireRunner.Tool;
using CommandLine;
using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;
using System.Text.Json;

var logger = new ConsoleLogger<Program>(verbose: false);
logger.LogInformation(Bold().Magenta("Aspire Dashboard Runner"));

var argsResult = Parser.Default.ParseArguments<Arguments>(args);
if (argsResult.Errors.Any() || argsResult.Value is null)
{
    switch (argsResult.Errors.FirstOrDefault())
    {
        case HelpRequestedError or VersionRequestedError:
            return ReturnCodes.Success;
        default:
            logger.LogError("Invalid arguments");
            return ReturnCodes.InvalidArguments;
    }
}

var arguments = argsResult.Value;
logger.Verbose = arguments.Verbose;
logger.LogDebug("Arguments: {@Arguments}", arguments);

var nugetHelper = new NugetHelper(new ConsoleLogger<NugetHelper>(arguments.Verbose));
var dotnet = new DotnetCli(new ConsoleLogger<DotnetCli>(arguments.Verbose));
if (!await dotnet.InitializeAsync())
{
    logger.LogError("Could not find the dotnet CLI, make sure it is installed and available in the PATH");
    return ReturnCodes.DotnetCliError;
}

var protocol = arguments.UseHttps is true or null ? "https" : "http";
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
        LaunchBrowser = arguments.LaunchBrowser,
        AutoDownload = arguments.AutoDownload ?? true,
        SingleInstanceHandling = arguments.AllowMultipleInstances ? SingleInstanceHandling.Ignore : SingleInstanceHandling.ReplaceExisting
    }
};

if (logger.IsEnabled(LogLevel.Debug))
{
    logger.LogDebug("Dashboard options: {@DashboardOptions}", JsonSerializer.Serialize(dashboardOptions));
}

var aspireDashboardManager = new AspireDashboardManager(dotnet, nugetHelper, new ConsoleLogger<AspireDashboardManager>(arguments.Verbose));
await aspireDashboardManager.InitializeAsync();

var (isInstalled, _) = await aspireDashboardManager.IsInstalledAsync();
if (!isInstalled && !dashboardOptions.Runner.AutoDownload)
{
    logger.LogError($"""
                     The Aspire Dashboard is not installed.

                     Pass the option {Bold("--auto-download")} to automatically download and install the Aspire Dashboard.
                     Alternatively, Run '{Bold("dotnet workload install aspire")}' to install the aspire workload through the dotnet CLI.
                     """);

    return ReturnCodes.AspireInstallationError;
}

try
{
    var aspireDashboard = await aspireDashboardManager.GetDashboardAsync(dashboardOptions, new ConsoleLogger<AspireDashboard>(arguments.Verbose));
    aspireDashboard.DashboardStarted += url => logger.LogInformation(Green("The Aspire Dashboard is ready at {Url}"), url);
    aspireDashboard.OtlpEndpointReady += endpoint => logger.LogInformation(Green("The OTLP/{Protocol} endpoint is ready at {Url}"), endpoint.Protocol, endpoint.Url);

    var stopHandler = (PosixSignalContext _) => aspireDashboard.Stop();
    using var sigInt = PosixSignalRegistration.Create(PosixSignal.SIGINT, stopHandler);
    using var sigTerm = PosixSignalRegistration.Create(PosixSignal.SIGTERM, stopHandler);

    aspireDashboard.Start();
    await aspireDashboard.WaitForExitAsync();

    logger.LogDebug("Aspire Dashboard exited, Errors = {HasErrors}", aspireDashboard.HasErrors);
    return aspireDashboard.HasErrors ? ReturnCodes.AspireDashboardError : ReturnCodes.Success;
}
catch (Exception e)
{
    logger.LogError("An error occurred while starting the Aspire Dashboard, {Error}", e.Message);
    return ReturnCodes.AspireDashboardError;
}