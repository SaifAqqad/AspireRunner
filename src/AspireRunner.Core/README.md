## AspireRunner.Core

This package contains the core functionality for running the Aspire Dashboard.

The core package depends on the dashboard being pre-installed on the machine either using the dotnet tool or the
installer package. For more info, refer to the [installer package docs](../AspireRunner.Installer/README.md)

> [!NOTE]
> By default, The runner will download the dashboard to the user's `.dotnet` directory (`~/.dotnet/.AspireRunner`),
> this can be changed by setting the `ASPIRE_RUNNER_PATH` environment variable.
