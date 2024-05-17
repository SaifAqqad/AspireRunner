using System.Text.RegularExpressions;

namespace AspireRunner;

public partial class AspireDashboard
{
    [GeneratedRegex(@$"((?:{LoginConsoleMessage})|(?:{DashboardStartedConsoleMessage})) +(?<url>https?:\/\/[^\s]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex LaunchUrlRegex();
}