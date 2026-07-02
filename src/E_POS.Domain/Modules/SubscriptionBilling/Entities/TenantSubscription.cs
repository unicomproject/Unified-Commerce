using E_POS.Domain.Common.Entities;
using E_POS.Domain.Modules.SubscriptionBilling.Constants;

namespace E_POS.Domain.Modules.SubscriptionBilling.Entities;

public class TenantSubscription : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public string SubscriptionNumber { get; protected set; } = string.Empty;
    public Guid SubscriptionPlanId { get; protected set; }
    public string SubscriptionStatus { get; protected set; } = string.Empty;
    public string BillingCycle { get; protected set; } = TenantSubscriptionBillingConstants.BillingCycleMonthly;
    public DateTimeOffset? TrialStartAt { get; protected set; }
    public DateTimeOffset? TrialEndAt { get; protected set; }
    public DateTimeOffset? BillingStartAt { get; protected set; }
    public DateTimeOffset? NextBillingAt { get; protected set; }
    public bool AutoRenew { get; protected set; } = true;
    public string? DiscountType { get; protected set; }
    public decimal? DiscountValue { get; protected set; }
    public decimal TaxPercentage { get; protected set; }
    public string? InvoiceEmail { get; protected set; }
    public string? PaymentMethod { get; protected set; }
    public string? Notes { get; protected set; }
    public int? MaxOutletsOverride { get; protected set; }
    public int? MaxTillsOverride { get; protected set; }
    public int? MaxUsersOverride { get; protected set; }

    public static TenantSubscription Create(
        Guid id,
        Guid tenantId,
        Guid subscriptionPlanId,
        string subscriptionStatus,
        DateTimeOffset createdAt)
    {
        return Create(
            id,
            tenantId,
            subscriptionPlanId,
            subscriptionStatus,
            TenantSubscriptionBillingConstants.BillingCycleMonthly,
            trialStartAt: null,
            trialEndAt: null,
            billingStartAt: null,
            nextBillingAt: null,
            autoRenew: true,
            discountType: null,
            discountValue: null,
            taxPercentage: 0m,
            invoiceEmail: null,
            paymentMethod: null,
            notes: null,
            maxOutletsOverride: null,
            maxTillsOverride: null,
            maxUsersOverride: null,
            createdAt);
    }

    public static TenantSubscription Create(
        Guid id,
        Guid tenantId,
        Guid subscriptionPlanId,
        string subscriptionStatus,
        string billingCycle,
        DateTimeOffset? trialStartAt,
        DateTimeOffset? trialEndAt,
        DateTimeOffset? billingStartAt,
        DateTimeOffset? nextBillingAt,
        bool autoRenew,
        string? discountType,
        decimal? discountValue,
        decimal taxPercentage,
        string? invoiceEmail,
        string? paymentMethod,
        string? notes,
        int? maxOutletsOverride,
        int? maxTillsOverride,
        int? maxUsersOverride,
        DateTimeOffset createdAt)
    {
        return new TenantSubscription
        {
            Id = id,
            TenantId = tenantId,
            SubscriptionNumber = $"SUB-{id.ToString("N")[..8]}",
            SubscriptionPlanId = subscriptionPlanId,
            SubscriptionStatus = subscriptionStatus,
            BillingCycle = billingCycle,
            TrialStartAt = trialStartAt,
            TrialEndAt = trialEndAt,
            BillingStartAt = billingStartAt,
            NextBillingAt = nextBillingAt,
            AutoRenew = autoRenew,
            DiscountType = NormalizeOptionalText(discountType),
            DiscountValue = discountValue,
            TaxPercentage = taxPercentage,
            InvoiceEmail = NormalizeOptionalText(invoiceEmail),
            PaymentMethod = NormalizeOptionalText(paymentMethod),
            Notes = NormalizeOptionalText(notes),
            MaxOutletsOverride = maxOutletsOverride,
            MaxTillsOverride = maxTillsOverride,
            MaxUsersOverride = maxUsersOverride,
            CreatedAt = createdAt,
            UpdatedAt = createdAt
        };
    }

    public void Activate(DateTimeOffset now)
    {
        SubscriptionStatus = TenantSubscriptionStatusConstants.Active;
        UpdatedAt = now;
    }

    public void ChangePlan(Guid subscriptionPlanId, DateTimeOffset now)
    {
        SubscriptionPlanId = subscriptionPlanId;
        UpdatedAt = now;
    }

    public void TouchUpdatedAt(DateTimeOffset now)
    {
        UpdatedAt = now;
    }

    private static string? NormalizeOptionalText(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }
}
