using Microsoft.Extensions.DependencyInjection;

namespace AspireRunner.Installer;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAspireDashboardInstaller(this IServiceCollection services)
    {
        services.AddSingleton<IDashboardInstaller, DashboardInstaller>();

        return services;
    }
}