namespace AspireRunner.Tool.Commands;

public class CleanupCommand : AsyncCommand
{
    public override async Task<int> ExecuteAsync(CommandContext context)
    {
        Widgets.Write([Widgets.Header(), Widgets.RunnerVersion]);
        Widgets.WriteLines(2);

        var runningInstance = Dashboard.TryGetRunningInstance();
        if (runningInstance.Dashboard.IsRunning())
        {
            Widgets.Write(Widgets.Error(
                $"An instance of the dashboard is currently running (PID = [{Widgets.PrimaryColorText}]{runningInstance.Dashboard.Id}[/]). Please stop it before attempting to cleanup"
            ));

            return -1;
        }

        if (runningInstance.Runner.IsRunning())
        {
            Widgets.Write(Widgets.Error(
                $"An instance of the runner is currently running (PID = [{Widgets.PrimaryColorText}]{runningInstance.Runner.Id}[/]). Please stop it before attempting to cleanup"
            ));

            return -1;
        }

        var runnerPath = Dashboard.GetRunnerPath();
        var instanceFile = Path.Combine(runnerPath, Dashboard.InstanceFile);
        if (File.Exists(instanceFile))
        {
            Widgets.Write("Removing instance file ");
            File.Delete(instanceFile);
            Widgets.Write([" ".Widget(), Widgets.SuccessCheck()], true);
        }

        var installedVersions = Dashboard.GetInstalledVersions();
        if (installedVersions.Length < 2)
        {
            Widgets.Write("No redundant versions of the dashboard are installed", true);
            return 0;
        }

        var command = new UninstallCommand();
        Widgets.WriteInterpolated($"Found [{Widgets.PrimaryColorText}]{installedVersions.Length}[/] versions installed, keeping the latest ([{Widgets.PrimaryColorText}]{installedVersions[0]}[/])", true);

        foreach (var version in installedVersions.Skip(1))
        {
            await command.UninstallVersionAsync(version);
        }

        return 0;
    }
}