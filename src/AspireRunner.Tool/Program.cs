using AspireRunner.Tool;
using AspireRunner.Tool.Commands;

var app = new CommandApp();

app.Configure(config =>
{
    config.SetApplicationName(Runner.CommandName);
    config.SetApplicationVersion(Runner.Version.ToString());
    config.SetExceptionHandler((ex, _) =>
    {
        if (ex is ApplicationException)
        {
            AnsiConsole.MarkupLineInterpolated($"[red bold][[Error]][/] {ex.Message}");
            return -2;
        }

        AnsiConsole.WriteException(ex, ExceptionFormats.ShortenEverything);
        return -1;
    });

    // Register commands
    config.AddCommand<RunCommand>("run");
    config.AddCommand<InstallCommand>("install");
    config.AddCommand<UninstallCommand>("uninstall");

#if DEBUG
    config.PropagateExceptions();
    config.ValidateExamples();
#endif
});

app.SetDefaultCommand<RunCommand>().WithDescription($"Aspire Runner v{Runner.Version}");
return await app.RunAsync(args);