using System.Text.RegularExpressions;

namespace AspireRunner.Extensions;

public static partial class StringExtensions
{
    public static Version ParseVersion(this string name)
    {
        var parts = BuildTypeRegex()
            .Replace(name, string.Empty)
            .Split('.', 4, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return new Version(
            int.Parse(parts.ElementAtOrDefault(0) ?? "0"),
            int.Parse(parts.ElementAtOrDefault(1) ?? "0"),
            int.Parse(parts.ElementAtOrDefault(2) ?? "0"),
            int.Parse(parts.ElementAtOrDefault(3)?.Replace(".", string.Empty) ?? "0")
        );
    }

    [GeneratedRegex(@"\-\w+", RegexOptions.Compiled)]
    private static partial Regex BuildTypeRegex();
}