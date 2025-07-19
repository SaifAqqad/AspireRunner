using System.Reflection;

namespace AspireRunner.Tool;

public static class Runner
{
    private static readonly Dictionary<string, string?> AssemblyMetadata = Assembly.GetEntryAssembly()?
        .GetCustomAttributes<AssemblyMetadataAttribute>()
        .ToDictionary(a => a.Key, a => a.Value) ?? [];

    public static Version Version { get; } = new(AssemblyMetadata["Version"]);

    public static string ProjectUrl { get; } = AssemblyMetadata["PackageProjectUrl"]!;

    public static string CommandName { get; } = AssemblyMetadata["CommandName"]!;
}