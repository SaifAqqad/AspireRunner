## AspireRunner.Core

This package contains the core functionality for running the Aspire Dashboard.

The runner can be used as a [dotnet tool]() or with the [ASP.NET extension]() which runs and manages the dashboard alongside any ASP.NET app.
<hr>

The dashboard can be used to display OpenTelemetry data (traces, metrics and logs) from any
application ([more info](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/dashboard/overview))


> [!NOTE]
> Currently the runner requires both the .NET 8 SDK and the [aspire workload](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/setup-tooling?tabs=dotnet-cli%2Cunix) to
> be installed.
> <br>
>
> Eventually (wip), the runner will only require the .NET 8 runtime.
