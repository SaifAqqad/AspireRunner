using System.Text.RegularExpressions;

namespace AspireRunner.Core.Helpers;

public partial class DotnetCli
{
#if NET7_0_OR_GREATER
    private static readonly Regex SdkOutputRegex = BuildSdkOutputRegex();

    private static readonly Regex RuntimeOutputRegex = BuildRuntimeOutputRegex();

    private static readonly Regex TableContentRegex = BuildTableContentRegex();

    private static readonly Regex TableColumnSeperatorRegex = BuildTableColumnSeperatorRegex();

    [GeneratedRegex(@"([\d\.\w\-]+?)\s+(?:\[(.+)\])?", RegexOptions.Compiled)]
    private static partial Regex BuildSdkOutputRegex();

    [GeneratedRegex(@"(.+?) ([\d\-_.\w]+?) \[(.+)\]", RegexOptions.Compiled)]
    private static partial Regex BuildRuntimeOutputRegex();

    [GeneratedRegex(@"-+\r?\n([\w\W]+?)\r?\n\r?\n", RegexOptions.Compiled)]
    private static partial Regex BuildTableContentRegex();

    [GeneratedRegex(@"\s{2,}", RegexOptions.Compiled)]
    private static partial Regex BuildTableColumnSeperatorRegex();

#else
    private static readonly Regex SdkOutputRegex = new(@"([\d\.\w\-]+?)\s+(?:\[(.+)\])?", RegexOptions.Compiled);

    private static readonly Regex RuntimeOutputRegex = new(@"(.+?) ([\d\-_.\w]+?) \[(.+)\]", RegexOptions.Compiled);

    private static readonly Regex TableContentRegex = new(@"-+\r?\n([\w\W]+?)\r?\n\r?\n", RegexOptions.Compiled);

    private static readonly Regex TableColumnSeperatorRegex = new(@"\s{2,}", RegexOptions.Compiled);
#endif
}