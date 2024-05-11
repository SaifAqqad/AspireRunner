## AspireRunner.Core

This package contains the core functionality for running the Aspire Dashboard.

The runner can be used as a [dotnet tool](https://www.nuget.org/packages/AspireRunner.Tool) or with the [ASP.NET Core extension](https://www.nuget.org/packages/AspireRunner.AspNetCore) which runs and manages the dashboard alongside any ASP.NET app.
<hr>

The dashboard can be used to display OpenTelemetry data (traces, metrics and logs) from any
application ([more info](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/dashboard/overview))


> [!NOTE]
> Currently, the runner requires both the .NET 8 SDK and the [aspire workload](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/setup-tooling?tabs=dotnet-cli%2Cunix) to
> be installed.
> <br>
>
> Eventually (wip), it'll be able to automatically fetch and install the dashboard and therefore would only require the .NET runtime.
