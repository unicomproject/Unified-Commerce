namespace E_POS.Domain.Modules.Platform.Subscription.Constants;

public static class ManualPaymentConstants
{
    public const string Provider = "MANUAL";
    public const string TransactionType = "PAYMENT";

    public const string AwaitingPayment = "AWAITING_PAYMENT";
    public const string PaymentSubmitted = "PAYMENT_SUBMITTED";
    public const string UnderReview = "UNDER_REVIEW";
    public const string Paid = "PAID";
    public const string Rejected = "REJECTED";
    public const string ActionRequired = "ACTION_REQUIRED";
    public const string Failed = "FAILED";
    public const string Expired = "EXPIRED";
    public const string Cancelled = "CANCELLED";
    public const string Deferred = "DEFERRED";
    public const string NotRequired = "NOT_REQUIRED";

    public const string Approve = "APPROVE";
    public const string Reject = "REJECT";
    public const string RequestInformation = "REQUEST_INFORMATION";

    public const string AccessPurpose = "MANUAL_PAYMENT";
    public const string AccessPendingDelivery = "PENDING_DELIVERY";
    public const string AccessActive = "ACTIVE";
    public const string AccessRevoked = "REVOKED";
    public const string AccessExpired = "EXPIRED";

    public const string ScanPending = "PENDING";
    public const string ScanClean = "CLEAN";
    public const string ScanRejected = "REJECTED";
    public const string ScanUnavailable = "UNAVAILABLE";

    public static bool CanSubmit(string status) => status is AwaitingPayment or ActionRequired or Rejected;

    public static bool CanReview(string status) => status is PaymentSubmitted or UnderReview;
}
