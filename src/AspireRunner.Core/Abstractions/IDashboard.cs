namespace AspireRunner.Core.Abstractions;

public interface IDashboard
{
    Version Version { get; }

    DashboardOptions Options { get; }

    string InstallationPath { get; }

    bool HasErrors { get; }

    bool IsRunning { get; }

    int? Pid { get; }

    string? Url { get; }

    IReadOnlyList<(string Url, string Protocol)>? OtlpEndpoints { get; }

    /// <summary>
    /// Triggered when the Aspire Dashboard has started and the UI is ready.
    /// <br/>
    /// The dashboard URL (including the browser token) is passed to the event handler.
    /// </summary>
    event Action<string>? DashboardStarted;

    /// <summary>
    /// Triggered when the OTLP endpoint is ready to receive telemetry data.
    /// <br/>
    /// The OTLP endpoint URL and protocol are passed to the event handler.
    /// </summary>
    event Action<(string Url, string Protocol)>? OtlpEndpointReady;

    /// <summary>
    /// Starts the Aspire Dashboard process.
    /// </summary>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops the Aspire Dashboard process.
    /// </summary>
    Task StopAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a task that completes when the Aspire Dashboard process exits or when the cancellation token is triggered.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token to monitor for cancellation requests.</param>
    Task WaitForExitAsync(CancellationToken cancellationToken = default);
}