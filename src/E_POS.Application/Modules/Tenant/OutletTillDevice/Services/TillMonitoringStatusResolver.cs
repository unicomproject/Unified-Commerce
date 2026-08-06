using E_POS.Domain.Modules.Tenant.OutletTillDevice.Constants;

namespace E_POS.Application.Modules.Tenant.OutletTillDevice.Services;

public static class TillMonitoringStatusResolver
{
    public sealed record ResolvedStatus(
        string OperationalStatus,
        bool NeedsAttention,
        string DisplayStatus,
        IReadOnlyList<string> AttentionReasons);

    public static ResolvedStatus Resolve(
        string tillLifecycleStatus,
        bool hasActiveAssignment,
        string? assignedDeviceStatus,
        bool isPosDeviceTrusted,
        DateTimeOffset? lastDeviceSeenAt,
        DateTimeOffset now,
        int heartbeatTimeoutSeconds)
    {
        var reasons = new List<string>();
        var isOnline = true;

        if (tillLifecycleStatus == TillConstants.InactiveStatus)
        {
            isOnline = false;
        }
        else if (tillLifecycleStatus == TillConstants.MaintenanceStatus)
        {
            isOnline = false;
            reasons.Add("TILL_MAINTENANCE");
        }
        else if (tillLifecycleStatus == TillConstants.ActiveStatus)
        {
            if (!hasActiveAssignment)
            {
                isOnline = false;
                reasons.Add("NO_ACTIVE_POS_DEVICE_ASSIGNMENT");
            }
            else
            {
                if (assignedDeviceStatus != PosDeviceConstants.ActiveStatus)
                {
                    isOnline = false;
                    reasons.Add("POS_DEVICE_INACTIVE");
                }

                if (!isPosDeviceTrusted)
                {
                    isOnline = false;
                    reasons.Add("POS_DEVICE_NOT_TRUSTED");
                }

                if (!lastDeviceSeenAt.HasValue)
                {
                    isOnline = false;
                    reasons.Add("POS_DEVICE_HEARTBEAT_MISSING");
                }
                else
                {
                    var heartbeatAge = now - lastDeviceSeenAt.Value;
                    if (heartbeatAge.TotalSeconds > heartbeatTimeoutSeconds)
                    {
                        isOnline = false;
                        reasons.Add("POS_DEVICE_HEARTBEAT_STALE");
                    }
                }
            }
        }
        else
        {
            isOnline = false;
        }

        var operationalStatus = isOnline ? "ONLINE" : "OFFLINE";
        var needsAttention = reasons.Count > 0;
        
        string displayStatus;
        if (needsAttention)
        {
            displayStatus = "NEEDS_ATTENTION";
        }
        else if (isOnline)
        {
            displayStatus = "ONLINE";
        }
        else
        {
            displayStatus = "OFFLINE";
        }

        return new ResolvedStatus(operationalStatus, needsAttention, displayStatus, reasons);
    }
}
