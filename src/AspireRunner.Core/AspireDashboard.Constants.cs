namespace AspireRunner.Core;

public partial class AspireDashboard
{
    internal const string SdkName = "Aspire.Dashboard.Sdk";

    internal const string DllName = "Aspire.Dashboard.dll";

    internal const string AspRuntimeName = "Microsoft.AspNetCore.App";

    internal const string DataFolder = ".AspireRunner";

    internal const string DownloadFolder = "dashboard";

    internal static readonly Version MinimumRuntimeVersion = new(8, 0, 0);

    private const int DefaultErrorLogDelay = 2000;

    private const int InstanceLockTimeout = 30;

    private const string InstanceLock = "aspire_dashboard";

    private const string InstanceFile = "aspire-dashboard.instance";

    private const string LoginConsoleMessage = "Login to the dashboard at";

    private const string DashboardStartedConsoleMessage = "Now listening on:";
}