using System.Text.RegularExpressions;

namespace AspireRunner.Core.Helpers;

public partial class DotnetCli
{
    [GeneratedRegex(@"([\d\.]+)\s+(?:\[(.+)\])?", RegexOptions.Compiled)]
    private static partial Regex SdkOutputRegex();

    [GeneratedRegex(@"(.+?) ([\d\-_.\w]+?) \[(.+)\]", RegexOptions.Compiled)]
    private static partial Regex RuntimeOutputRegex();

    [GeneratedRegex(@"-+\r?\n([\w\W]+?)\r?\n\r?\n", RegexOptions.Compiled)]
    private static partial Regex TableContentRegex();

    [GeneratedRegex(@"\s{2,}", RegexOptions.Compiled)]
    private static partial Regex TableColumnSeperatorRegex();
}