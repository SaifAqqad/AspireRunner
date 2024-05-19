namespace AspireRunner.Core;

public partial class AspireDashboard
{
    private const string WorkloadId = "aspire";

    private const string SdkName = "Aspire.Dashboard.Sdk";

    private const string DllName = "Aspire.Dashboard.dll";

    private const string AspRuntimeName = "Microsoft.AspNetCore.App";

    private const string DataFolder = ".AspireRunner";

    private const string InstanceFile = "aspire-dashboard.pid";

    private const string LoginConsoleMessage = "Login to the dashboard at";

    private const string DashboardStartedConsoleMessage = "Now listening on:";

    private const int DefaultErrorLogDelay = 2000;
}