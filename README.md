
## AspireRunner

A dotnet tool for running the [Aspire Dashboard](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/dashboard/overview) that's bundled with the [aspire workload](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/setup-tooling?tabs=dotnet-cli%2Cunix)

The dashboard can be used to display opentelemetry data (traces, metrics and logs) from any application (doesn't have to be dotnet).

## Installation

```bash
# Install the aspire workload (can be skipped if already installed)
dotnet workload install aspire

# Install the AspireRunner tool
dotnet tool install -g AspireRunner
```

## Usage

```bash
aspire-dashboard.exe <options>

# or using the dotnet CLI
dotnet aspire-dashboard <options>

Options:
  -p, --port         (Default: 18888) The port the dashboard will listen on

  -o, --otlp-port    (Default: 4317) The port the OTLP server will listen on

  -s, --https        (Default: true) Use HTTPS instead of HTTP

  -a, --auth         Use browser token authentication for the dashboard

  -b, --browser      Launch the dashboard in the default browser

  --help             Display this help screen.

  --version          Display version information.
```
