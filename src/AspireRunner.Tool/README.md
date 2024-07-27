## AspireRunner.Tool

A dotnet tool for downloading and running the standalone [Aspire Dashboard](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/dashboard/standalone)

The dashboard can display OpenTelemetry data (traces, metrics, and logs) from any
application ([more info](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/dashboard/overview)).

[![NuGet Version](https://img.shields.io/nuget/vpre/AspireRunner.Tool?style=flat&logo=nuget&color=%230078d4&link=https%3A%2F%2Fwww.nuget.org%2Fpackages%2FAspireRunner.Tool)](https://www.nuget.org/packages/AspireRunner.Tool)

## Installation

```console
dotnet tool install -g AspireRunner.Tool
```

## Usage

```console
aspire-dashboard <options>

Options:
  -b, --browser          Launch the dashboard in the default browser

  -p, --port             (Default: 18888) The port the dashboard will be available on

  -a, --auth             Use browser token authentication for the dashboard

  -s, --https            (Default: true) Use HTTPS instead of HTTP, this applies to both the dashboard and the OTLP server

  --dashboard-https      Use HTTPS instead of HTTP for the dashboard

  --otlp-port            (Default: 4317) The port the OTLP/gRPC server will listen on

  --otlp-http-port       The port the OTLP/HTTP server will listen on, by default, only the gRPC server is started

  --otlp-key             The API key to use for the OTLP server

  --otlp-https           Use HTTPS instead of HTTP for the OTLP/gRPC and OTLP/HTTP endpoints

  -m, --multiple         Allow running multiple instances of the dashboard, if this isn't passed, existing instances will be replaced

  -d, --auto-download    (Default: true) Automatically download the dashboard if it's not installed

  -v, --verbose          Enable verbose logging

  --help                 Display this help screen.

  --version              Display version information.
```

> [!NOTE]
> The runner will prioritize using the dashboard bundled with
> the [Aspire workload](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/setup-tooling?tabs=linux&pivots=dotnet-cli) if it's installed, otherwise, The runner will
> download the dashboard to the user's `.dotnet` directory (`~/.dotnet/.AspireRunner`).