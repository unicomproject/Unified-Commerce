namespace E_POS.Domain.Modules.Tenant.TenantFoundation.Constants;

/// <summary>
/// DATA MIGRATION RULES only — maps legacy polluted <c>tenants.status</c> values.
/// Not approved future workflow states.
/// </summary>
public static class TenantLifecycleLegacyMapper
{
    public sealed record MapResult(string LifecycleStatus, bool IsUnknown);

    public static MapResult Map(string? rawStatus, DateTimeOffset? activatedAt, bool? isActiveEvidence = null)
    {
        var normalized = string.IsNullOrWhiteSpace(rawStatus)
            ? string.Empty
            : rawStatus.Trim().ToLowerInvariant();

        if (TenantStatusConstants.IsApproved(normalized))
        {
            return new MapResult(TenantStatusConstants.Normalize(normalized), IsUnknown: false);
        }

        if (normalized is "cancelled" or "canceled")
        {
            return new MapResult(TenantStatusConstants.Cancelled, IsUnknown: false);
        }

        if (normalized is "suspended")
        {
            return new MapResult(TenantStatusConstants.Suspended, IsUnknown: false);
        }

        var hasActivationEvidence = activatedAt.HasValue || isActiveEvidence == true;

        // Activation evidence takes priority over billing labels, except explicit suspended/cancelled above.
        if (hasActivationEvidence &&
            normalized is not ("pending" or "unpaid" or "overdue" or "failed"))
        {
            if (normalized is "inactive")
            {
                return new MapResult(TenantStatusConstants.Suspended, IsUnknown: false);
            }

            if (normalized is "paid" or "verified" or "waived" or "setup_pending")
            {
                return new MapResult(TenantStatusConstants.Active, IsUnknown: false);
            }
        }

        return normalized switch
        {
            "pending" or "unpaid" or "overdue" or "failed" =>
                new MapResult(TenantStatusConstants.PendingPayment, IsUnknown: false),
            "paid" or "verified" or "waived" =>
                new MapResult(TenantStatusConstants.PendingActivation, IsUnknown: false),
            "setup_pending" =>
                new MapResult(TenantStatusConstants.Active, IsUnknown: false),
            "inactive" when hasActivationEvidence =>
                new MapResult(TenantStatusConstants.Suspended, IsUnknown: false),
            "inactive" =>
                new MapResult(TenantStatusConstants.Draft, IsUnknown: false),
            _ => new MapResult(normalized, IsUnknown: true)
        };
    }
}
