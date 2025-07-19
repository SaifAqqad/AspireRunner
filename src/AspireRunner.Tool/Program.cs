using AspireRunner.Tool;
using AspireRunner.Tool.Commands;
using System.Reflection;

var app = new CommandApp();

app.Configure(config =>
{
    config.SetApplicationName(Runner.CommandName);
    config.SetApplicationVersion(Runner.Version.ToString());

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