namespace AspireRunner.Core;

public partial class AspireDashboard
{
    internal const string WorkloadId = "aspire";

    internal const string SdkName = "Aspire.Dashboard.Sdk";

    internal const string DllName = "Aspire.Dashboard.dll";

    internal const string AspRuntimeName = "Microsoft.AspNetCore.App";

    internal const string DataFolder = ".AspireRunner";

    internal const string DownloadFolder = "dashboard";

    internal const string LoginConsoleMessage = "Login to the dashboard at";

    internal const string DashboardStartedConsoleMessage = "Now listening on:";

    internal const int DefaultErrorLogDelay = 2000;

    internal static readonly Version MinimumRuntimeVersion = new(8, 0, 0);

    private const string InstanceFile = "aspire-dashboard.instance";
}