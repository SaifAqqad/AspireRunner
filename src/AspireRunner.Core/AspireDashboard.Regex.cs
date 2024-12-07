using System.Text.RegularExpressions;

namespace AspireRunner.Core;

public partial class AspireDashboard
{
    private static readonly Regex OtlpEndpointRegex = BuildOtlpEndpointRegex();
    private static readonly Regex DashboardLaunchUrlRegex = BuildDashboardLaunchUrlRegex();

    [GeneratedRegex(@"OTLP/(?<protocol>\w+) listening on: +(?<url>https?:\/\/[^\s]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex BuildOtlpEndpointRegex();

    [GeneratedRegex(@$"((?:{LoginConsoleMessage})|(?:{DashboardStartedConsoleMessage})) +(?<url>https?:\/\/[^\s]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex BuildDashboardLaunchUrlRegex();
}