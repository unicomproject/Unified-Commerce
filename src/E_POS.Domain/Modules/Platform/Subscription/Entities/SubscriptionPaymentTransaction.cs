using E_POS.Domain.Common.Entities;
using E_POS.Domain.Modules.Platform.Subscription.Constants;

namespace E_POS.Domain.Modules.Platform.Subscription.Entities;

public class SubscriptionPaymentTransaction : AuditableEntity
{
    public decimal Amount { get; protected set; }
    public string ProviderTransactionReference { get; protected set; } = string.Empty;
    public Guid SubscriptionInvoiceId { get; protected set; }
    public Guid? SubscriptionPaymentLinkId { get; protected set; }
    public Guid TenantId { get; protected set; }
    public Guid InvoiceId { get; protected set; }
    public Guid? PaymentLinkId { get; protected set; }
    public string TransactionType { get; protected set; } = string.Empty;
    public string ProviderName { get; protected set; } = string.Empty;
    public string? ProviderTransactionId { get; protected set; }
    public string? IdempotencyKey { get; protected set; }
    public string TransactionStatus { get; protected set; } = string.Empty;
    public string CurrencyCode { get; protected set; } = string.Empty;
    public decimal ProviderFee { get; protected set; }
    public decimal NetAmount { get; protected set; }
    public DateTimeOffset? PaidAt { get; protected set; }
    public DateTimeOffset? FailedAt { get; protected set; }
    public string? FailureReason { get; protected set; }
    public string? ProviderResponseJson { get; protected set; }
    public Guid TenantSubscriptionId { get; protected set; }
    public decimal ExpectedAmount { get; protected set; }
    public decimal? SubmittedAmount { get; protected set; }
    public decimal? ApprovedAmount { get; protected set; }
    public string? PaymentMethod { get; protected set; }
    public string? ManualReference { get; protected set; }
    public string? ManualReferenceNormalized { get; protected set; }
    public DateTimeOffset? PaymentDate { get; protected set; }
    public DateTimeOffset? SubmittedAt { get; protected set; }
    public string? SubmittedByType { get; protected set; }
    public Guid? SubmittedById { get; protected set; }
    public string? PayerNote { get; protected set; }
    public DateTimeOffset? VerifiedAt { get; protected set; }
    public Guid? VerifiedByPlatformUserId { get; protected set; }
    public string? ReviewNote { get; protected set; }
    public string? RejectionReasonCode { get; protected set; }
    public string? FailureCode { get; protected set; }
    public string? LastCommandIdempotencyKeyHash { get; protected set; }
    public string? LastCommandRequestHash { get; protected set; }
    public long SubmissionVersion { get; protected set; }
    public long Version { get; protected set; } = 1;
    public string? ProviderEventId { get; protected set; }
    public string? ProviderCheckoutUrl { get; protected set; }
    public string? ProviderCustomerReferenceId { get; protected set; }
    public string? ProviderStatus { get; protected set; }
    public string? ProviderCallbackReceiptJson { get; protected set; }

    public static SubscriptionPaymentTransaction CreatePending(
        Guid id,
        Guid tenantId,
        Guid tenantSubscriptionId,
        Guid subscriptionInvoiceId,
        Guid subscriptionPaymentLinkId,
        decimal amount,
        string currencyCode,
        string providerName,
        string providerTransactionReference,
        DateTimeOffset now,
        string? transactionType = null,
        string? idempotencyKey = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(currencyCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(providerName);
        ArgumentException.ThrowIfNullOrWhiteSpace(providerTransactionReference);

        var normalizedAmount = Math.Max(0m, amount);
        var normalizedReference = providerTransactionReference.Trim();
        var normalizedType = string.IsNullOrWhiteSpace(transactionType)
            ? SubscriptionBillingAlignmentConstants.PaymentTransactionTypePayment
            : transactionType.Trim().ToUpperInvariant();

        return new SubscriptionPaymentTransaction
        {
            Id = id,
            TenantId = tenantId,
            TenantSubscriptionId = tenantSubscriptionId,
            SubscriptionInvoiceId = subscriptionInvoiceId,
            InvoiceId = subscriptionInvoiceId,
            SubscriptionPaymentLinkId = subscriptionPaymentLinkId,
            PaymentLinkId = subscriptionPaymentLinkId,
            Amount = normalizedAmount,
            ExpectedAmount = normalizedAmount,
            CurrencyCode = currencyCode.Trim().ToUpperInvariant(),
            ProviderName = providerName.Trim(),
            ProviderTransactionReference = normalizedReference,
            ProviderTransactionId = normalizedReference,
            TransactionType = normalizedType,
            TransactionStatus = SubscriptionBillingAlignmentConstants.PaymentTransactionStatusPending,
            IdempotencyKey = NormalizeOptional(idempotencyKey),
            ProviderFee = 0m,
            NetAmount = normalizedAmount,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public static SubscriptionPaymentTransaction CreateAwaitingManual(Guid id, Guid tenantId,
        Guid tenantSubscriptionId, Guid invoiceId, decimal expectedAmount, string currencyCode,
        string reference, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(currencyCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(reference);
        if (expectedAmount < 0) throw new ArgumentOutOfRangeException(nameof(expectedAmount));
        var normalizedReference = reference.Trim();
        return new SubscriptionPaymentTransaction
        {
            Id = id,
            TenantId = tenantId,
            TenantSubscriptionId = tenantSubscriptionId,
            SubscriptionInvoiceId = invoiceId,
            InvoiceId = invoiceId,
            Amount = expectedAmount,
            ExpectedAmount = expectedAmount,
            CurrencyCode = currencyCode.Trim().ToUpperInvariant(),
            ProviderName = ManualPaymentConstants.Provider,
            ProviderTransactionReference = normalizedReference,
            ProviderTransactionId = null,
            TransactionType = ManualPaymentConstants.TransactionType,
            TransactionStatus = ManualPaymentConstants.AwaitingPayment,
            ProviderFee = 0,
            NetAmount = expectedAmount,
            Version = 1,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public string SubmitManual(decimal submittedAmount, string currencyCode, string method, string reference,
        DateTimeOffset paymentDate, string? payerNote, string keyHash, string requestHash,
        string submittedByType, Guid? submittedById, DateTimeOffset now)
    {
        if (!ManualPaymentConstants.CanSubmit(TransactionStatus))
            throw new InvalidOperationException("Payment is not eligible for submission.");
        if (submittedAmount <= 0) throw new ArgumentOutOfRangeException(nameof(submittedAmount));
        ArgumentException.ThrowIfNullOrWhiteSpace(currencyCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(method);
        ArgumentException.ThrowIfNullOrWhiteSpace(reference);
        var before = TransactionStatus;
        SubmittedAmount = submittedAmount;
        PaymentMethod = method.Trim().ToUpperInvariant();
        ManualReference = reference.Trim();
        ManualReferenceNormalized = NormalizeReference(reference);
        PaymentDate = paymentDate;
        PayerNote = NormalizeOptional(payerNote);
        SubmittedAt = now;
        SubmittedByType = submittedByType.Trim().ToUpperInvariant();
        SubmittedById = submittedById;
        LastCommandIdempotencyKeyHash = keyHash;
        LastCommandRequestHash = requestHash;
        TransactionStatus = ManualPaymentConstants.PaymentSubmitted;
        SubmissionVersion++;
        Version++;
        UpdatedAt = now;
        return before;
    }

    public string BeginReview(DateTimeOffset now)
    {
        if (!ManualPaymentConstants.CanReview(TransactionStatus))
            throw new InvalidOperationException("Payment is not eligible for review.");
        var before = TransactionStatus;
        TransactionStatus = ManualPaymentConstants.UnderReview;
        Version++;
        UpdatedAt = now;
        return before;
    }

    public void Approve(Guid reviewerId, decimal approvedAmount, string? note, DateTimeOffset now)
    {
        if (TransactionStatus != ManualPaymentConstants.UnderReview)
            throw new InvalidOperationException("Payment must be under review before approval.");
        TransactionStatus = ManualPaymentConstants.Paid;
        ApprovedAmount = approvedAmount;
        Amount = approvedAmount;
        NetAmount = approvedAmount;
        PaidAt = now;
        VerifiedAt = now;
        VerifiedByPlatformUserId = reviewerId;
        ReviewNote = NormalizeOptional(note);
        RejectionReasonCode = null;
        Version++;
        UpdatedAt = now;
    }

    public void Reject(Guid reviewerId, string reasonCode, string note, DateTimeOffset now)
        => CompleteNegativeReview(ManualPaymentConstants.Rejected, reviewerId, reasonCode, note, now);

    public void RequestInformation(Guid reviewerId, string reasonCode, string note, DateTimeOffset now)
        => CompleteNegativeReview(ManualPaymentConstants.ActionRequired, reviewerId, reasonCode, note, now);

    private void CompleteNegativeReview(string status, Guid reviewerId, string reasonCode, string note, DateTimeOffset now)
    {
        if (TransactionStatus != ManualPaymentConstants.UnderReview)
            throw new InvalidOperationException("Payment must be under review before review completion.");
        ArgumentException.ThrowIfNullOrWhiteSpace(reasonCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(note);
        TransactionStatus = status;
        VerifiedAt = now;
        VerifiedByPlatformUserId = reviewerId;
        RejectionReasonCode = reasonCode.Trim().ToUpperInvariant();
        ReviewNote = note.Trim();
        PaidAt = null;
        Version++;
        UpdatedAt = now;
    }

    public void MarkSucceeded(DateTimeOffset now, decimal? providerFee = null, string? providerResponseJson = null)
    {
        var normalizedFee = Math.Max(0m, providerFee ?? 0m);
        ProviderFee = normalizedFee;
        NetAmount = Math.Max(0m, Amount - normalizedFee);
        TransactionStatus = SubscriptionBillingAlignmentConstants.PaymentTransactionStatusSucceeded;
        PaidAt = now;
        ProviderResponseJson = NormalizeOptional(providerResponseJson);
        FailureReason = null;
        FailedAt = null;
        UpdatedAt = now;
    }

    public void MarkFailed(DateTimeOffset now, string failureReason, string? providerResponseJson = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(failureReason);

        TransactionStatus = SubscriptionBillingAlignmentConstants.PaymentTransactionStatusFailed;
        FailedAt = now;
        FailureReason = failureReason.Trim();
        ProviderResponseJson = NormalizeOptional(providerResponseJson);
        PaidAt = null;
        UpdatedAt = now;
    }

    private static string? NormalizeOptional(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static string NormalizeReference(string value) =>
        new(value.Trim().ToUpperInvariant().Where(char.IsLetterOrDigit).ToArray());
}
