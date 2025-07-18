using System.Text.Json.Serialization;

namespace AspireRunner.Core;

/// <summary>
/// Configuration options used by the Aspire Dashboard.
/// <see href="https://github.com/dotnet/aspire/tree/v9.0.0/src/Aspire.Dashboard/Configuration">Aspire.Dashboard/Configuration</see>
/// </summary>
public sealed record DashboardOptions
{
    /// <summary>
    /// The application name to be displayed in the dashboard UI.
    /// </summary>
    public string ApplicationName { get; set; } = "AspireRunner";

    public OtlpOptions Otlp { get; set; } = new();

    public FrontendOptions Frontend { get; set; } = new();

    public RunnerOptions Runner { get; set; } = new();

    public TelemetryLimitOptions? TelemetryLimits { get; set; }
}

public sealed record OtlpOptions
{
    public const int DefaultOtlpGrpcPort = 4317;
    public const int DefaultOtlpHttpPort = 4318;

    /// <summary>
    /// Specifies the primary API key. The API key can be any text, but a value with at least 128 bits of entropy is recommended. This value is required if auth mode is API key.
    /// </summary>
    public string? PrimaryApiKey { get; set; }

    /// <summary>
    /// Specifies the secondary API key. The API key can be any text, but a value with at least 128 bits of entropy is recommended.
    /// This value is optional. If a second API key is specified then the incoming x-otlp-api-key header value can match either the primary or secondary key.
    /// </summary>
    public string? SecondaryApiKey { get; set; }

    /// <summary>
    /// Can be set to ApiKey or Unsecured. Unsecured should only be used during local development. It's not recommended when hosting the dashboard publicly or in other settings.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public OtlpAuthMode AuthMode { get; set; } = OtlpAuthMode.Unsecured;

    /// <summary>
    /// The OTLP/gRPC endpoint. This endpoint hosts an OTLP service to receive telemetry.
    /// </summary>
    public string? EndpointUrl { get; set; }

    /// <summary>
    /// The OTLP/HTTP endpoint. This endpoint hosts an OTLP service to receive telemetry. OTLP/gRPC and OTLP/HTTP endpoints can be used simultaneously.
    /// </summary>
    public string? HttpEndpointUrl { get; set; }

    /// <summary>
    /// The CORS configuration for the OTLP/HTTP endpoint.
    /// This must be configured to allow browsers to send telemetry data to the OTLP/HTTP endpoint.
    /// <br/>
    /// <see href="https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/dashboard/enable-browser-telemetry">Enable browser telemetry</see>
    /// </summary>
    public OtlpCorsOptions? Cors { get; set; }
}

public sealed record OtlpCorsOptions
{
    /// <summary>
    /// Specifies the allowed origins for CORS. The value is a comma-delimited string and can include the * wildcard to allow any domain
    /// </summary>
    public string? AllowedOrigins { get; set; }

    /// <summary>
    /// A comma-delimited string representing the allowed headers for CORS
    /// </summary>
    public string? AllowedHeaders { get; set; }
}

public sealed record FrontendOptions
{
    /// <summary>
    /// One or more HTTP endpoints through which the dashboard frontend is served. The frontend endpoint is used to view the dashboard in a browser.
    /// </summary>
    public string EndpointUrls { get; set; } = "https://localhost:18888;http://localhost:18889";

    /// <summary>
    /// Can be set to BrowserToken or Unsecured. Unsecured should only be used during local development. It's not recommended when hosting the dashboard publicly or in other settings.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public FrontendAuthMode AuthMode { get; set; } = FrontendAuthMode.Unsecured;

    /// <summary>
    /// Specifies the browser token. If the browser token isn't specified, then the dashboard will generate one.
    /// </summary>
    public string? BrowserToken { get; set; }
}

public sealed record TelemetryLimitOptions
{
    public int MaxLogCount { get; set; } = 10_000;

    public int MaxTraceCount { get; set; } = 10_000;

    public int MaxMetricsCount { get; set; } = 50_000;

    public int MaxAttributeCount { get; set; } = 128;

    public int MaxAttributeLength { get; set; } = int.MaxValue;

    public int MaxSpanEventCount { get; set; } = int.MaxValue;
}

public sealed record RunnerOptions
{
    /// <summary>
    /// Pipe the output of the Aspire Dashboard process to the logger.
    /// </summary>
    public bool PipeOutput { get; set; }

    /// <summary>
    /// Automatically launch the dashboard in the default browser.
    /// </summary>
    public bool LaunchBrowser { get; set; }

    /// <summary>
    /// Defines how existing instances should be handled when starting the dashboard.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public SingleInstanceHandling SingleInstanceHandling { get; set; } = SingleInstanceHandling.WarnAndExit;

    /// <summary>
    /// Automatically update the dashboard to the latest version.
    /// </summary>
    public bool AutoUpdate { get; set; } = true;

    /// <summary>
    /// Specifies the preferred version of the dashboard to run/download.
    /// </summary>
    /// <remarks>Can be a specifc version (e.g. <c>9.2.0</c>), or a valid semver range specifier (e.g. <c>9.x.x</c> or <c>>=9.1.0</c></remarks>
    public string? PreferredVersion { get; set; }

    /// <summary>
    /// Attempt to restart the dashboard if it exits unexpectedly. (i.e., not using the runner)
    /// </summary>
    public bool RestartOnFailure { get; set; } = true;

    /// <summary>
    /// The number of times to retry running the dashboard if it fails to start.
    /// </summary>
    public int RunRetryCount { get; set; } = 0;

    /// <summary>
    /// The delay in seconds between retry attempts to run the dashboard.
    /// </summary>
    public int RunRetryDelay { get; set; } = 5;
}

public enum FrontendAuthMode
{
    Unsecured,
    BrowserToken
}

public enum OtlpAuthMode
{
    Unsecured,
    ApiKey
}

public enum SingleInstanceHandling
{
    /// <summary>
    /// Logs a warning and exits if an existing instance is found.
    /// </summary>
    WarnAndExit = 0,

    /// <summary>
    /// Disables checking for running instances of the Aspire Dashboard.
    /// <br/>
    /// New instances will fail to start if an existing one is using the same port
    /// </summary>
    Ignore = 1,

    /// <summary>
    /// Kills the existing instance and starts a new one.
    /// </summary>
    ReplaceExisting = 2
}