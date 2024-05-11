using System.Text.RegularExpressions;

namespace AspireRunner;

public partial class DotnetCli
{
    [GeneratedRegex(@"([\d\.]+)\s+(?:\[(.+)\])?", RegexOptions.Compiled)]
    private static partial Regex SdkOutputRegex();

    [GeneratedRegex(@"-+\n([\w\W]+?)\n\n", RegexOptions.Compiled)]
    private static partial Regex TableContentRegex();

    [GeneratedRegex(@"\s{2,}")]
    private static partial Regex TableColumnSeperatorRegex();
}