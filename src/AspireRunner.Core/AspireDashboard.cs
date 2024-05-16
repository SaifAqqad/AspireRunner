using AspireRunner.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Text.Json;

namespace AspireRunner;

public class AspireDashboard
{
    private const string AspireSdkName = "Aspire.Dashboard.Sdk";
    private const string AspireDashboardDll = "Aspire.Dashboard.dll";
    private const string AspireDashboardInstanceFile = "aspire-dashboard.pid";

    private readonly DotnetCli _dotnetCli;
    private readonly AspireDashboardOptions _options;
    private readonly ILogger<AspireDashboard> _logger;

    private Process? _process;

    public AspireDashboard(DotnetCli dotnetCli, IOptions<AspireDashboardOptions> options, ILogger<AspireDashboard> logger)
    {
        _logger = logger;
        _dotnetCli = dotnetCli;
        _options = options.Value;
    }

    public bool IsInstalled()
    {
        return _dotnetCli is { SdkPath: not null } && _dotnetCli.GetInstalledWorkloads().Contains("aspire");
    }

    public void Start(AspireDashboardOptions? options = null)
    {
        if (_process != null)
        {
            throw new ApplicationException("The Aspire Dashboard is already running.");
        }

        if (!IsInstalled())
        {
            throw new ApplicationException("The Aspire Dashboard is not installed.");
        }

        _logger.LogInformation("Starting the Aspire Dashboard...");

        var packsFolder = _dotnetCli.GetPacksFolders()
            .SelectMany(Directory.GetDirectories)
            .First(dir => dir.Contains(AspireSdkName));

        var newestVersionPath = Directory.GetDirectories(packsFolder)
            .Select(d => new DirectoryInfo(d))
            .MaxBy(d => new Version(d.Name))?
            .FullName;

        if (newestVersionPath == null)
        {
            throw new ApplicationException("Failed to find the newest version of the Aspire Dashboard.");
        }

        var aspirePath = Path.Combine(newestVersionPath, "tools");
        var envOptions = BuildOptionsEnvVars(options ?? _options);
        
        Console.WriteLine(JsonSerializer.Serialize(envOptions));
    }

    private static IDictionary<string, string> BuildOptionsEnvVars(AspireDashboardOptions options)
    {
        var envVars = new Dictionary<string, string>();
        var jsonObject = JsonSerializer.SerializeToNode(options)?.AsObject().Flatten() ?? [];

        foreach (var property in jsonObject)
        {
            if (property.Value is null || property.Value.GetValueKind() is JsonValueKind.Null)
            {
                continue;
            }

            var key = property.Key.Replace("$.", string.Empty).Replace(".", "__").ToUpperInvariant();
            envVars[key] = property.Value.ToString();
        }

        return envVars;
    }
}