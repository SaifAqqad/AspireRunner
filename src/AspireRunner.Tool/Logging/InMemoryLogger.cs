using Microsoft.Extensions.Logging;

namespace AspireRunner.Tool.Logging;

public class InMemoryLogger(string category) : ILogger
{
    private const int MaxLogLines = 2500;

    private static int _currentIndex;
    private static readonly LogMessage[] LogMessages = new LogMessage[MaxLogLines];

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var message = formatter(state, exception);
        lock (LogMessages)
        {
            LogMessages[_currentIndex] = new LogMessage(category, logLevel, message);
            _currentIndex = (_currentIndex + 1) % MaxLogLines;
        }
    }

    public static LogMessage[] CurrentLog => [..LogMessages];

    public bool IsEnabled(LogLevel logLevel) => true;

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
}