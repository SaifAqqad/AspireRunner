## AspireRunner.AspNetCore

A library for running the Aspire Dashboard alongside ASP.NET Core apps.

The dashboard can display OpenTelemetry data (traces, metrics, and logs) from any application. This is intended
to be used for local development only.

[![NuGet Version](https://img.shields.io/nuget/vpre/AspireRunner.AspNetCore?style=flat&logo=nuget&color=%230078d4&link=https%3A%2F%2Fwww.nuget.org%2Fpackages%2FAspireRunner.AspNetCore)](https://www.nuget.org/packages/AspireRunner.AspNetCore)

> [!IMPORTANT]
> The package depends on the dashboard being pre-installed on the machine either using the dotnet tool or
> the installer package. For more info, refer to the [installer package docs](https://www.nuget.org/packages/AspireRunner.Installer)

### Example usage

```csharp
using AspireRunner.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// ...

if (builder.Environment.IsDevelopment())
{
    // bind from configuration (appsettings.json, etc)
    builder.Services.AddAspireDashboard(options => builder.Configuration.GetSection("AspireDashboard").Bind(options));

    // or pass an options instance
    builder.Services.AddAspireDashboard(new AspireDashboardOptions
    {
        Frontend = new FrontendOptions
        {
            EndpointUrls = "https://localhost:5020"
        },
        Otlp = new OtlpOptions
        {
            EndpointUrl = "https://localhost:4317"
        },
        Runner = new RunnerOptions
        {
            LaunchBrowser = true
        }
    });
}

var app = builder.Build();

// ...

await app.RunAsync();
```

> [!NOTE]
> By default, The runner will download the dashboard to the user's `.dotnet` directory (`~/.dotnet/.AspireRunner`),
> this can be changed by setting the `ASPIRE_RUNNER_PATH` environment variable.

### Configuration

The runner can be configured with the [`AspireDashboardOptions`](https://github.com/SaifAqqad/AspireRunner/blob/main/src/AspireRunner.Core/AspireDashboardOptions.cs) class, which
contains a subset of the [options supported by the Aspire dashboard](https://github.com/dotnet/aspire/blob/v8.1.0/src/Aspire.Dashboard/Configuration/DashboardOptions.cs), but also
has runner-specific options under the `Runner` property:

- `PipeOutput` (bool): When enabled, the runner will pipe the output of the dashboard process to the logger.
- `LaunchBrowser` (bool): When enabled, the runner will attempt to launch the dashboard in the default browser on startup.
- `SingleInstanceHandling` ([enum](https://github.com/SaifAqqad/AspireRunner/blob/main/src/AspireRunner.Core/AspireDashboardOptions.cs#L134)): Controls how the runner should
  handle multiple instances of the dashboard process:
    1. `WarnAndExit`: Logs a warning and exits if an existing instance is found.
    2. `Ignore`: Disables checking for running instances of the Aspire Dashboard. Note that new instances will fail to start if an existing one is using the same port
    3. `ReplaceExisting`: Kills any existing instance before starting a new one.
- `AutoUpdate` (bool): When enabled, the runner will automatically check and update the dashboard to the latest 
  version at startup.
- `PreferredVersion` (string): The version of the dashboard to download/run. If not specified or invalid, the latest 
  version will be used.
- `RestartOnFailure` (bool): When enabled, the runner will automatically restart the dashboard if it exits 
  unexpectedly.
- `RunRetryCount` (int): The number of times to retry running the dashboard if it fails to start
- `RunRetryDelay` (int): The delay between retry attempts to restart the dashboard (in seconds).