namespace DoMin.Node.Core;

public static class CharMemoryExtensions
{
    public static string LeftPart(this string strVal, char needle)
    {
        if (strVal == null) return null!;
        var pos = strVal.IndexOf(needle);
        return pos == -1
            ? strVal
            : strVal.Substring(0, pos);
    }
    public static string RightPart(this string strVal, char needle)
    {
        if (strVal == null) return null!;
        var pos = strVal.IndexOf(needle);
        return pos == -1
            ? strVal
            : strVal.Substring(pos + 1);
    }
    public static string LastRightPart(this string strVal, char needle)
    {
        if (strVal == null) return null!;
        var pos = strVal.LastIndexOf(needle);
        return pos == -1
            ? strVal
            : strVal.Substring(pos + 1);
    }

}