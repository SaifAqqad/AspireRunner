﻿using Microsoft.Extensions.Options;
using System.Text.Json.Serialization;

namespace AspireRunner;

/// <summary>
/// Configuration options used by the Aspire Dashboard.
/// <see href="https://github.com/dotnet/aspire/tree/v8.0.0-preview.6.24214.1/src/Aspire.Dashboard/Configuration"/> 
/// </summary>
public class AspireDashboardOptions : IOptions<AspireDashboardOptions>
{
    public string ApplicationName { get; set; } = "Aspire";

    public OtlpOptions Otlp { get; set; } = new();

    public FrontendOptions Frontend { get; set; } = new();

    public TelemetryLimitOptions? TelemetryLimits { get; set; }

    [JsonIgnore]
    public AspireDashboardOptions Value => this;
}

public class OtlpOptions
{
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
    /// The OTLP endpoint. This endpoint hosts an OTLP service and receives telemetry. Securing the dashboard with HTTPS is recommended.
    /// </summary>
    public string EndpointUrl { get; set; } = "https://localhost:4317";
}

public sealed class FrontendOptions
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
    /// Tooling that wants to automate logging in with browser token authentication can specify a token and open a browser with the token in the query string.
    /// A new token should be generated each time the dashboard is launched.
    /// </summary>
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