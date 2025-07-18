namespace AspireRunner.Tool.Commands;

public class UninstallCommand : AsyncCommand<UninstallCommand.Settings>
{
    public class Settings : CommandSettings { }

    public override Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        throw new NotImplementedException();
    }
}