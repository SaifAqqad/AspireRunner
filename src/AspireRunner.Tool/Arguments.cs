using CommandLine;

namespace AspireRunner.Tool;

[Verb("run", isDefault: true, aliases: ["launch"], HelpText = "Launch the Aspire dashboard")]
public record RunArguments
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

    [Option('m', "multiple", HelpText = "Allow running multiple instances of the dashboard, if this isn't passed, existing instances will be replaced")]
    public bool AllowMultipleInstances { get; set; }

    [Option('r', "runtime-version", HelpText = "The version of the .NET runtime to use")]
    public string? RuntimeVersion { get; set; }

    [Option('v', "verbose", HelpText = "Enable verbose logging")]
    public bool Verbose { get; set; }
}

[Verb("install", HelpText = "Download and install the Aspire dashboard")]
public class InstallArguments
{
    [Option('r', "runtime-version", HelpText = "The version of the .NET runtime to use")]
    public string? RuntimeVersion { get; set; }

    [Option('v', "verbose", HelpText = "Enable verbose logging")]
    public bool Verbose { get; set; }
}

[Verb("uninstall", HelpText = "Uninstall the Aspire dashboard")]
public class UninstallArguments
{
    [Option('v', "verbose", HelpText = "Enable verbose logging")]
    public bool Verbose { get; set; }
}