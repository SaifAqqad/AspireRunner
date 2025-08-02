using Microsoft.Extensions.Logging;
using System.Text;

namespace AspireRunner.Core;

public partial class Dashboard
{
    private LogLevel? _lastOutputLevel;
    private StringBuilder? _lastOutput;
    private DateTimeOffset? _lastOutputTime;

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Failed to start Dashboard, retrying in {RetryDelay} seconds..."
    )]
    private partial void WarnFailedToStartDashboardWithRetry(int retryDelay);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Dashboard has already been stopped"
    )]
    private partial void WarnDashboardAlreadyStopped();

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Another instance of the Aspire Dashboard is already running, Process Id = {PID}"
    )]
    private partial void WarnExistingInstance(int pid);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Failed to start Dashboard process"
    )]
    private partial void LogFailedToStartDashboardProcess();

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Failed to start Dashboard"
    )]
    private partial void LogFailedToStartDashboard(Exception exception);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Dashboard exited unexpectedly, Attempting to restart..."
    )]
    private partial void WarnDashboardExitedUnexpectedly();

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Failed to find a suitable URL opener"
    )]
    private partial void WarnFailedToFindUrlOpener();

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Failed to launch the default browser"
    )]
    private partial void WarnFailedToLaunchBrowser();


    private void OnStandardOutput(string line)
    {
        _lastOutput ??= new StringBuilder();

        var trimmedLine = line.Trim();
        if (LogLineRegex().Match(trimmedLine) is not { Success: true } match)
        {
            // Most likely a continuation of the previous log
            if (!string.IsNullOrEmpty(trimmedLine) && !IsStackTrace(trimmedLine))
            {
                _lastOutput.AppendLine(trimmedLine);
            }

            ResetOutputLogDelay();
            return;
        }

        // Log the previous output before collecting a new one
        LogOutput();

        line = match.Groups["line"].Value;
        _lastOutputLevel = ParseConsoleLogLevel(match.Groups["logLevel"].Value);

        if (!string.IsNullOrWhiteSpace(line))
        {
            _lastOutput.AppendLine(line);
        }

        ResetOutputLogDelay();
    }

    private void ResetOutputLogDelay()
    {
        var currentTime = _lastOutputTime = DateTimeOffset.Now;
        Task.Delay(DefaultLogDelay).ContinueWith(_ =>
        {
            if (_lastOutputTime == null || _lastOutputTime != currentTime)
            {
                return;
            }

            LogOutput();
        });
    }

    private void LogOutput()
    {
        if (_lastOutput is null or { Length: 0 })
        {
            return;
        }

        var output = _lastOutput.ToString();
        var logLevel = _lastOutputLevel ?? LogLevel.Information;
        if (logLevel is LogLevel.Error or LogLevel.Critical)
        {
            HasErrors = true;
        }

        if (_logger.IsEnabled(logLevel) && (Options.Runner.PipeOutput || logLevel >= LogLevel.Warning))
        {
            _logger.Log(logLevel, output);
        }

        HandleDashboardOutput(output);
        _lastOutput.Clear();
        _lastOutputLevel = null;
    }

    private static bool IsStackTrace(string line) =>
        line.StartsWith("at ", StringComparison.OrdinalIgnoreCase) ||
        line.Contains("stack trace", StringComparison.OrdinalIgnoreCase);

    private static LogLevel ParseConsoleLogLevel(string? level) => level switch
    {
        "trce" => LogLevel.Trace,
        "dbug" => LogLevel.Debug,
        "fail" => LogLevel.Error,
        "warn" => LogLevel.Warning,
        "crit" => LogLevel.Critical,
        _ => LogLevel.Information
    };
}