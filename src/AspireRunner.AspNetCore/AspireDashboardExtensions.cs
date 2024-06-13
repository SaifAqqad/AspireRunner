using AspireRunner.Core;
using AspireRunner.Core.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AspireRunner.AspNetCore;

public static class AspireDashboardExtensions
{
    public static IServiceCollection AddAspireDashboard(this IServiceCollection services, Action<AspireDashboardOptions>? configureOptions = null)
    {
        services.AddOptions<AspireDashboardOptions>()
            .Configure(options => configureOptions?.Invoke(options));

        services.AddSingleton<DotnetCli>();
        services.AddSingleton<NugetHelper>();
        services.AddSingleton<AspireDashboard>(sp =>
        {
            var dotnet = sp.GetRequiredService<DotnetCli>();
            var nugetHelper = sp.GetRequiredService<NugetHelper>();
            var logger = sp.GetRequiredService<ILogger<AspireDashboard>>();
            var options = sp.GetRequiredService<IOptions<AspireDashboardOptions>>();
            
            return new AspireDashboard(dotnet, nugetHelper, options.Value, logger);
        });

        services.AddHostedService<AspireDashboardService>();
        return services;
    }
}