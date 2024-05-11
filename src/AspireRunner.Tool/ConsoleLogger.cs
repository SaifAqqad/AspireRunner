using Microsoft.Extensions.Logging;

namespace AspireRunner.Tool;

public class ConsoleLogger<T> : ILogger<T>
{
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var message = formatter(state, exception);
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        if (logLevel == LogLevel.Error)
        {
            Console.Write(Bold().Red("Error "));
        }
        else if (logLevel == LogLevel.Warning)
        {
            Console.Write(Bold().Yellow("Warning "));
        }

        Console.WriteLine(message);
    }

    public bool IsEnabled(LogLevel logLevel) => true;

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
}