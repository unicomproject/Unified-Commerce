using E_POS.Domain.Common.Entities;

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
}
