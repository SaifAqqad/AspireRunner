## AspireRunner.AspNetCore

A library for running the Aspire Dashboard alongside ASP.NET Core apps (as a background service).

The dashboard can display OpenTelemetry data (traces, metrics, and logs) from any application, although this is intended to be used for local development only.

[![NuGet Version](https://img.shields.io/nuget/vpre/AspireRunner.AspNetCore?style=flat&logo=nuget&color=%230078d4&link=https%3A%2F%2Fwww.nuget.org%2Fpackages%2FAspireRunner.AspNetCore)](https://www.nuget.org/packages/AspireRunner.Tool)

### Example usage

```csharp
using AspireRunner.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// ...

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddAspireDashboard(config => {
        config.Otlp.EndpointUrl = "https://localhost:33554";

        // Or bind from configuration (appsettings.json, etc)
        builder.Configuration.GetSection("AspireDashboard").Bind(config);
    });
}

//...

var app = builder.Build();

//...
```


> [!NOTE]
> The runner will prioritize using the dashboard bundled with
> the [Aspire workload](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/setup-tooling?tabs=windows&pivots=visual-studio), if it's installed.

> [!IMPORTANT]
> While the runner itself targets .NET 6 (and later), the dashboard requires the .NET 8/9 runtime to run.
>
> Meaning that the runner can be used as part of a .NET 6 application, but you'll still need the .NET 8/9 runtime to run the dashboard.
