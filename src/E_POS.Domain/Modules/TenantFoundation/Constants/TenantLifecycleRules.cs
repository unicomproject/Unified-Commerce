namespace E_POS.Domain.Modules.TenantFoundation.Constants;

public static class TenantLifecycleRules
{
    private static readonly HashSet<string> ActivatableTenantStatuses =
    [
        TenantStatusConstants.SetupPending,
        TenantStatusConstants.PendingPayment,
        TenantStatusConstants.PendingActivation,
        TenantStatusConstants.Inactive,
        TenantStatusConstants.Draft
    ];

    private static readonly HashSet<string> SuspendableTenantStatuses =
    [
        TenantStatusConstants.Active
    ];

    public static bool CanActivate(string? tenantStatus)
    {
        return ActivatableTenantStatuses.Contains(Normalize(tenantStatus));
    }

    public static bool CanSuspend(string? tenantStatus, string? subscriptionStatus)
    {
        return SuspendableTenantStatuses.Contains(Normalize(tenantStatus)) ||
               IsSubscriptionStatus(subscriptionStatus, "TRIAL");
    }

    private static string Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToLowerInvariant();

    private static bool IsSubscriptionStatus(string? value, string expected) =>
        !string.IsNullOrWhiteSpace(value) &&
        string.Equals(value, expected, StringComparison.OrdinalIgnoreCase);
}
