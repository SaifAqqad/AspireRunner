namespace AspireRunner.Core.Helpers;

public static class EnvironmentVariables
{
    public static string? NugetRepoUrl => GetValue("ASPIRE_RUNNER_NUGET_REPO");

    public static string? RunnerPath => GetValue("ASPIRE_RUNNER_PATH");

    public static string? DotnetHostPath => GetValue("DOTNET_HOST_PATH");

    public static string[] Paths
    {
        get
        {
            var paths = GetValue("PATH")?.Split(Path.PathSeparator) ?? [];
            if (PlatformHelper.IsWsl())
            {
                // exclude windows paths to avoid conflicts
                paths = paths.Where(p => !p.StartsWith("/mnt/c/")).ToArray();
            }

            return paths;
        }
    }

    private static string? GetValue(string name) => Environment.GetEnvironmentVariable(name);

    private static bool IsEnabled(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);
        return value is not null && bool.TryParse(value, out var result) && result;
    }
}