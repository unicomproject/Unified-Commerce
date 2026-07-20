using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.Payment.Entities;

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

    public static SalesPaymentTransaction CreateCompletedCash(
        Guid id, Guid tenantId, Guid salesPaymentId, decimal amount,
        string currencyCode, string idempotencyKey, Guid? processedByTenantUserId,
        DateTimeOffset now) => new()
        {
            Id = id,
            TenantId = tenantId,
            SalesPaymentId = salesPaymentId,
            TransactionType = "CAPTURE",
            TransactionStatus = "SUCCEEDED",
            Amount = amount,
            CurrencyCode = currencyCode.Trim().ToUpperInvariant(),
            ProviderName = "CASH",
            IdempotencyKey = idempotencyKey.Trim(),
            ProcessedByTenantUserId = processedByTenantUserId,
            ProcessedAt = now,
            CreatedAt = now,
            UpdatedAt = now
        };

    /// <summary>
    /// Successful provider/terminal capture. Persists only safe card tip metadata in
    /// <see cref="ProviderResponseJson"/> (brand + last4). Never stores PAN/CVV/PIN/raw payloads.
    /// </summary>
    public static SalesPaymentTransaction CreateCompletedProviderCapture(
        Guid id,
        Guid tenantId,
        Guid salesPaymentId,
        decimal amount,
        string currencyCode,
        string providerName,
        string? externalTransactionReference,
        string? cardBrand,
        string? cardLast4,
        string idempotencyKey,
        Guid? processedByTenantUserId,
        DateTimeOffset now) => new()
        {
            Id = id,
            TenantId = tenantId,
            SalesPaymentId = salesPaymentId,
            TransactionType = "CAPTURE",
            TransactionStatus = "SUCCEEDED",
            Amount = amount,
            CurrencyCode = currencyCode.Trim().ToUpperInvariant(),
            ExternalTransactionReference = string.IsNullOrWhiteSpace(externalTransactionReference)
                ? null
                : externalTransactionReference.Trim(),
            ProviderName = string.IsNullOrWhiteSpace(providerName)
                ? null
                : providerName.Trim(),
            ProviderResponseJson = SafePaymentDisplay.ToSanitizedCardMetadataJson(cardBrand, cardLast4),
            IdempotencyKey = idempotencyKey.Trim(),
            ProcessedByTenantUserId = processedByTenantUserId,
            ProcessedAt = now,
            CreatedAt = now,
            UpdatedAt = now
        };
}

