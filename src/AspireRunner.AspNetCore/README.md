## AspireRunner.AspNetCore

A library for running the Aspire Dashboard alongside ASP.NET Core apps.

The dashboard can display OpenTelemetry data (traces, metrics, and logs) from any application, although this is intended to be used for local development only.

[![NuGet Version](https://img.shields.io/nuget/vpre/AspireRunner.AspNetCore?style=flat&logo=nuget&color=%230078d4&link=https%3A%2F%2Fwww.nuget.org%2Fpackages%2FAspireRunner.AspNetCore)](https://www.nuget.org/packages/AspireRunner.Tool)

### Example usage

```csharp
using AspireRunner.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// ...

if (builder.Environment.IsDevelopment())
{
    // bind from configuration (appsettings.json, etc)
    builder.Services.AddAspireDashboard(config => {
        builder.Configuration.GetSection("AspireDashboard").Bind(config);
    });

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
            AutoDownload = true
        }
    });
}

var app = builder.Build();

// ...

await app.RunAsync();
```

> [!NOTE]
> The runner will download the dashboard to the user's `.dotnet` directory (`~/.dotnet/.AspireRunner`).

> [!IMPORTANT]
> While the runner itself targets .NET 6 (and later), the dashboard requires the .NET 8/9 runtime to run.
>
> Meaning that the runner can be used as part of a .NET 6 application, but you'll still need the .NET 8/9 runtime to run the dashboard.

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
- `AutoUpdate` (bool): When enabled, the runner will automatically update the dashboard to the latest version on startup.
- `PreferredVersion` (string): The version of the dashboard to download/run. If not specified or invalid, the latest 
  version will be used.