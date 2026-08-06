using E_POS.Application.Modules.Tenant.OutletTillDevice.Dtos.TenantAdmin;

namespace E_POS.Application.Modules.Tenant.OutletTillDevice.Services;

public static class OutletOperationalHealthCalculator
{
    public const string HealthyStatus = "HEALTHY";
    public const string NeedsAttentionStatus = "NEEDS_ATTENTION";
    public const string CriticalStatus = "CRITICAL";
    public const string UnknownStatus = "UNKNOWN";

    public const string SeverityCritical = "CRITICAL";
    public const string SeverityWarning = "WARNING";
    public const string SeverityInfo = "INFO";

    public record TillHealthInput(
        Guid TillId,
        string TillCode,
        string TillName,
        string TillStatus,
        string DeviceStatus,
        DateTimeOffset? DeviceLastSeenAt);

    public record HealthCalculationResult(
        string Status,
        DateTimeOffset? LastActivityAt,
        IReadOnlyList<OutletOverviewAlertResponse> Alerts,
        int TotalActiveAlertCount);

    public static HealthCalculationResult Calculate(
        string outletStatus,
        IReadOnlyList<TillHealthInput> tills,
        int maxAlerts = 5)
    {
        var activeTills = tills
            .Where(t => string.Equals(t.TillStatus, "ACTIVE", StringComparison.OrdinalIgnoreCase))
            .ToList();

        DateTimeOffset? lastActivityAt = tills
            .Select(t => t.DeviceLastSeenAt)
            .Where(d => d.HasValue)
            .Max();

        if (activeTills.Count == 0)
        {
            return new HealthCalculationResult(
                Status: UnknownStatus,
                LastActivityAt: lastActivityAt,
                Alerts: Array.Empty<OutletOverviewAlertResponse>(),
                TotalActiveAlertCount: 0);
        }

        var allAlerts = new List<OutletOverviewAlertResponse>();
        int onlineCount = 0;
        int offlineCount = 0;

        var now = DateTimeOffset.UtcNow;

        foreach (var till in activeTills)
        {
            if (string.Equals(till.DeviceStatus, "Online", StringComparison.OrdinalIgnoreCase))
            {
                onlineCount++;
            }
            else
            {
                offlineCount++;
                allAlerts.Add(new OutletOverviewAlertResponse(
                    AlertId: $"TILL_OFFLINE_{till.TillId}",
                    Title: $"Till Offline: {till.TillName}",
                    Severity: SeverityWarning,
                    Description: $"Till '{till.TillCode}' is active but assigned device is offline or missing heartbeat.",
                    OccurredAt: till.DeviceLastSeenAt ?? now));
            }
        }

        var overallStatus = Classify(activeTills.Count, onlineCount);

        var sortedAlerts = allAlerts
            .OrderByDescending(a => GetSeverityPriority(a.Severity))
            .ThenByDescending(a => a.OccurredAt)
            .Take(maxAlerts)
            .ToList();

        return new HealthCalculationResult(
            Status: overallStatus,
            LastActivityAt: lastActivityAt,
            Alerts: sortedAlerts,
            TotalActiveAlertCount: allAlerts.Count);
    }

    private static int GetSeverityPriority(string severity) => severity switch
    {
        SeverityCritical => 3,
        SeverityWarning => 2,
        SeverityInfo => 1,
        _ => 0
    };

    public static string Classify(int activeTillCount, int onlineTillCount)
    {
        if (activeTillCount <= 0) return UnknownStatus;
        if (onlineTillCount >= activeTillCount) return HealthyStatus;
        return onlineTillCount <= 0 ? CriticalStatus : NeedsAttentionStatus;
    }
}
