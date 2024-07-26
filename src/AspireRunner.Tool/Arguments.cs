using CommandLine;

namespace AspireRunner.Tool;

public record Arguments
{
    [Option('b', "browser", HelpText = "Launch the dashboard in the default browser")]
    public bool LaunchBrowser { get; set; }

    [Option('p', "port", Default = 18888, HelpText = "The port the dashboard will be available on")]
    public int DashboardPort { get; init; }

    [Option('a', "auth", HelpText = "Use browser token authentication for the dashboard")]
    public bool UseAuth { get; init; }

    [Option('s', "https", HelpText = "Use HTTPS instead of HTTP, this applies to both the dashboard and the OTLP server")]
    public bool? UseHttps { get; init; }

    [Option("dashboard-https", HelpText = "Use HTTPS instead of HTTP for the dashboard")]
    public bool? DashboardHttps { get; init; }

    [Option("otlp-port", Default = 4317, HelpText = "The port the OTLP/gRPC server will listen on")]
    public int OtlpPort { get; init; }

    [Option("otlp-http-port", HelpText = "The port the OTLP/HTTP server will listen on, by default, only the gRPC server is started")]
    public int? OtlpHttpPort { get; set; }

    [Option("otlp-key", HelpText = "The API key to use for the OTLP server")]
    public string? OtlpKey { get; set; }

    [Option("otlp-https", HelpText = "Use HTTPS instead of HTTP for the OTLP/gRPC and OTLP/HTTP endpoints")]
    public bool? OtlpHttps { get; set; }

    [Option('m', "multiple", HelpText = "Allow running multiple instances of the dashboard, if this isn't passed, existing instances will be replaced")]
    public bool AllowMultipleInstances { get; set; }

    [Option('d', "auto-download", HelpText = "Automatically download the dashboard if it's not installed")]
    public bool? AutoDownload { get; set; }

    [Option('v', "verbose", HelpText = "Enable verbose logging")]
    public bool Verbose { get; set; }
}