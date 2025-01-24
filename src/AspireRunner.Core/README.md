## AspireRunner.Core

This package contains the core functionality for running the Aspire Dashboard.

The runner can be used as a [dotnet tool](https://www.nuget.org/packages/AspireRunner.Tool) or with
the [ASP.NET Core extension](https://www.nuget.org/packages/AspireRunner.AspNetCore) which runs and manages the dashboard process alongside any ASP.NET app.

________

The dashboard can be used to display OpenTelemetry data (traces, metrics and logs) from any
application ([more info](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/dashboard/overview))

> [!NOTE]
> The runner will download the dashboard to the user's `.dotnet` directory (`~/.dotnet/.AspireRunner`).
