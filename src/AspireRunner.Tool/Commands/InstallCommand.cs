using AspireRunner.Core.Abstractions;
using AspireRunner.Installer;
using Microsoft.Extensions.Logging.Abstractions;
using System.ComponentModel;

namespace AspireRunner.Tool.Commands;

public class InstallCommand : AsyncCommand<InstallCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [CommandArgument(0, "[version]")]
        [Description("The version of the dashboard to install, pass 'latest' to install the latest version available")]
        public string? Version { get; set; }

        [CommandOption("-p|--allow-prerelease")]
        [Description("Allow prerelease versions when installing")]
        public bool IncludePrerelease { get; set; }
    }

    private readonly IDashboardInstaller _installer = new DashboardInstaller(NullLogger<DashboardInstaller>.Instance);

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        if (context.Name is "install")
        {
            Widgets.Write([Widgets.Header(), Widgets.RunnerVersion]);
            Widgets.WriteLines(2);

            if (string.IsNullOrEmpty(DotnetCli.Path))
            {
                Widgets.Write(Widgets.Error("The dotnet CLI was not found, make sure it's installed and available in your PATH environment variable."));
                return -100;
            }
        }

        var compatibleRuntimes = await Dashboard.GetCompatibleRuntimesAsync();
        if (compatibleRuntimes.Length == 0)
        {
            throw new ApplicationException($"The dashboard requires version '{Dashboard.MinimumRuntimeVersion}' or newer of the '{Dashboard.RequiredRuntimeName}' runtime");
        }

        Widgets.Write("Fetching available versions ");
        var availableVersions = await _installer.GetAvailableVersionsAsync(settings.IncludePrerelease, cancellationToken)
            .ShowSpinner(withResult: true);

        var latestRuntimeVersion = compatibleRuntimes.Max();
        var legacyRuntime = Dashboard.VersionCompatibilityMatrix
            .FirstOrDefault(v => v.Runtime.IsSatisfied(latestRuntimeVersion));

        availableVersions = availableVersions
            .Where(v => legacyRuntime == default || v <= legacyRuntime.LastSupportedVersion)
            .ToArray();

        if (availableVersions.Length is 0)
        {
            Widgets.Write("No compatible versions of the dashboard were found on Nuget", true);
            return 0;
        }

        Widgets.WriteInterpolated($"Found [{Widgets.PrimaryColorText}]{availableVersions.Length}[/] versions", true);
        if (settings.Version?.ToLowerInvariant() is "latest")
        {
            return await InstallVersionAsync(availableVersions.First()) ? 0 : -99;
        }

        if (string.IsNullOrWhiteSpace(settings.Version))
        {
            var versionToInstall = await PromptVersionAsync(availableVersions);
            return await InstallVersionAsync(versionToInstall) ? 0 : -99;
        }

        if (!VersionRange.TryParse(settings.Version, true, out var versionRange))
        {
            Widgets.Write(Widgets.Error($"Invalid version '{settings.Version}'"));
            return 1;
        }

        var matchingVersion = availableVersions.FirstOrDefault(v => versionRange.IsSatisfied(v));
        if (matchingVersion is null)
        {
            Widgets.Write(Widgets.Error($"No version matching '{settings.Version}' was found"));
            return 2;
        }

        return await InstallVersionAsync(matchingVersion) ? 0 : -99;
    }

    private async Task<Version> PromptVersionAsync(Version[] versions)
    {
        var selected = await AnsiConsole.PromptAsync(
            new SelectionPrompt<string>()
                .EnableSearch()
                .HighlightStyle(Widgets.PrimaryColor)
                .Title("Which version do you want to install?")
                .AddChoices(versions.Select(v => v.ToString()))
        );

        return versions.First(v => selected == v.ToString());
    }

    private async Task<bool> InstallVersionAsync(Version version)
    {
        var success = false;

        try
        {
            var installedVersions = Dashboard.GetInstalledVersions();
            if (installedVersions.Any(v => v == version))
            {
                Widgets.Write([$"Version {version} is already installed ".Widget(), Widgets.SuccessCheck()], true);
                return true;
            }

            Widgets.WriteInterpolated($"Installing version [{Widgets.PrimaryColorText}]{version}[/] ");
            success = await _installer.InstallAsync(version, CancellationToken.None).ShowSpinner();

            Widgets.Write(success ? Widgets.SuccessCheck() : Widgets.ErrorCross());
        }
        catch (Exception ex)
        {
            Widgets.Write([Widgets.ErrorCross(), " ".Widget(), ex.ErrorWidget()]);
        }

        Widgets.WriteLines();
        return success;
    }
}