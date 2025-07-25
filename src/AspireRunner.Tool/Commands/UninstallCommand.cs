using AspireRunner.Core.Abstractions;
using AspireRunner.Installer;
using Microsoft.Extensions.Logging.Abstractions;
using Spectre.Console.Extensions;
using System.ComponentModel;

namespace AspireRunner.Tool.Commands;

public class UninstallCommand : AsyncCommand<UninstallCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [CommandArgument(0, "[version]")]
        [Description("The version of the dashboard to uninstall, pass 'all' or '*' to uninstall all versions")]
        public string? Version { get; set; }
    }

    private readonly IDashboardInstaller _installer = new DashboardInstaller(NullLogger<DashboardInstaller>.Instance);

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        AnsiConsole.Write(Widgets.Header());
        AnsiConsole.Write(Widgets.RunnerVersion);
        AnsiConsole.Console.EmptyLines(2);

        var runningInstance = Dashboard.TryGetRunningInstance();
        if (runningInstance.Dashboard.IsRunning())
        {
            AnsiConsole.Write(Widgets.Error(
                $"An instance of the dashboard is currently running (PID = [{Widgets.DefaultColorText}]{runningInstance.Dashboard.Id}[/]). Please stop it before attempting to uninstall"
            ));

            return -1;
        }

        var installedVersions = Dashboard.GetInstalledVersions();
        if (installedVersions.Length is 0)
        {
            AnsiConsole.MarkupLine("No versions of the dashboard are installed");
            return 0;
        }

        if (settings.Version is "all" or "*")
        {
            AnsiConsole.MarkupLineInterpolated($"Found [{Widgets.DefaultColorText}]{installedVersions.Length}[/] versions installed");

            foreach (var version in installedVersions)
            {
                await UninstallVersionAsync(version);
            }

            return 0;
        }

        if (!string.IsNullOrWhiteSpace(settings.Version) && VersionRange.TryParse(settings.Version, true, out var versionRange))
        {
            var matchingVersions = installedVersions.Where(v => versionRange.IsSatisfied(v)).ToArray();
            if (matchingVersions.Length is 0)
            {
                AnsiConsole.MarkupLine($"No versions matching '{settings.Version}' are installed");
                return 1;
            }

            if (matchingVersions.Length is 1)
            {
                await UninstallVersionAsync(matchingVersions[0]);
                return 0;
            }

            var versionsToUninstall = await PromptVersionsAsync(matchingVersions);
            foreach (var version in versionsToUninstall)
            {
                await UninstallVersionAsync(version);
            }

            return 0;
        }

        var versions = await PromptVersionsAsync(installedVersions);
        foreach (var version in versions)
        {
            await UninstallVersionAsync(version);
        }

        return 0;
    }

    private async Task<IEnumerable<Version>> PromptVersionsAsync(Version[] versions)
    {
        var selected = await AnsiConsole.PromptAsync(
            new SelectionPrompt<string>()
                .EnableSearch()
                .HighlightStyle(Widgets.DefaultColor)
                .Title("Which version do you want to uninstall?")
                .AddChoices(["All", ..versions.Select(v => v.ToString())])
        );

        if (selected == "All")
        {
            return versions;
        }

        return versions.Where(v => selected == v.ToString());
    }

    private async Task UninstallVersionAsync(Version version)
    {
        try
        {
            AnsiConsole.MarkupInterpolated($"Uninstalling version {version}");
            var success = await _installer.RemoveAsync(version, CancellationToken.None)
                .Spinner(Spinner.Known.Dots, Widgets.DefaultColor);

            if (success)
            {
                AnsiConsole.Markup(" [green]✓[/]");
            }
            else
            {
                AnsiConsole.Markup(" [red]✕[/]");
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.Markup(" [red]✕[/] ");
            AnsiConsole.Write(ex.ErrorWidget());
        }

        AnsiConsole.WriteLine();
    }
}