namespace E_POS.Domain.Modules.Tenant.TenantFoundation.Constants;

public static class TenantLifecycleRules
{
    private static readonly HashSet<string> ActivatableTenantStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        TenantStatusConstants.PendingActivation,
        TenantStatusConstants.Draft
    };

    private static readonly HashSet<string> SuspendableTenantStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        TenantStatusConstants.Active
    };

    /// <summary>
    /// Lifecycle statuses that may enter activate orchestration.
    /// Paid <see cref="TenantStatusConstants.PendingPayment"/> is excluded —
    /// payment must be verified first (→ pending_activation).
    /// </summary>
    public static bool CanActivate(string? tenantStatus)
    {
        return ActivatableTenantStatuses.Contains(Normalize(tenantStatus));
    }

    public static bool CanSuspend(string? tenantStatus, string? subscriptionStatus = null)
    {
        _ = subscriptionStatus;
        return SuspendableTenantStatuses.Contains(Normalize(tenantStatus));
    }

    public static bool IsLoginAllowed(string? tenantStatus) =>
        string.Equals(Normalize(tenantStatus), TenantStatusConstants.Active, StringComparison.OrdinalIgnoreCase);

    private static string Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToLowerInvariant();
}
