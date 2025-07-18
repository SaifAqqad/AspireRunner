namespace AspireRunner.Tool.Commands;

public class UninstallCommand : ICommand<UninstallCommand.Settings>
{
    public class Settings : CommandSettings { }

    public Task<int> Execute(CommandContext context, Settings settings)
    {
        throw new NotImplementedException();
    }

    public ValidationResult Validate(CommandContext context, CommandSettings settings)
    {
        throw new NotImplementedException();
    }

    public Task<int> Execute(CommandContext context, CommandSettings settings)
    {
        throw new NotImplementedException();
    }
}