﻿using CommandLine;

namespace AspireRunner.Tool;

public record Arguments
{
    [Option('b', "browser", HelpText = "Launch the dashboard in the default browser")]
    public bool LaunchBrowser { get; set; }

    [Option('p', "port", Default = 18888, HelpText = "The port the dashboard will be available on")]
    public int DashboardPort { get; init; }

    [Option('a', "auth", HelpText = "Use browser token authentication for the dashboard")]
    public bool UseAuth { get; init; }

    [Option('s', "https", HelpText = "Use HTTPS instead of HTTP, this applies to both the dashboard and the OTLP server, Enabled by default")]
    public bool? UseHttps { get; init; }

    [Option("dashboard-https", HelpText = "Use HTTPS instead of HTTP for the dashboard, overrides the global HTTPS option")]
    public bool? DashboardHttps { get; init; }

    [Option("otlp-port", Default = 4317, HelpText = "The port the OTLP/gRPC server will listen on, can be disabled by passing 0")]
    public int OtlpPort { get; init; }

    [Option("otlp-http-port", HelpText = "The port the OTLP/HTTP server will listen on, by default, only the gRPC server is started")]
    public int? OtlpHttpPort { get; set; }

    [Option("otlp-key", HelpText = "The API key to use for the OTLP server")]
    public string? OtlpKey { get; set; }

    [Option("otlp-https", HelpText = "Use HTTPS instead of HTTP for the OTLP/gRPC and OTLP/HTTP endpoints, overrides the global HTTPS option")]
    public bool? OtlpHttps { get; set; }

    [Option("allow-browsers", HelpText = "Shorthand for enabling browser telemetry, this will enable the OTLP/HTTP endpoint (default port 4318) and configure cors-origins to the passed value or *")]
    public string? AllowBrowserTelemetry { get; set; }

    [Option("cors-origins", HelpText = "The allowed origins for CORS requests, separated by a comma, wildcard (*) can be used to allow any domain")]
    public string? CorsAllowedOrigins { get; set; }

    [Option("cors-headers", HelpText = "The allowed headers for CORS requests, separated by a comma")]
    public string? CorsAllowedHeaders { get; set; }

    [Option('m', "multiple", HelpText = "Allow running multiple instances of the dashboard, if this isn't passed, existing instances will be replaced")]
    public bool AllowMultipleInstances { get; set; }

    [Option("auto-update", HelpText = "Automatically update the dashboard to the latest version, Enabled by default")]
    public bool? AutoUpdate { get; set; }

    [Option("dashboard-version", HelpText = "The preferred version of the dashboard to use/download, will fallback to the latest version if it's invalid")]
    public string? PreferredVersion { get; set; }

    [Option('v', "verbose", HelpText = "Enable verbose logging")]
    public bool Verbose { get; set; }
}