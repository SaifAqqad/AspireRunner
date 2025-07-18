namespace AspireRunner.Tool.Commands;

public class InstallCommand : AsyncCommand<InstallCommand.Settings>
{
    public class Settings : CommandSettings { }

    public override Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        throw new NotImplementedException();
    }
}