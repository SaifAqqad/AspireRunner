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
        services.Configure<AspireDashboardOptions>(options => configureOptions?.Invoke(options));

        services.AddSingleton<DotnetCli>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<DotnetCli>>();
            var dotnetCli = DotnetCli.TryCreate();
            if (dotnetCli == null)
            {
                logger.LogWarning("Could not find the dotnet CLI, make sure it is installed and available in the PATH");
            }

            return dotnetCli!;
        });

        services.AddSingleton<NugetHelper>();
        services.AddSingleton<AspireDashboard>(sp =>
        {
            var nugetHelper = sp.GetRequiredService<NugetHelper>();
            var logger = sp.GetRequiredService<ILogger<AspireDashboard>>();
            var options = sp.GetRequiredService<IOptions<AspireDashboardOptions>>();

            var dotnetCli = sp.GetService<DotnetCli>();
            if (dotnetCli == null)
            {
                return null!;
            }

            return new AspireDashboard(dotnetCli, nugetHelper, options.Value, logger);
        });

        services.AddHostedService<AspireDashboardService>();
        return services;
    }
}