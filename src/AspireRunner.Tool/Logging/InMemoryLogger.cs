using Microsoft.Extensions.Logging;

namespace AspireRunner.Tool.Logging;

public class InMemoryLogger<T>() : InMemoryLogger(typeof(T).Name), ILogger<T>;

public class InMemoryLogger(string category) : ILogger
{
    private const int MaxLogLines = 2500;

    private int _currentIndex;
    private readonly LogMessage[] _log = new LogMessage[MaxLogLines];

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var message = formatter(state, exception);
        lock (_log)
        {
            _log[_currentIndex] = new LogMessage(category, logLevel, message);
            _currentIndex = (_currentIndex + 1) % MaxLogLines;
        }
    }

    public LogMessage[] CurrentLog => [.._log];

    public bool IsEnabled(LogLevel logLevel) => true;

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
}