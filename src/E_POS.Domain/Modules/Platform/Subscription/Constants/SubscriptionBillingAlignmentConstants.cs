namespace E_POS.Domain.Modules.Platform.Subscription.Constants;

public static class SubscriptionBillingAlignmentConstants
{
    public const string InvoiceTypeSubscription = "SUBSCRIPTION";

    public const string PaymentLinkStatusActive = "ACTIVE";
    public const string PaymentLinkStatusExpired = "EXPIRED";
    public const string PaymentLinkStatusRevoked = "REVOKED";
    public const string PaymentLinkStatusUsed = "USED";

    public const string PaymentTransactionTypePayment = "PAYMENT";

    public const string PaymentTransactionStatusPending = "PENDING";
    public const string PaymentTransactionStatusSucceeded = "SUCCEEDED";
    public const string PaymentTransactionStatusFailed = "FAILED";

    public const string CreditNoteStatusDraft = "DRAFT";
    public const string CreditNoteStatusIssued = "ISSUED";
    public const string CreditNoteStatusApplied = "APPLIED";

    public const string InvoiceLineTypePlan = "PLAN";
    public const string InvoiceLineTypeAddon = "ADDON";
}
