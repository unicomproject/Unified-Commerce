using E_POS.Application.Modules.Tenant.OutletTillDevice.Dtos.TenantAdmin;

namespace E_POS.Application.Modules.Tenant.OutletTillDevice.Services;

public static class HardwareAttentionReasonBuilder
{
    public const string SeverityInfo = "INFO";
    public const string SeverityWarning = "WARNING";
    public const string SeverityError = "ERROR";
    public const string SeverityCritical = "CRITICAL";

    public static IReadOnlyList<TenantAdminTillAttentionReasonResponse> Build(
        IReadOnlyList<string> tillAttentionReasonCodes,
        IReadOnlyList<TenantAdminHardwareConnectionResponse> connections,
        bool hasActivePosAssignment,
        DateTimeOffset now)
    {
        var reasons = new List<TenantAdminTillAttentionReasonResponse>();

        foreach (var code in tillAttentionReasonCodes.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            reasons.Add(MapTillReason(code, now));
        }

        if (!hasActivePosAssignment &&
            !reasons.Any(r => r.Code == "POS_DEVICE_NOT_ASSIGNED" || r.Code == "NO_ACTIVE_POS_DEVICE_ASSIGNMENT"))
        {
            reasons.Add(new TenantAdminTillAttentionReasonResponse(
                "POS_DEVICE_NOT_ASSIGNED",
                SeverityWarning,
                "No active POS device is assigned to this till.",
                null,
                null,
                now));
        }

        if (connections.Count == 0)
        {
            reasons.Add(new TenantAdminTillAttentionReasonResponse(
                "HARDWARE_NOT_ASSIGNED",
                SeverityInfo,
                "No hardware devices are assigned to this till.",
                null,
                null,
                now));
        }

        foreach (var connection in connections)
        {
            if (string.IsNullOrWhiteSpace(connection.WarningCode) &&
                connection.ConnectionStatus is HardwareConnectionStatusResolver.Connected
                    or HardwareConnectionStatusResolver.NotAssigned)
            {
                continue;
            }

            var code = connection.WarningCode
                       ?? connection.ConnectionStatus switch
                       {
                           HardwareConnectionStatusResolver.Disconnected => "HARDWARE_HEARTBEAT_EXPIRED",
                           HardwareConnectionStatusResolver.Unknown => "HARDWARE_STATUS_UNKNOWN",
                           HardwareConnectionStatusResolver.Maintenance => "DEVICE_MAINTENANCE",
                           HardwareConnectionStatusResolver.NeedsAttention => "HARDWARE_TEST_WARNING",
                           _ => "HARDWARE_STATUS_UNKNOWN",
                       };

            var severity = connection.HealthStatus switch
            {
                HardwareConnectionStatusResolver.HealthFailed => SeverityError,
                HardwareConnectionStatusResolver.HealthWarning => SeverityWarning,
                _ when connection.ConnectionStatus == HardwareConnectionStatusResolver.Disconnected => SeverityWarning,
                _ when connection.ConnectionStatus == HardwareConnectionStatusResolver.Maintenance => SeverityWarning,
                _ => SeverityWarning,
            };

            reasons.Add(new TenantAdminTillAttentionReasonResponse(
                code,
                severity,
                connection.WarningMessage ?? $"Hardware {connection.HardwareDeviceName} requires attention.",
                connection.HardwareDeviceId,
                connection.HardwareDeviceType,
                connection.LastSeenAt ?? now));
        }

        return Deduplicate(reasons);
    }

    public static int CalculateAlertCount(IEnumerable<TenantAdminTillAttentionReasonResponse> reasons)
    {
        return Deduplicate(reasons)
            .Count(r =>
                string.Equals(r.Severity, SeverityWarning, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(r.Severity, SeverityError, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(r.Severity, SeverityCritical, StringComparison.OrdinalIgnoreCase));
    }

    private static TenantAdminTillAttentionReasonResponse MapTillReason(string code, DateTimeOffset now)
    {
        return code.Trim().ToUpperInvariant() switch
        {
            "TILL_MAINTENANCE" => new(
                "DEVICE_MAINTENANCE",
                SeverityWarning,
                "Till is in maintenance.",
                null,
                null,
                now),
            "NO_ACTIVE_POS_DEVICE_ASSIGNMENT" => new(
                "POS_DEVICE_NOT_ASSIGNED",
                SeverityWarning,
                "No active POS device is assigned to this till.",
                null,
                null,
                now),
            "POS_DEVICE_INACTIVE" => new(
                "POS_DEVICE_OFFLINE",
                SeverityWarning,
                "Assigned POS device is inactive.",
                null,
                null,
                now),
            "POS_DEVICE_NOT_TRUSTED" => new(
                "POS_DEVICE_OFFLINE",
                SeverityWarning,
                "Assigned POS device is not trusted.",
                null,
                null,
                now),
            "POS_DEVICE_HEARTBEAT_MISSING" => new(
                "POS_DEVICE_OFFLINE",
                SeverityWarning,
                "Assigned POS device has never reported a heartbeat.",
                null,
                null,
                now),
            "POS_DEVICE_HEARTBEAT_STALE" => new(
                "POS_DEVICE_OFFLINE",
                SeverityWarning,
                "Assigned POS device heartbeat has expired.",
                null,
                null,
                now),
            _ => new(code, SeverityWarning, code, null, null, now),
        };
    }

    private static List<TenantAdminTillAttentionReasonResponse> Deduplicate(
        IEnumerable<TenantAdminTillAttentionReasonResponse> reasons)
    {
        return reasons
            .GroupBy(
                r => $"{r.Code}|{r.HardwareDeviceId?.ToString() ?? "none"}",
                StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToList();
    }
}
