using System.Diagnostics;

namespace AspireRunner.Core.Helpers;

internal static class ProcessHelper
{
    public static async Task<(string Output, string Error)> GetAsync(string processName, string[] arguments, IDictionary<string, string?>? environment = null, string? workingDir = null)
    {
        var process = Process.Start(BuildProcessInfo(processName, arguments, environment, workingDir));
        if (process is null)
        {
            return (string.Empty, "Failed to start the process");
        }

        await process.WaitForExitAsync();
        return (await process.StandardOutput.ReadToEndAsync(), await process.StandardError.ReadToEndAsync());
    }

    public static (string Output, string Error) Get(string processName, string[] arguments, IDictionary<string, string?>? environment = null, string? workingDir = null)
    {
        var process = Process.Start(BuildProcessInfo(processName, arguments, environment, workingDir));
        if (process is null)
        {
            return (string.Empty, "Failed to start the process");
        }

        process.WaitForExit();
        return (process.StandardOutput.ReadToEnd(), process.StandardError.ReadToEnd());
    }

    public static Process? Run(string processName, string[] arguments, IDictionary<string, string?>? environment = null, string? workingDir = null, Action<string>? outputHandler = null, Action<string>? errorHandler = null)
    {
        var process = Process.Start(BuildProcessInfo(processName, arguments, environment, workingDir));
        if (process is null)
        {
            return null;
        }

        if (outputHandler is not null)
        {
            process.OutputDataReceived += (_, e) => outputHandler(e.Data ?? string.Empty);
            process.BeginOutputReadLine();
        }

        if (errorHandler is not null)
        {
            process.ErrorDataReceived += (_, e) => errorHandler(e.Data ?? string.Empty);
            process.BeginErrorReadLine();
        }

        return process;
    }

    public static bool IsRunning(this Process? process)
    {
        return process?.HasExited is false;
    }

    public static Process? GetProcessOrDefault(int pid)
    {
        if (pid <= 0)
        {
            return null;
        }

        try
        {
            return Process.GetProcessById(pid);
        }
        catch
        {
            return null;
        }
    }

    private static ProcessStartInfo BuildProcessInfo(string processName, string[] arguments, IDictionary<string, string?>? environment, string? workingDir)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = processName,
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            WorkingDirectory = workingDir ?? string.Empty
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        if (environment is not null)
        {
            foreach (var (key, value) in environment)
            {
                startInfo.Environment[key] = value;
            }
        }

        return startInfo;
    }
}