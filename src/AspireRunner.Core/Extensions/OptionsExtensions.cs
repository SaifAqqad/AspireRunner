using System.Text.Json;

namespace AspireRunner.Core.Extensions;

public static class OptionsExtensions
{
    public static Dictionary<string, string?> ToEnvironmentVariables(this DashboardOptions options)
    {
        var envVars = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        var jsonObject = JsonSerializer.SerializeToNode(options)!.AsObject().Flatten();

        foreach (var property in jsonObject)
        {
            if (property.Value is null || property.Value.GetValueKind() is JsonValueKind.Null)
            {
                continue;
            }

            var envVarName = property.Key
                .Replace("$", "Dashboard")
                .Replace(".", "__")
                .ToUpperInvariant();

            envVars[envVarName] = property.Value.ToString();
        }

        if (envVars.TryGetValue("DASHBOARD__OTLP__ENDPOINTURL", out var otlpUrl))
        {
            envVars["DOTNET_DASHBOARD_OTLP_ENDPOINT_URL"] = otlpUrl;
        }

        if (envVars.TryGetValue("DASHBOARD__OTLP__HTTPENDPOINTURL", out var httpOtlpUrl))
        {
            envVars["DOTNET_DASHBOARD_OTLP_HTTP_ENDPOINT_URL"] = httpOtlpUrl;
        }

        if (options.Otlp.EndpointUrl is null && options.Otlp.HttpEndpointUrl is null)
        {
            envVars["DOTNET_DASHBOARD_OTLP_ENDPOINT_URL"] = UrlHelper.BuildLocalUrl(OtlpOptions.DefaultOtlpGrpcPort);
        }

        if (envVars.TryGetValue("DASHBOARD__FRONTEND__ENDPOINTURLS", out var frontendUrls))
        {
            envVars["ASPNETCORE_URLS"] = frontendUrls;
        }

        return envVars;
    }
}