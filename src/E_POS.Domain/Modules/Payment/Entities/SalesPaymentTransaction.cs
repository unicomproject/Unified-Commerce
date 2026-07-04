using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Payment.Entities;

public class SalesPaymentTransaction : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid SalesPaymentId { get; protected set; }
    public Guid? ParentTransactionId { get; protected set; }
    public string TransactionType { get; protected set; } = string.Empty;
    public string TransactionStatus { get; protected set; } = string.Empty;
    public decimal Amount { get; protected set; }
    public string CurrencyCode { get; protected set; } = string.Empty;
    public string? ExternalTransactionReference { get; protected set; }
    public string? ProviderName { get; protected set; }
    public string? ProviderResponseJson { get; protected set; }
    public string? IdempotencyKey { get; protected set; }
    public Guid? ProcessedByTenantUserId { get; protected set; }
    public DateTimeOffset ProcessedAt { get; protected set; }
}
