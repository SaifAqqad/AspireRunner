using System.Text.RegularExpressions;

namespace AspireRunner.Core;

public partial class AspireDashboard
{
    private static readonly Regex LaunchUrlRegex = new(@$"((?:{LoginConsoleMessage})|(?:{DashboardStartedConsoleMessage})) +(?<url>https?:\/\/[^\s]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
}