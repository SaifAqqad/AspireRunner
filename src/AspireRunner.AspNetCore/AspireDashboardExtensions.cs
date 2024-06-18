using AspireRunner.Core;
using AspireRunner.Core.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace AspireRunner.AspNetCore;

public static class AspireDashboardExtensions
{
    public static IServiceCollection AddAspireDashboard(this IServiceCollection services, Action<AspireDashboardOptions>? configureOptions = null)
    {
        services.AddOptions<AspireDashboardOptions>()
            .Configure(options => configureOptions?.Invoke(options));

        services.AddSingleton<DotnetCli>();
        services.AddSingleton<NugetHelper>();
        services.AddSingleton<AspireDashboardManager>();

        services.AddHostedService<AspireDashboardService>();
        return services;
    }
}