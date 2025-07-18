using AspireRunner.Tool.Commands;
using System.Reflection;

var app = new CommandApp();

app.Configure(config =>
{
    config.SetApplicationName("Aspire Runner");
    config.SetApplicationVersion(Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "N/A");

    // Register commands
    config.AddCommand<RunCommand>("run");
    config.AddCommand<InstallCommand>("install");
    config.AddCommand<UninstallCommand>("uninstall");

#if DEBUG
    config.PropagateExceptions();
    config.ValidateExamples();
#endif
});

app.SetDefaultCommand<RunCommand>();
return await app.RunAsync(args);