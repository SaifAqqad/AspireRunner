namespace AspireRunner.Core.Helpers;

public static class UrlHelper
{
    public static string BuildLocalUrl(int port, bool secure = false, string? hostname = null)
    {
        var protocol = secure ? "https" : "http";
        if (string.IsNullOrWhiteSpace(hostname))
        {
            hostname = "localhost";
        }

        return $"{protocol}://{hostname}:{port}";
    }

    public static string ReplaceDefaultRoute(string url, string? hostname = null)
    {
        var ipv4 = hostname ?? "127.0.0.1";
        var ipv6 = hostname ?? "[::1]";

        return url.Replace("*", ipv4)
            .Replace("+", ipv4)
            .Replace("0.0.0.0", ipv4)
            .Replace("[::]", ipv6);
    }

    public static Dictionary<string, string> GetQuery(string url)
    {
        return url.IndexOf('?') is > -1 and var queryStart ?
            url[(queryStart + 1)..].Split('&', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(q => q.Split('=', 2))
                .ToDictionary(p => p[0], p => p[1])
            : new Dictionary<string, string>();
    }
}