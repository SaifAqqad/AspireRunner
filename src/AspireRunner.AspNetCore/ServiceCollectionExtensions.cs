using Microsoft.Extensions.DependencyInjection;

namespace AspireRunner.AspNetCore;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAspireDashboard(this IServiceCollection services, Action<DashboardOptions>? configureOptions = null)
    {
        services.AddOptions<DashboardOptions>()
            .Configure(options => configureOptions?.Invoke(options));

        services.AddSingleton<IDashboardFactory, DashboardFactory>();
        services.AddHostedService<AspireDashboardService>();

        return services;
    }

    public static IServiceCollection AddAspireDashboard(this IServiceCollection services, DashboardOptions options)
    {
        ArgumentNullException.ThrowIfNull(options, nameof(options));
        return services.AddAspireDashboard(options.CloneTo);
    }

    private static void CloneTo(this DashboardOptions srcOptions, DashboardOptions destOptions)
    {
        destOptions.ApplicationName = srcOptions.ApplicationName;
        destOptions.Runner = srcOptions.Runner with { };
        destOptions.Frontend = srcOptions.Frontend with { };
        destOptions.Otlp = srcOptions.Otlp with { };
        destOptions.TelemetryLimits = srcOptions.TelemetryLimits != null ? srcOptions.TelemetryLimits with { } : null;
    }
}