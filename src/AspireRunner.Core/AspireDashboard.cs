using AspireRunner.Core.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Text;

namespace AspireRunner.Core;

public partial class AspireDashboard
{
    private readonly string _runnerFolder;
    private readonly DotnetCli _dotnetCli;
    private readonly AspireDashboardOptions _options;
    private readonly ILogger<AspireDashboard> _logger;

    private Process? _process;
    private StringBuilder? _lastError;
    private DateTimeOffset? _lastErrorTime;

    /// <summary>
    /// Triggered when the Aspire Dashboard has started and the UI is ready.
    /// <br/>
    /// The dashboard URL (including the browser token) is passed to the event handler.
    /// </summary>
    public event Action<string>? DashboardStarted;

    /// <summary>
    /// Indicates whether the Aspire Dashboard process has encountered any errors.
    /// </summary>
    public bool HasErrors { get; private set; }

    /// <summary>
    /// Indicates whether the Aspire Dashboard process is currently running.
    /// </summary>
    public bool IsRunning => _process?.HasExited is false;

    public AspireDashboard(DotnetCli dotnetCli, IOptions<AspireDashboardOptions> options, ILogger<AspireDashboard> logger)
    {
        _logger = logger;
        _dotnetCli = dotnetCli;
        _options = options.Value;

        _runnerFolder = Path.Combine(_dotnetCli.DataPath, DataFolder);
        if (!Directory.Exists(_runnerFolder))
        {
            Directory.CreateDirectory(_runnerFolder);
        }
    }

    public bool IsInstalled()
    {
        return _dotnetCli is { SdkPath: not null } && _dotnetCli.GetInstalledWorkloads().Contains(WorkloadId);
    }

    public void Start()
    {
        if (_process != null)
        {
            throw new ApplicationException("The Aspire Dashboard is already running.");
        }

        if (!IsInstalled())
        {
            throw new ApplicationException("The Aspire Dashboard is not installed.");
        }

        switch (_options.SingleInstanceHandling)
        {
            case SingleInstanceHandling.ReplaceExisting:
            {
                TryGetRunningProcess()?.Kill(true);
                break;
            }
            case SingleInstanceHandling.WarnAndExit:
            {
                var runningInstance = TryGetRunningProcess();
                if (runningInstance != null)
                {
                    _logger.LogWarning("Another instance of the Aspire Dashboard is already running, Process Id = {PID}", runningInstance.Id);
                    return;
                }

                break;
            }
        }

        var aspirePath = GetInstallationPath();
        if (aspirePath == null)
        {
            throw new ApplicationException("Failed to locate the Aspire Dashboard installation.");
        }

        _logger.LogInformation("Starting the Aspire Dashboard...");

        try
        {
            _process = _dotnetCli.Run(["exec", Path.Combine(aspirePath, DllName)], aspirePath, _options.ToEnvironmentVariables(), OutputHandler, ErrorHandler);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to start the Aspire Dashboard.");
            return;
        }

        PersistProcessId();
    }

    private string? GetInstallationPath()
    {
        try
        {
            var packsFolder = _dotnetCli.GetPacksFolders()
                .SelectMany(Directory.GetDirectories)
                .First(dir => dir.Contains(SdkName));

            var newestVersionPath = Directory.GetDirectories(packsFolder)
                .Select(d => new DirectoryInfo(d))
                .MaxBy(d => new Version(d.Name))?
                .FullName;

            return newestVersionPath == null ? null : Path.Combine(newestVersionPath, "tools");
        }
        catch
        {
            return null;
        }
    }

    public void Stop()
    {
        if (_process == null)
        {
            return;
        }

        try
        {
            _logger.LogInformation("Stopping the Aspire Dashboard...");
            _process.Kill(true);
        }
        catch (InvalidOperationException)
        {
            _logger.LogWarning("The Aspire Dashboard has already been stopped.");
        }

        _process = null;
    }

    public void WaitForExit()
    {
        _process?.WaitForExit();
    }

    public Task WaitForExitAsync(CancellationToken cancellationToken = default)
    {
        if (_process == null)
        {
            return Task.CompletedTask;
        }

        var tcs = new TaskCompletionSource();
        _process.EnableRaisingEvents = true;
        _process.Exited += (_, _) => tcs.SetResult();

        return Task.WhenAny(tcs.Task, Task.Delay(-1, cancellationToken));
    }

    private void PersistProcessId()
    {
        File.WriteAllText(Path.Combine(_runnerFolder, InstanceFile), _process!.Id.ToString());
    }

    private Process? TryGetRunningProcess()
    {
        var instanceFile = Path.Combine(_runnerFolder, InstanceFile);
        if (!File.Exists(instanceFile) || !int.TryParse(File.ReadAllText(instanceFile), out var pid))
        {
            return null;
        }

        try
        {
            return Process.GetProcessById(pid) is { ProcessName: "dotnet" } p ? p : null;
        }
        catch
        {
            return null;
        }
    }

    private void OutputHandler(string output)
    {
        if (_options.PipeOutput)
        {
            _logger.LogInformation("{AspireOutput}", output);
        }

        if (_options.Frontend.AuthMode is FrontendAuthMode.BrowserToken && output.Contains(DashboardStartedConsoleMessage, StringComparison.OrdinalIgnoreCase))
        {
            // Wait for the authentication token to be printed
            return;
        }

        if (LaunchUrlRegex().Match(output) is { Success: true } match)
        {
            var url = match.Groups["url"].Value;
            if (_options.LaunchBrowser)
            {
                try
                {
                    LaunchBrowser(url);
                }
                catch
                {
                    _logger.LogWarning("Failed to launch the browser.");
                }
            }

            DashboardStarted?.Invoke(url);
        }
    }

    private void ErrorHandler(string error)
    {
        // To avoid spamming the otel logs with partial errors, we need to combine the error lines into a single error message and then log it
        // This approach will combine the error lines as they're piped from the process, and then log them after a delay
        // If the error line starts with a space, it's considered a continuation of the previous error line
        // otherwise the previous error is logged and the new error will start to be collected

        HasErrors = true;
        _lastError ??= new StringBuilder();

        if (error.StartsWith(' ') || error.Length == 0)
        {
            _lastError.AppendLine(error);
            ResetErrorLogDelay();
            return;
        }

        // Log the previous error before collecting the new one
        _logger.LogError("{AspireError}", _lastError.ToString());

        _lastError.Clear();
        _lastError.AppendLine(error);
        ResetErrorLogDelay();
    }

    private void ResetErrorLogDelay()
    {
        var currentTime = _lastErrorTime = DateTimeOffset.Now;
        Task.Delay(DefaultErrorLogDelay).ContinueWith(_ =>
        {
            if (_lastError is null or { Length: 0 } || _lastErrorTime == null || _lastErrorTime != currentTime)
            {
                return;
            }

            _logger.LogError("{AspireError}", _lastError.ToString());
            _lastError.Clear();
        });
    }

    private static void LaunchBrowser(string url)
    {
        if (OperatingSystem.IsWindows())
        {
            Process.Start(new ProcessStartInfo
            {
                UseShellExecute = true,
                FileName = url
            });
        }
        else if (OperatingSystem.IsLinux())
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "xdg-open",
                UseShellExecute = true,
                Arguments = $"\"{url}\""
            });
        }
        else if (OperatingSystem.IsMacOS())
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "open",
                UseShellExecute = true,
                Arguments = $"\"{url}\""
            });
        }
    }
}