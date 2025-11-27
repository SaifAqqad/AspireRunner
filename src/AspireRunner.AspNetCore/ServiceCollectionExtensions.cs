using Microsoft.Extensions.DependencyInjection;

namespace AspireRunner.AspNetCore;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAspireDashboard(this IServiceCollection services, Action<DashboardOptions>? configureOptions = null)
    {
        services.AddOptions<DashboardOptions>()
            .Configure(options => configureOptions?.Invoke(options));

        services.AddSingleton<IDashboardFactory, DashboardFactory>();
        services.AddHostedService<AspireRunnerService>();

        return services;
    }

    public static IServiceCollection AddAspireDashboard(this IServiceCollection services, DashboardOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return services.AddAspireDashboard(options.CloneTo);
    }

    private static void CloneTo(this DashboardOptions srcOptions, DashboardOptions destOptions)
    {
        destOptions.Mcp = srcOptions.Mcp with { };
        destOptions.Otlp = srcOptions.Otlp with { };
        destOptions.Runner = srcOptions.Runner with { };
        destOptions.Frontend = srcOptions.Frontend with { };
        destOptions.ApplicationName = srcOptions.ApplicationName;
        destOptions.TelemetryLimits = srcOptions.TelemetryLimits != null ? srcOptions.TelemetryLimits with { } : null;
    }
}