using Microsoft.Extensions.Logging;

namespace AspireRunner.Tool.Logging;

public class InMemoryLoggerFactory : ILoggerFactory
{
    public ILogger CreateLogger(string categoryName)
    {
        return new InMemoryLogger(categoryName);
    }

    public void AddProvider(ILoggerProvider provider)
    {
        // TODO: ???
    }

    public void Dispose() { }
}