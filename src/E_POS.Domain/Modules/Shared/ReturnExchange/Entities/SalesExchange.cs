using E_POS.Domain.Common.Entities;
using E_POS.Domain.Modules.Shared.ReturnExchange.Constants;

namespace E_POS.Domain.Modules.Shared.ReturnExchange.Entities;

public class SalesExchange : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid? DocumentNumberSequenceId { get; protected set; }
    public Guid? SalesReturnId { get; protected set; }
    public Guid? ReplacementSalesOrderId { get; protected set; }
    public string ExchangeNumber { get; protected set; } = string.Empty;
    public string ExchangeStatus { get; protected set; } = string.Empty;
    public string ExchangeMode { get; protected set; } = string.Empty;
    public decimal PriceDifferenceAmount { get; protected set; }
    public decimal AdditionalPaymentAmount { get; protected set; }
    public decimal RefundBackAmount { get; protected set; }
    public DateTimeOffset? CompletedAt { get; protected set; }
    public DateTimeOffset? CancelledAt { get; protected set; }
    public string? Notes { get; protected set; }
    public string? IdempotencyKey { get; protected set; }
    public Guid? CreatedByTenantUserId { get; protected set; }
    public Guid? UpdatedByTenantUserId { get; protected set; }

    public static SalesExchange CreateCompleted(
        Guid id,
        Guid tenantId,
        Guid salesReturnId,
        Guid replacementSalesOrderId,
        string exchangeNumber,
        string exchangeMode,
        decimal priceDifferenceAmount,
        decimal additionalPaymentAmount,
        decimal refundBackAmount,
        string? notes,
        string idempotencyKey,
        Guid tenantUserId,
        DateTimeOffset now) => new()
        {
            Id = id,
            TenantId = tenantId,
            SalesReturnId = salesReturnId,
            ReplacementSalesOrderId = replacementSalesOrderId,
            ExchangeNumber = exchangeNumber.Trim().ToUpperInvariant(),
            ExchangeStatus = SalesExchangeConstants.Status.Completed,
            ExchangeMode = exchangeMode.Trim().ToUpperInvariant(),
            PriceDifferenceAmount = priceDifferenceAmount,
            AdditionalPaymentAmount = additionalPaymentAmount,
            RefundBackAmount = refundBackAmount,
            CompletedAt = now,
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            IdempotencyKey = idempotencyKey.Trim(),
            CreatedByTenantUserId = tenantUserId,
            UpdatedByTenantUserId = tenantUserId,
            CreatedAt = now,
            UpdatedAt = now,
        };
}

