namespace AspireRunner.Core;

public partial class Dashboard
{
    public const string DownloadFolder = "dashboard";

    public const string RunnerFolder = ".AspireRunner";

    public const string DllName = "Aspire.Dashboard.dll";

    public const string RequiredRuntimeName = "Microsoft.AspNetCore.App";

    public static readonly Version MinimumRuntimeVersion = new(8, 0, 0);

    private const int DefaultErrorLogDelay = 2000;

    private const int InstanceLockTimeout = 30;

    private const string InstanceLock = "aspire_dashboard";

    private const string InstanceFile = "aspire-dashboard.instance";

    private const string LoginConsoleMessage = "Login to the dashboard at";

    private const string DashboardStartedConsoleMessage = "Now listening on:";
}