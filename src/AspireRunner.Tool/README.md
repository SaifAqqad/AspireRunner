
## AspireRunner

A dotnet tool for running the Aspire Dashboard that's bundled with the [aspire workload](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/setup-tooling?tabs=dotnet-cli%2Cunix).

The dashboard can be used to display opentelemetry data (traces, metrics and logs) from any application ([more info](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/dashboard/overview))

[![NuGet Version](https://img.shields.io/nuget/vpre/AspireRunner.Tool?style=flat&logo=nuget&color=%230078d4&link=https%3A%2F%2Fwww.nuget.org%2Fpackages%2FAspireRunner.Tool)](https://www.nuget.org/packages/AspireRunner.Tool)

## Installation

```bash
# Install the aspire workload (can be skipped if already installed)
dotnet workload install aspire

# Install the AspireRunner tool
dotnet tool install -g AspireRunner.Tool
```

## Usage

```bash
aspire-dashboard <options>

Options:
  -b, --browser      Launch the dashboard in the default browser

  -p, --port         (Default: 18888) The port the dashboard will be available on

  -o, --otlp-port    (Default: 4317) The port the OTLP server will listen on

  -k, --otlp-key     The API key to use for the OTLP server

  -s, --https        (Default: true) Use HTTPS instead of HTTP, this applies to both the dashboard and the OTLP server

  -a, --auth         Use browser token authentication for the dashboard

  -m, --multiple     Allow running multiple instances of the dashboard

  --help             Display this help screen.

  --version          Display version information.
```
