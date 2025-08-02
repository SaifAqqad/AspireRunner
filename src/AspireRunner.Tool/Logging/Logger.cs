using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Threading.Channels;

namespace AspireRunner.Tool.Logging;

public static class Logger
{
    public record struct LogRecord(LogLevel Level, string Message);

    private const int MaxLogRecords = 20;
    private static readonly ConcurrentDictionary<string, Channel<LogRecord>> Channels = new();
    private static readonly BoundedChannelOptions BoundedChannelOptions = new(MaxLogRecords)
    {
        SingleReader = true,
        FullMode = BoundedChannelFullMode.DropOldest
    };

    public static ILoggerFactory DefaultFactory { get; } = new LoggerFactory([new InMemoryLoggerProvider()]);

    public static bool Verbose { get; set; }

    public static void Write(string channelName, LogRecord record)
    {
        if (string.IsNullOrWhiteSpace(record.Message))
        {
            return;
        }

        var channel = Channels.GetOrAdd(channelName, _ => Channel.CreateBounded<LogRecord>(BoundedChannelOptions));
        channel.Writer.TryWrite(record);
    }

    public static LogRecord[] Read(string channelName, int maxCount = 1)
    {
        var channel = Channels.GetOrAdd(channelName, _ => Channel.CreateBounded<LogRecord>(BoundedChannelOptions));

        maxCount = Math.Max(Math.Min(maxCount, channel.Reader.Count), 0);
        var records = new LogRecord[maxCount];

        for (int i = 0; i < maxCount; i++)
        {
            if (!channel.Reader.TryRead(out var record))
            {
                break;
            }

            records[i] = record;
        }

        return records;
    }
}