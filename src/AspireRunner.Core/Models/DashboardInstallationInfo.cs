namespace AspireRunner.Core.Models;

internal record DashboardInstallationInfo
{
    public required string Path { get; init; }

    public required Version Version { get; init; }
}