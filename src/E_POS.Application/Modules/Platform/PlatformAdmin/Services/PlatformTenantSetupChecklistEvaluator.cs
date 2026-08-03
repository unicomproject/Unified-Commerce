namespace E_POS.Application.Modules.Platform.PlatformAdmin.Services;

/// <summary>
/// Shared Setup Pending checklist evaluator (Dashboard count population + tenant list/detail).
/// Mandatory steps align with Create Tenant Wizard + Tenant Activation contracts.
/// Outlets/tills are intentionally optional and never block completion.
/// </summary>
public static class PlatformTenantSetupChecklistEvaluator
{
    public const string StepBusinessProfile = "business_profile";
    public const string StepSubscriptionPlan = "subscription_plan";
    public const string StepEntitlements = "entitlements";
    public const string StepBillingCondition = "billing_condition";
    public const string StepTenantAdmin = "tenant_admin";

    public static readonly IReadOnlyList<string> MandatorySteps =
    [
        StepBusinessProfile,
        StepSubscriptionPlan,
        StepEntitlements,
        StepBillingCondition,
        StepTenantAdmin
    ];

    public sealed record ChecklistInput(
        bool HasBusinessProfile,
        bool HasSubscriptionPlan,
        bool HasEnabledEntitlements,
        bool BillingConditionSatisfied,
        bool HasTenantAdmin);

    public sealed record ChecklistResult(
        IReadOnlyList<string> CompletedSteps,
        IReadOnlyList<string> MissingSteps,
        int ProgressPercent,
        bool IsComplete);

    public static ChecklistResult Evaluate(ChecklistInput input)
    {
        var completed = new List<string>(MandatorySteps.Count);
        var missing = new List<string>(MandatorySteps.Count);

        void Check(string step, bool ok)
        {
            if (ok)
            {
                completed.Add(step);
            }
            else
            {
                missing.Add(step);
            }
        }

        Check(StepBusinessProfile, input.HasBusinessProfile);
        Check(StepSubscriptionPlan, input.HasSubscriptionPlan);
        Check(StepEntitlements, input.HasEnabledEntitlements);
        Check(StepBillingCondition, input.BillingConditionSatisfied);
        Check(StepTenantAdmin, input.HasTenantAdmin);

        var percent = MandatorySteps.Count == 0
            ? 100
            : (int)Math.Round(completed.Count * 100d / MandatorySteps.Count, MidpointRounding.AwayFromZero);

        return new ChecklistResult(completed, missing, percent, missing.Count == 0);
    }

    public static bool IsBillingConditionSatisfied(string? tenantBillingStatus, string? subscriptionStatus)
    {
        if (string.Equals(subscriptionStatus, "TRIAL", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return string.Equals(tenantBillingStatus, "PAID", StringComparison.OrdinalIgnoreCase)
               || string.Equals(tenantBillingStatus, "WAIVED", StringComparison.OrdinalIgnoreCase)
               || string.Equals(tenantBillingStatus, "CURRENT", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// List/detail billing gate for Setup Pending: pending_payment lifecycle and pending invoices
    /// always leave billing incomplete even when the subscription is TRIAL.
    /// </summary>
    public static bool IsSetupBillingSatisfied(
        string? tenantBillingStatus,
        string? subscriptionStatus,
        bool hasPendingInvoice,
        bool isPendingPaymentStatus)
    {
        if (isPendingPaymentStatus || hasPendingInvoice)
        {
            return false;
        }

        return IsBillingConditionSatisfied(tenantBillingStatus, subscriptionStatus);
    }

    public static bool IsTenantAdminStatus(string? accountStatus) =>
        string.Equals(accountStatus, "INVITED", StringComparison.OrdinalIgnoreCase)
        || string.Equals(accountStatus, "ACTIVE", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Approved Continue Setup destination is the tenant detail / activation surface (not a wizard deep-link).
    /// Missing-step order is exposed via checklist; UI resumes on detail.
    /// </summary>
    public static string ContinueSetupPath(Guid tenantId) => $"/admin/tenants/{tenantId}";

    public static string? FirstMissingMandatoryStep(ChecklistResult result) =>
        result.MissingSteps.Count == 0 ? null : result.MissingSteps[0];
}
