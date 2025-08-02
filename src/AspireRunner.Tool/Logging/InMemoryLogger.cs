using Microsoft.Extensions.Logging;

namespace AspireRunner.Tool.Logging;

public class InMemoryLogger : ILogger
{
    public string CategoryName { get; }

    public InMemoryLogger(string categoryName)
    {
        CategoryName = categoryName;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var message = formatter(state, exception);
        Logger.Write(CategoryName, new LogRecord(logLevel, message));
    }

    public bool IsEnabled(LogLevel logLevel) => Logger.Verbose || logLevel > LogLevel.Information;

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
}

public class InMemoryLoggerProvider : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName)
    {
        return new InMemoryLogger(categoryName);
    }

    public void Dispose() { }
}