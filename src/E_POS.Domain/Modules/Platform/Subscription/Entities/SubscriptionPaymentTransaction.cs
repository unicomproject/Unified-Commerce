using E_POS.Domain.Common.Entities;
using E_POS.Domain.Modules.Platform.Subscription.Constants;

namespace E_POS.Domain.Modules.Platform.Subscription.Entities;

public class SubscriptionPaymentTransaction : AuditableEntity
{
    public decimal Amount { get; protected set; }
    public string ProviderTransactionReference { get; protected set; } = string.Empty;
    public Guid SubscriptionInvoiceId { get; protected set; }
    public Guid SubscriptionPaymentLinkId { get; protected set; }
    public Guid TenantId { get; protected set; }
    public Guid InvoiceId { get; protected set; }
    public Guid? PaymentLinkId { get; protected set; }
    public string TransactionType { get; protected set; } = string.Empty;
    public string ProviderName { get; protected set; } = string.Empty;
    public string ProviderTransactionId { get; protected set; } = string.Empty;
    public string? IdempotencyKey { get; protected set; }
    public string TransactionStatus { get; protected set; } = string.Empty;
    public string CurrencyCode { get; protected set; } = "LKR";
    public decimal ProviderFee { get; protected set; }
    public decimal NetAmount { get; protected set; }
    public DateTimeOffset? PaidAt { get; protected set; }
    public DateTimeOffset? FailedAt { get; protected set; }
    public string? FailureReason { get; protected set; }
    public string? ProviderResponseJson { get; protected set; }

    public static SubscriptionPaymentTransaction CreatePending(
        Guid id,
        Guid tenantId,
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
            SubscriptionInvoiceId = subscriptionInvoiceId,
            InvoiceId = subscriptionInvoiceId,
            SubscriptionPaymentLinkId = subscriptionPaymentLinkId,
            PaymentLinkId = subscriptionPaymentLinkId,
            Amount = normalizedAmount,
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
}
