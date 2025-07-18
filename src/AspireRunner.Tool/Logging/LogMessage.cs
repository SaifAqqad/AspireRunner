using Microsoft.Extensions.Logging;

namespace AspireRunner.Tool.Logging;

public record struct LogMessage(string Category, LogLevel Level, string Message)
{
    public DateTimeOffset Timestamp { get; } = DateTimeOffset.UtcNow;
}