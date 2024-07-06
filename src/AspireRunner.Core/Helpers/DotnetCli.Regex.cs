using System.Text.RegularExpressions;

namespace AspireRunner.Core.Helpers;

public partial class DotnetCli
{
    private static readonly Regex SdkOutputRegex = new(@"([\d\.\w\-]+?)\s+(?:\[(.+)\])?", RegexOptions.Compiled);

    private static readonly Regex RuntimeOutputRegex = new(@"(.+?) ([\d\-_.\w]+?) \[(.+)\]", RegexOptions.Compiled);

    private static readonly Regex TableContentRegex = new(@"-+\r?\n([\w\W]+?)\r?\n\r?\n", RegexOptions.Compiled);

    private static readonly Regex TableColumnSeperatorRegex = new(@"\s{2,}", RegexOptions.Compiled);
}