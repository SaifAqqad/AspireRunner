using Microsoft.Extensions.Logging;

namespace AspireRunner.Tool;

public class ConsoleLogger<T> : ILogger<T>
{
    public bool Verbose { get; set; }

    public ConsoleLogger(bool verbose)
    {
        Verbose = verbose;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        var message = formatter(state, exception);
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        if (logLevel == LogLevel.Error || exception is not null)
        {
            Console.Write(Bold().Red("Error "));
        }
        else if (logLevel == LogLevel.Warning)
        {
            Console.Write(Bold().Yellow("Warning "));
        }
        else if (logLevel is LogLevel.Trace or LogLevel.Debug)
        {
            Console.Write(Bold().Cyan("Debug "));
        }

        Console.WriteLine(message);
    }

    public bool IsEnabled(LogLevel logLevel) => Verbose || logLevel is not (LogLevel.Debug or LogLevel.Trace);

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
}