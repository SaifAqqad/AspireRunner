## AspireRunner.Core

This package contains the core functionality for running the Aspire Dashboard.

The runner can be used as a [dotnet tool](https://www.nuget.org/packages/AspireRunner.Tool) or with
the [ASP.NET Core extension](https://www.nuget.org/packages/AspireRunner.AspNetCore) which runs and manages the dashboard process alongside any ASP.NET app.
<hr>

The dashboard can be used to display OpenTelemetry data (traces, metrics and logs) from any
application ([more info](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/dashboard/overview))

> [!NOTE]
> The runner will prioritize using the dashboard bundled with
> the [Aspire workload](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/setup-tooling?tabs=linux&pivots=dotnet-cli) if it's installed, otherwise, The runner will
> download the dashboard to the user's `.dotnet` directory (`~/.dotnet/.AspireRunner`).

> [!IMPORTANT]
> While the runner itself targets .NET 6 (and later), the dashboard requires the .NET 8/9 runtime to run.
>
> Meaning that the runner can be used as part of a .NET 6 application, but you still need to have the .NET 8/9 runtime installed to run the dashboard.
