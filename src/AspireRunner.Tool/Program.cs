using AspireRunner.Tool;
using AspireRunner.Tool.Commands;
using System.Text;

// Ensure console is using UTF-8 encoding
Console.OutputEncoding = Encoding.UTF8;

if (string.IsNullOrEmpty(DotnetCli.Path))
{
    AnsiConsole.Cursor.Show();
    Widgets.Write([
        Widgets.Header(),
        Text.NewLine,
        Widgets.Error("The dotnet CLI was not found, make sure it's installed and available in your PATH environment variable.")
    ]);

    return -100;
}

var app = new CommandApp();
app.Configure(config =>
{
    config.SetApplicationName(RunnerInfo.CommandName);
    config.SetApplicationVersion(RunnerInfo.Version.ToString());
    config.SetExceptionHandler((ex, _) =>
    {
        AnsiConsole.Cursor.Show();
        AnsiConsole.Write(Widgets.Error(ex.Message));

#if DEBUG
        AnsiConsole.WriteException(ex, ExceptionFormats.ShortenPaths);
#endif

        return -99;
    });

    // Register commands
    config.AddCommand<RunCommand>("run");
    config.AddCommand<InstallCommand>("install");
    config.AddCommand<UninstallCommand>("uninstall");
    config.AddCommand<CleanupCommand>("cleanup").WithDescription("Remove old versions of the dashboard and other temporary files");
});

app.SetDefaultCommand<RunCommand>().WithDescription($"Aspire Runner v{RunnerInfo.Version}");
return await app.RunAsync(args);