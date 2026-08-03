namespace E_POS.Application.Modules.Tenant.OutletTillDevice.Options;

public sealed class TillMonitoringOptions
{
    public const string SectionName = "TillMonitoring";

    public int HeartbeatTimeoutSeconds { get; set; } = 300;
}
