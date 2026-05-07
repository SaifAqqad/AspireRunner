# AspireRunner

A standalone runner for the
.NET [Aspire Dashboard](https://aspire.dev/dashboard/standalone/) which can
display OpenTelemetry data (traces,
metrics, and logs) from any application.

## [AspireRunner.AspNetCore](./src/AspireRunner.AspNetCore/README.md)

Integrates the Dashboard directly into your ASP.NET Core App, ideal for local development
environments where you want the dashboard to start and stop automatically with your web apps.

[![NuGet Version](https://img.shields.io/nuget/vpre/AspireRunner.AspNetCore?style=flat&logo=nuget&color=%230078d4&link=https%3A%2F%2Fwww.nuget.org%2Fpackages%2FAspireRunner.AspNetCore)](https://www.nuget.org/packages/AspireRunner.AspNetCore)

_______________

## ~~AspireRunner.Tool~~

~~Provides a quick and easy to use CLI for downloading and running the Dashboard~~

This dotnet tool is deprecated now, as the same functionality ships with the
[official aspire CLI](https://aspire.dev/reference/cli/commands/aspire-dashboard-run/).

```bash
  aspire dashboard run --allow-anonymous --banner
```