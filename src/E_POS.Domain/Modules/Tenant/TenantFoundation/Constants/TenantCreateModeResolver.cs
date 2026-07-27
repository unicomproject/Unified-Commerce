namespace E_POS.Domain.Modules.Tenant.TenantFoundation.Constants;

/// <summary>
/// Resolves create orchestration mode from subscription fields.
/// Subscription type/cycle/status must never be written into <c>tenants.status</c>.
/// </summary>
public static class TenantCreateModeResolver
{
    public const string DemoBillingCycle = "demo";

    public static TenantCreateMode Resolve(
        string? rawSubscriptionStatus,
        string? rawBillingCycle,
        bool isLegacyMinimalCreate = false)
    {
        if (isLegacyMinimalCreate)
        {
            return TenantCreateMode.Trial;
        }

        var cycle = Normalize(rawBillingCycle);
        var status = Normalize(rawSubscriptionStatus);

        if (cycle == DemoBillingCycle || status == DemoBillingCycle)
        {
            return TenantCreateMode.Demo;
        }

        if (string.IsNullOrEmpty(status) || status == "trial")
        {
            return TenantCreateMode.Trial;
        }

        return TenantCreateMode.Paid;
    }

    public static string InitialLifecycleStatus(TenantCreateMode mode) =>
        mode is TenantCreateMode.Trial or TenantCreateMode.Demo
            ? TenantStatusConstants.Draft
            : TenantStatusConstants.PendingPayment;

    private static string Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToLowerInvariant();
}
