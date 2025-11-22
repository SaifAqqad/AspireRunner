using Range = SemanticVersioning.Range;

namespace AspireRunner.Core;

public partial class Dashboard
{
    public const string DownloadFolder = "dashboard";

    public const string RunnerFolder = ".AspireRunner";

    public const string DllName = "Aspire.Dashboard.dll";

    public const string RequiredRuntimeName = "Microsoft.AspNetCore.App";

    public const string InstanceFile = "aspire-dashboard.instance";

    private const int DefaultLogDelay = 500;

    private const int InstanceLockTimeout = 30;

    private const string InstanceLock = "aspire_dashboard";

    private const string LoginConsoleMessage = "Login to the dashboard at";

    private const string DashboardStartedConsoleMessage = "Now listening on:";

    public static readonly Version MinimumRuntimeVersion = new(8, 0, 0);

    /// <summary>
    /// Version compatibility matrix for previous runtimes
    /// </summary>
    /// <remarks>
    /// Any runtime version not in the matrix and newer than <see cref="Dashboard.MinimumRuntimeVersion">MinimumRuntimeVersion</see> is considered compatible with the latest dashboard version.
    /// </remarks>
    public static readonly (Range Runtime, Version LastSupportedVersion)[] VersionCompatibilityMatrix =
    [
        (new Range("8.x.x"), new Version(9, 5, 0)),
        (new Range("9.x.x"), new Version(9, 5, 0))
    ];

}