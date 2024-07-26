using System.Text.RegularExpressions;

namespace AspireRunner.Core;

public partial class AspireDashboard
{
#if NET7_0_OR_GREATER
    private static readonly Regex LaunchUrlRegex = BuildLaunchUrlRegex();

    [GeneratedRegex(@"((?:Login to the dashboard at)|(?:Now listening on:)) +(?<url>https?:\/\/[^\s]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled, "en-US")]
    private static partial Regex BuildLaunchUrlRegex();

#else
    private static readonly Regex LaunchUrlRegex = new(@$"((?:{LoginConsoleMessage})|(?:{DashboardStartedConsoleMessage})) +(?<url>https?:\/\/[^\s]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
#endif
}