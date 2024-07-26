using System.Text.RegularExpressions;

namespace AspireRunner.Core;

public partial class AspireDashboard
{
#if NET7_0_OR_GREATER
    private static readonly Regex OtlpEndpointRegex = BuildOtlpEndpointRegex();
    private static readonly Regex DashboardLaunchUrlRegex = BuildDashboardLaunchUrlRegex();

    [GeneratedRegex(@"OTLP/(?<protocol>\w+) listening on: +(?<url>https?:\/\/[^\s]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex BuildOtlpEndpointRegex();

    [GeneratedRegex(@$"((?:{LoginConsoleMessage})|(?:{DashboardStartedConsoleMessage})) +(?<url>https?:\/\/[^\s]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex BuildDashboardLaunchUrlRegex();
#else
    private static readonly Regex OtlpEndpointRegex = new(@"OTLP/(?<protocol>\w+) listening on: +(?<url>https?://[^\s]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex DashboardLaunchUrlRegex = new(@$"((?:{LoginConsoleMessage})|(?:{DashboardStartedConsoleMessage})) +(?<url>https?://[^\s]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
#endif
}