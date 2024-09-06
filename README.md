# AspireRunner

A standalone runner for the .NET [Aspire Dashboard](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/dashboard/standalone) which can display OpenTelemetry data (traces,
metrics, and logs) from any application.

The runner can be used as a [dotnet tool](./src/AspireRunner.Tool/README.md) or as part of an [ASP.NET Core application](./src/AspireRunner.AspNetCore/README.md), it will
automatically download the dashboard if it's not installed, and will run and manage the dashboard process.

## [AspireRunner.Tool](./src/AspireRunner.Tool/README.md)

Provides a quick and easy to use CLI for downloading and running the Dashboard

[![NuGet Version](https://img.shields.io/nuget/vpre/AspireRunner.Tool?style=flat&logo=nuget&color=%230078d4&link=https%3A%2F%2Fwww.nuget.org%2Fpackages%2FAspireRunner.Tool)](https://www.nuget.org/packages/AspireRunner.Tool)

### Installation

```bash
dotnet tool install -g AspireRunner.Tool
```

## [AspireRunner.AspNetCore](./src/AspireRunner.AspNetCore/README.md)

A library for running the Aspire Dashboard alongside ASP.NET Core apps.

[![NuGet Version](https://img.shields.io/nuget/vpre/AspireRunner.AspNetCore?style=flat&logo=nuget&color=%230078d4&link=https%3A%2F%2Fwww.nuget.org%2Fpackages%2FAspireRunner.AspNetCore)](https://www.nuget.org/packages/AspireRunner.AspNetCore)

