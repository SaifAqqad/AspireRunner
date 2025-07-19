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
}