using AspireRunner.Core;
using AspireRunner.Core.Extensions;
using AspireRunner.Core.Helpers;
using AspireRunner.Tool;
using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;
using System.Text.Json;

try
{
    return await RunTool(args);
}
catch (Exception ex)
{
    if (ex is not ConsoleRunnerException runnerException)
    {
        runnerException = new ConsoleRunnerException
        {
            InnerException = ex,
            ReturnCode = ReturnCodes.RunnerError,
            FormattedMessage = $"An error occurred: {ex.Message}"
        };
    }

    runnerException.LogAndExit();
}

return ReturnCodes.Success;

async Task<int> RunTool(string[] strings)
{
    var logger = new ConsoleLogger<Program>(verbose: false);
    logger.LogTitle(Magenta("Aspire Dashboard Runner"));

    var arguments = Arguments.Parse(strings);

    logger.Verbose = arguments.Verbose;
    logger.LogDebug("Arguments: {@Arguments}", arguments);

    var nugetHelper = new NugetHelper(new ConsoleLogger<NugetHelper>(arguments.Verbose));
    var dotnet = GetDotnetCli(arguments);

    var dashboardOptions = BuildOptions(arguments);
    logger.LogDebug("Dashboard options: {DashboardOptions}", JsonSerializer.Serialize(dashboardOptions));

    var aspireDashboardManager = new AspireDashboardManager(dotnet, nugetHelper, new ConsoleLogger<AspireDashboardManager>(arguments.Verbose));
    var aspireDashboard = await GetDashboardAsync(aspireDashboardManager, dashboardOptions, arguments);

    aspireDashboard.DashboardStarted += url => logger.LogInformation(Green("The Aspire Dashboard is ready at {Url}"), url);
    aspireDashboard.OtlpEndpointReady += endpoint => logger.LogInformation(Green("The OTLP/{Protocol} endpoint is ready at {Url}"), endpoint.Protocol, endpoint.Url);

    var stopHandler = (PosixSignalContext _) => aspireDashboard.Stop();
    using var sigInt = PosixSignalRegistration.Create(PosixSignal.SIGINT, stopHandler);
    using var sigTerm = PosixSignalRegistration.Create(PosixSignal.SIGTERM, stopHandler);

    try
    {
        aspireDashboard.Start();
        await aspireDashboard.WaitForExitAsync();

        logger.LogDebug("Aspire Dashboard exited, Errors = {HasErrors}", aspireDashboard.HasErrors);
        return aspireDashboard.HasErrors ? ReturnCodes.AspireDashboardError : ReturnCodes.Success;
    }
    catch (Exception e)
    {
        throw new ConsoleRunnerException
        {
            FormattedMessage = $"An error occurred while starting the Aspire Dashboard: {e.Message}",
            ReturnCode = ReturnCodes.AspireDashboardError
        };
    }
}

AspireDashboardOptions BuildOptions(Arguments args)
{
    var useHttps = args.OtlpHttps ?? args.UseHttps ?? true;
    var corsConfigured = !string.IsNullOrWhiteSpace(args.CorsAllowedOrigins) || !string.IsNullOrWhiteSpace(args.CorsAllowedHeaders);
    var browserTelemetryEnabled = args.OtlpHttpPort is > 0 and <= 65535 || corsConfigured;

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
            Cors = browserTelemetryEnabled ? new OtlpCorsOptions() : null,
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

    if (browserTelemetryEnabled)
    {
        aspireDashboardOptions.Otlp.HttpEndpointUrl = OptionsExtensions.BuildLocalUrl(args.OtlpHttpPort ?? OtlpOptions.DefaultOtlpHttpPort, useHttps);
    }

    if (aspireDashboardOptions.Otlp.Cors is not null)
    {
        aspireDashboardOptions.Otlp.Cors.AllowedHeaders = args.CorsAllowedHeaders;
        aspireDashboardOptions.Otlp.Cors.AllowedOrigins = args.CorsAllowedOrigins ?? "*";
    }

    return aspireDashboardOptions;
}

DotnetCli GetDotnetCli(Arguments args)
{
    try
    {
        return new DotnetCli(new ConsoleLogger<DotnetCli>(args.Verbose));
    }
    catch (Exception e)
    {
        throw new ConsoleRunnerException
        {
            InnerException = e,
            ReturnCode = ReturnCodes.DotnetCliError,
            FormattedMessage = $"An error occurred while initializing the dotnet CLI: {e.Message}"
        };
    }
}

async Task<AspireDashboard> GetDashboardAsync(AspireDashboardManager aspireDashboardManager, AspireDashboardOptions dashboardOptions, Arguments arguments)
{
    try
    {
        var aspireDashboard = await aspireDashboardManager.GetDashboardAsync(dashboardOptions, new ConsoleLogger<AspireDashboard>(arguments.Verbose));
        return aspireDashboard;
    }
    catch (Exception e)
    {
        throw new ConsoleRunnerException
        {
            InnerException = e,
            ReturnCode = ReturnCodes.AspireInstallationError,
            FormattedMessage = $"An error occurred while initializing the Aspire Dashboard: {e.Message}"
        };
    }
}