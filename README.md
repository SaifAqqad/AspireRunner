# AspireRunner

A standalone runner for the .NET [Aspire Dashboard](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/dashboard/standalone) which can be used to display OpenTelemetry
data (traces, metrics, and logs) from any application.

> [!NOTE]
> Currently the runner requires both the .NET 8 SDK and the [aspire workload](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/setup-tooling?tabs=dotnet-cli%2Cunix) to
> be installed.
>
>
> Eventually (wip), it'll be able to automatically fetch and install the dashboard and therefore would only require the .NET runtime.

## [AspireRunner.Tool](./src/AspireRunner.Tool/README.md)

Provides an easy to use dotnet tool for running the Dashboard

[![NuGet Version](https://img.shields.io/nuget/vpre/AspireRunner.Tool?style=flat&logo=nuget&color=%230078d4&link=https%3A%2F%2Fwww.nuget.org%2Fpackages%2FAspireRunner.Tool)](https://www.nuget.org/packages/AspireRunner.Tool)

### Installation

```bash
# Install the aspire workload (skip if already installed)
dotnet workload install aspire

# Install the AspireRunner tool
dotnet tool install -g AspireRunner.Tool
```

## AspireRunner.AspNetCore (wip)

An ASP.NET Core extension for running the Aspire Dashboard alongside ASP.NET apps.

