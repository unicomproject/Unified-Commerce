namespace E_POS.Domain.Modules.Platform.Subscription.Constants;

public static class TenantSubscriptionBillingConstants
{
    public const string BillingCycleMonthly = "monthly";
    public const string BillingCycleYearly = "yearly";

    public const string DiscountTypeFixed = "fixed";
    public const string DiscountTypePercent = "percent";

    public const string InvoiceStatusDraft = "DRAFT";
    public const string InvoiceStatusPending = "PENDING";
    public const string InvoiceStatusPaid = "PAID";

    public static readonly IReadOnlyList<string> BillingCycles =
    [
        BillingCycleMonthly,
        BillingCycleYearly
    ];

    public static readonly IReadOnlyList<(string Value, string Label)> PaymentMethods =
    [
        ("manual", "Manual"),
        ("bank_transfer", "Bank Transfer")
    ];
}

