using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace AspireRunner;

public class AspireDashboard
{
    private readonly AspireDashboardOptions _options;
    private readonly ILogger<AspireDashboard> _logger;

    private Process? _process;

    public AspireDashboard(IOptions<AspireDashboardOptions> options, ILogger<AspireDashboard> logger, Process? process)
    {
        _logger = logger;
        _process = process;
        _options = options.Value;
    }
}