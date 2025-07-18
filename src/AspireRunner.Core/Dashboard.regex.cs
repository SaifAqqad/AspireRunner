using System.Text.RegularExpressions;

namespace AspireRunner.Core;

public partial class Dashboard
{
    [GeneratedRegex(@"OTLP/(?<protocol>\w+) listening on: +(?<url>https?:\/\/[^\s]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex OtlpEndpointRegex();

    [GeneratedRegex(@$"((?:{LoginConsoleMessage})|(?:{DashboardStartedConsoleMessage})) +(?<url>https?:\/\/[^\s]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex DashboardLaunchUrlRegex();
}