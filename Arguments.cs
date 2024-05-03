using CommandLine;

namespace AspireRunner;

public record Arguments
{
    [Option('p', "port", Default = 18888, HelpText = "The port the dashboard will listen on")]
    public int DashboardPort { get; init; }

    [Option('o', "otlp-port", Default = 4317, HelpText = "The port the OTLP server will listen on")]
    public int OtlpPort { get; init; }

    [Option('s', "https", Default = true, HelpText = "Use HTTPS instead of HTTP")]
    public bool UseHttps { get; init; }

    [Option('a', "auth", HelpText = "Use browser token authentication for the dashboard")]
    public bool UseAuth { get; init; }

    [Option('b', "browser", HelpText = "Launch the dashboard in the default browser")]
    public bool LaunchBrowser { get; set; }
}