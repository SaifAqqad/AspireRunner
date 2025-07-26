using AspireRunner.Core.Abstractions;
using AspireRunner.Installer;
using Microsoft.Extensions.Logging.Abstractions;
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
        Widgets.Write([Widgets.Header(), Widgets.RunnerVersion]);
        Widgets.WriteLines(2);

        var runningInstance = Dashboard.TryGetRunningInstance();
        if (runningInstance.Dashboard.IsRunning())
        {
            Widgets.Write(Widgets.Error(
                $"An instance of the dashboard is currently running (PID = [{Widgets.DefaultColorText}]{runningInstance.Dashboard.Id}[/]). Please stop it before attempting to uninstall"
            ));

            return -1;
        }

        var installedVersions = Dashboard.GetInstalledVersions();
        if (installedVersions.Length is 0)
        {
            Widgets.Write("No versions of the dashboard are installed", true);
            return 0;
        }

        var success = true;
        if (settings.Version?.ToLowerInvariant() is "all" or "*")
        {
            Widgets.WriteInterpolated($"Found [{Widgets.DefaultColorText}]{installedVersions.Length}[/] versions installed", true);

            foreach (var version in installedVersions)
            {
                success &= await UninstallVersionAsync(version);
            }

            return success ? 0 : -99;
        }

        if (!string.IsNullOrWhiteSpace(settings.Version) && VersionRange.TryParse(settings.Version, true, out var versionRange))
        {
            var matchingVersions = installedVersions.Where(v => versionRange.IsSatisfied(v)).ToArray();
            if (matchingVersions.Length is 0)
            {
                Widgets.WriteInterpolated($"No versions matching '{settings.Version}' are installed", true);
                return 1;
            }

            if (matchingVersions.Length is 1)
            {
                return await UninstallVersionAsync(matchingVersions[0]) ? 0 : -99;
            }

            var versionsToUninstall = await PromptVersionsAsync(matchingVersions);
            foreach (var version in versionsToUninstall)
            {
                success &= await UninstallVersionAsync(version);
            }
        }
        else
        {
            var versions = await PromptVersionsAsync(installedVersions);
            foreach (var version in versions)
            {
                success &= await UninstallVersionAsync(version);
            }
        }

        return success ? 0 : -99;
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

        if (selected is "All")
        {
            return versions;
        }

        return versions.Where(v => selected == v.ToString());
    }

    internal async Task<bool> UninstallVersionAsync(Version version)
    {
        var success = false;

        try
        {
            Widgets.WriteInterpolated($"Uninstalling version [{Widgets.DefaultColorText}]{version}[/] ");
            success = await _installer.RemoveAsync(version, CancellationToken.None).ShowSpinner();

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