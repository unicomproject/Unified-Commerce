using E_POS.Domain.Modules.Platform.Subscription.Constants;

namespace E_POS.Infrastructure.Modules.Platform.Subscription.Entitlements;

internal static class TenantEntitlementEffectivePredicate
{
    public static bool IsEnabled(
        string entitlementStatus,
        bool isEnabled,
        DateTimeOffset? revokedAt,
        DateTimeOffset effectiveFrom,
        DateTimeOffset? effectiveUntil,
        DateTimeOffset now)
    {
        return revokedAt is null &&
               effectiveFrom <= now &&
               (effectiveUntil is null || effectiveUntil > now) &&
               (string.Equals(entitlementStatus, TenantEntitlementStatusConstants.Enabled, StringComparison.OrdinalIgnoreCase) ||
                isEnabled);
    }
}
