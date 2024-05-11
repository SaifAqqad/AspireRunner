using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;

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
        
        // TODO: 

    }
}