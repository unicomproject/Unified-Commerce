using E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;

namespace E_POS.Application.Modules.Platform.PlatformAdmin.Services;

/// <summary>
/// Pure overall health aggregation for Dashboard System Health (testable without live providers).
/// </summary>
public static class PlatformDashboardHealthAggregator
{
    public static string Aggregate(IReadOnlyList<PlatformDashboardHealthDependencyDto> dependencies)
    {
        if (dependencies.Count == 0)
        {
            return "UNKNOWN";
        }

        // Any critical dependency failure (CRITICAL or DEGRADED) impairs the core path → CRITICAL.
        // Critical UNKNOWN remains DEGRADED (configured / required but signals unavailable).
        if (dependencies.Any(x =>
                x.IsCritical &&
                (string.Equals(x.Status, "CRITICAL", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(x.Status, "DEGRADED", StringComparison.OrdinalIgnoreCase))))
        {
            return "CRITICAL";
        }

        if (dependencies.Any(x =>
                string.Equals(x.Status, "DEGRADED", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(x.Status, "CRITICAL", StringComparison.OrdinalIgnoreCase) ||
                (x.IsCritical && string.Equals(x.Status, "UNKNOWN", StringComparison.OrdinalIgnoreCase))))
        {
            return "DEGRADED";
        }

        if (dependencies.All(x => string.Equals(x.Status, "HEALTHY", StringComparison.OrdinalIgnoreCase)))
        {
            return "HEALTHY";
        }

        if (dependencies.All(x => string.Equals(x.Status, "UNKNOWN", StringComparison.OrdinalIgnoreCase)))
        {
            return "UNKNOWN";
        }

        return "DEGRADED";
    }
}
