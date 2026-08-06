namespace E_POS.Application.Modules.Tenant.OutletTillDevice.Services;

/// <summary>
/// Canonical connection-status vocabulary for Till hardware readiness.
/// Lifecycle status remains separate from connection status.
/// </summary>
public static class HardwareConnectionStatusResolver
{
    public const string Connected = "CONNECTED";
    public const string Disconnected = "DISCONNECTED";
    public const string NeedsAttention = "NEEDS_ATTENTION";
    public const string Maintenance = "MAINTENANCE";
    public const string NotAssigned = "NOT_ASSIGNED";
    public const string Unknown = "UNKNOWN";

    public const string HealthHealthy = "HEALTHY";
    public const string HealthWarning = "WARNING";
    public const string HealthFailed = "FAILED";
    public const string HealthUnknown = "UNKNOWN";

    public sealed record ResolvedConnection(
        string ConnectionStatus,
        string HealthStatus,
        string? WarningCode,
        string? WarningMessage);

    public static ResolvedConnection Resolve(
        string hardwareDeviceLifecycleStatus,
        string? latestTestStatus,
        string? latestTestMessage,
        DateTimeOffset? testedAt,
        DateTimeOffset? lastSeenAt,
        DateTimeOffset now,
        int heartbeatTimeoutSeconds)
    {
        var lifecycle = (hardwareDeviceLifecycleStatus ?? string.Empty).Trim().ToUpperInvariant();
        if (lifecycle == "MAINTENANCE")
        {
            return new ResolvedConnection(Maintenance, HealthUnknown, "DEVICE_MAINTENANCE", "Hardware device is in maintenance.");
        }

        if (lifecycle is "INACTIVE" or "DELETED" || string.IsNullOrWhiteSpace(lifecycle))
        {
            return new ResolvedConnection(Disconnected, HealthUnknown, null, null);
        }

        var testStatus = (latestTestStatus ?? string.Empty).Trim().ToUpperInvariant();
        var hasFreshHeartbeat = lastSeenAt.HasValue &&
                                (now - lastSeenAt.Value).TotalSeconds <= heartbeatTimeoutSeconds;
        var neverSeen = !lastSeenAt.HasValue;

        if (testStatus is "FAILED" or "ERROR" or "TIMEOUT")
        {
            return new ResolvedConnection(
                NeedsAttention,
                HealthFailed,
                "HARDWARE_TEST_FAILED",
                string.IsNullOrWhiteSpace(latestTestMessage) ? "Latest hardware test failed." : latestTestMessage);
        }

        if (testStatus is "WARNING")
        {
            return new ResolvedConnection(
                NeedsAttention,
                HealthWarning,
                "HARDWARE_TEST_WARNING",
                string.IsNullOrWhiteSpace(latestTestMessage) ? "Latest hardware test reported a warning." : latestTestMessage);
        }

        if (neverSeen)
        {
            return new ResolvedConnection(Unknown, HealthUnknown, "HARDWARE_HEARTBEAT_MISSING", "No hardware telemetry has been reported yet.");
        }

        if (!hasFreshHeartbeat)
        {
            return new ResolvedConnection(
                Disconnected,
                HealthUnknown,
                "HARDWARE_HEARTBEAT_EXPIRED",
                "Hardware heartbeat has expired.");
        }

        return new ResolvedConnection(Connected, HealthHealthy, null, null);
    }
}
