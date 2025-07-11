using AspireRunner.Core;
using Microsoft.Extensions.DependencyInjection;

namespace AspireRunner.AspNetCore;

public static class AspireDashboardExtensions
{
    public static IServiceCollection AddAspireDashboard(this IServiceCollection services, Action<AspireDashboardOptions>? configureOptions = null)
    {
        services.AddOptions<AspireDashboardOptions>()
            .Configure(options => configureOptions?.Invoke(options));

        services.AddSingleton<AspireDashboardFactory>();
        services.AddHostedService<AspireDashboardService>();

        return services;
    }

    public static IServiceCollection AddAspireDashboard(this IServiceCollection services, AspireDashboardOptions options)
    {
        ArgumentNullException.ThrowIfNull(options, nameof(options));
        return services.AddAspireDashboard(options.CloneTo);
    }

    private static void CloneTo(this AspireDashboardOptions srcOptions, AspireDashboardOptions destOptions)
    {
        destOptions.ApplicationName = srcOptions.ApplicationName;
        destOptions.Runner = srcOptions.Runner with { };
        destOptions.Frontend = srcOptions.Frontend with { };
        destOptions.Otlp = srcOptions.Otlp with { };
        destOptions.TelemetryLimits = srcOptions.TelemetryLimits != null ? srcOptions.TelemetryLimits with { } : null;
    }
}