using CommandLine;

namespace AspireRunner.CommandLine;

public record Arguments
{
    [Option('b', "browser", HelpText = "Launch the dashboard in the default browser")]
    public bool LaunchBrowser { get; set; }

    [Option('p', "port", Default = 18888, HelpText = "The port the dashboard will be available on")]
    public int DashboardPort { get; init; }

    [Option('o', "otlp-port", Default = 4317, HelpText = "The port the OTLP server will listen on")]
    public int OtlpPort { get; init; }

    [Option('k', "otlp-key", HelpText = "The API key to use for the OTLP server")]
    public string? OtlpKey { get; set; }

    [Option('s', "https", Default = true, HelpText = "Use HTTPS instead of HTTP, this applies to both the dashboard and the OTLP server")]
    public bool UseHttps { get; init; }

    [Option('a', "auth", HelpText = "Use browser token authentication for the dashboard")]
    public bool UseAuth { get; init; }

    [Option('m', "multiple", HelpText = "Allow running multiple instances of the dashboard")]
    public bool AllowMultipleInstances { get; set; }
}