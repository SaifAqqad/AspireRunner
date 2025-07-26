## AspireRunner.Installer

This package contains functionality for installing and managing existing installations of the Aspire Dashboard.

### Example usage

```csharp
using AspireRunner.AspNetCore;
using AspireRunner.Installer;

var builder = WebApplication.CreateBuilder(args);

// ...

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddAspireDashboardInstaller();
    builder.Services.AddAspireDashboard(options => builder.Configuration.GetSection("AspireDashboard").Bind(options));
}

var app = builder.Build();

// ...

await app.RunAsync();
```

> [!NOTE]
> By default, The runner will download the dashboard to the user's `.dotnet` directory (`~/.dotnet/.AspireRunner`),
> this can be changed by setting the `ASPIRE_RUNNER_PATH` environment variable.
