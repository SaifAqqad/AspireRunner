using AspireRunner.Core;
using AspireRunner.Core.Extensions;
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

DotnetCli dotnet;
try
{
    dotnet = new DotnetCli(new ConsoleLogger<DotnetCli>(arguments.Verbose));
}
catch (Exception e)
{
    logger.LogError("An error occurred while initializing the dotnet CLI, {Error}", e.Message);
    return ReturnCodes.DotnetCliError;
}

var dashboardOptions = BuildOptions(arguments);
logger.LogDebug("Dashboard options: {DashboardOptions}", JsonSerializer.Serialize(dashboardOptions));

var aspireDashboardManager = new AspireDashboardManager(dotnet, nugetHelper, new ConsoleLogger<AspireDashboardManager>(arguments.Verbose));

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

AspireDashboardOptions BuildOptions(Arguments args)
{
    var useHttps = args.OtlpHttps ?? args.UseHttps ?? true;
    var browserTelemetryEnabled = !string.IsNullOrWhiteSpace(args.AllowBrowserTelemetry);
    var corsConfigured = !string.IsNullOrWhiteSpace(args.CorsAllowedOrigins) || !string.IsNullOrWhiteSpace(args.CorsAllowedHeaders);

    var aspireDashboardOptions = new AspireDashboardOptions
    {
        Frontend = new FrontendOptions
        {
            AuthMode = args.UseAuth ? FrontendAuthMode.BrowserToken : FrontendAuthMode.Unsecured,
            EndpointUrls = OptionsExtensions.BuildLocalUrl(args.DashboardPort, args.DashboardHttps ?? args.UseHttps ?? true)
        },
        Otlp = new OtlpOptions
        {
            PrimaryApiKey = args.OtlpKey,
            Cors = browserTelemetryEnabled || corsConfigured ? new OtlpCorsOptions() : null,
            AuthMode = string.IsNullOrWhiteSpace(args.OtlpKey) ? OtlpAuthMode.Unsecured : OtlpAuthMode.ApiKey
        },
        Runner = new RunnerOptions
        {
            LaunchBrowser = args.LaunchBrowser,
            AutoUpdate = args.AutoUpdate ?? true,
            PreferredVersion = args.PreferredVersion,
            SingleInstanceHandling = args.AllowMultipleInstances ? SingleInstanceHandling.Ignore : SingleInstanceHandling.ReplaceExisting
        }
    };

    if (args.OtlpPort is > 0 and <= 65535)
    {
        aspireDashboardOptions.Otlp.GrpcEndpointUrl = OptionsExtensions.BuildLocalUrl(args.OtlpPort, useHttps);
    }

    if (args.OtlpHttpPort is > 0 and <= 65535 || browserTelemetryEnabled)
    {
        aspireDashboardOptions.Otlp.HttpEndpointUrl = OptionsExtensions.BuildLocalUrl(args.OtlpHttpPort ?? OtlpOptions.DefaultOtlpHttpPort, useHttps);
    }

    if (aspireDashboardOptions.Otlp.Cors is not null)
    {
        aspireDashboardOptions.Otlp.Cors.AllowedHeaders = args.CorsAllowedHeaders;
        aspireDashboardOptions.Otlp.Cors.AllowedOrigins = args.CorsAllowedOrigins ?? args.AllowBrowserTelemetry ?? "*";
    }

    return aspireDashboardOptions;
}