using Microsoft.Extensions.Options;
using System.Text.Json.Serialization;

namespace AspireRunner;

/// <summary>
/// Configuration options used by the Aspire Dashboard.
/// <see href="https://github.com/dotnet/aspire/tree/v8.0.0-preview.6.24214.1/src/Aspire.Dashboard/Configuration"/> 
/// </summary>
public class AspireDashboardOptions : IOptions<AspireDashboardOptions>
{
    public OtlpOptions Otlp { get; set; } = new OtlpOptions();

    public FrontendOptions Frontend { get; set; } = new FrontendOptions();

    public TelemetryLimitOptions? TelemetryLimits { get; set; }

    [JsonIgnore]
    public AspireDashboardOptions Value => this;
}

public class OtlpOptions
{
    public string? PrimaryApiKey { get; set; }

    public string? SecondaryApiKey { get; set; }

    public OtlpAuthMode? AuthMode { get; set; }

    public string? EndpointUrl { get; set; }
}

public sealed class FrontendOptions
{
    public string? EndpointUrls { get; set; }

    public FrontendAuthMode? AuthMode { get; set; }

    public string? BrowserToken { get; set; }
}

public sealed class TelemetryLimitOptions
{
    public int MaxLogCount { get; set; } = 10_000;

    public int MaxTraceCount { get; set; } = 10_000;

    public int MaxMetricsCount { get; set; } = 50_000;

    public int MaxAttributeCount { get; set; } = 128;

    public int MaxAttributeLength { get; set; } = int.MaxValue;

    public int MaxSpanEventCount { get; set; } = int.MaxValue;
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