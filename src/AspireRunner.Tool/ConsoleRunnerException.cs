using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;

namespace AspireRunner.Tool;

public class ConsoleRunnerException : Exception
{
    private static readonly ConsoleLogger<ConsoleRunnerException> Logger = new(true);

    public required string FormattedMessage { get; init; }

    public required int ReturnCode { get; init; }

    public new Exception? InnerException { get; set; }

    public void Log()
    {
        Logger.LogError(FormattedMessage);
        if (InnerException is not null)
        {
            Logger.LogDebug("Inner exception: {InnerException}", InnerException);
        }
    }

    [DoesNotReturn]
    public void LogAndExit()
    {
        Log();
        Environment.Exit(ReturnCode);
    }
}