using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.Payment.Entities;

public class SalesPayment : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid? DocumentNumberSequenceId { get; protected set; }
    public Guid SalesOrderId { get; protected set; }
    public string PaymentNumber { get; protected set; } = string.Empty;
    public Guid PaymentMethodId { get; protected set; }
    public Guid? TillId { get; protected set; }
    public Guid? TillSessionId { get; protected set; }
    public string PaymentStatus { get; protected set; } = string.Empty;
    public string CurrencyCode { get; protected set; } = string.Empty;
    public decimal? RequestedAmount { get; protected set; }
    public decimal? TenderedAmount { get; protected set; }
    public decimal PaidAmount { get; protected set; }
    public decimal ChangeAmount { get; protected set; }
    public decimal RefundedAmount { get; protected set; }
    public string? ExternalReference { get; protected set; }
    public string? IdempotencyKey { get; protected set; }
    public string? PaymentNote { get; protected set; }
    public DateTimeOffset InitiatedAt { get; protected set; }
    public DateTimeOffset? PaidAt { get; protected set; }
    public DateTimeOffset? CancelledAt { get; protected set; }
    public string? CancellationReason { get; protected set; }
    public Guid? CreatedByTenantUserId { get; protected set; }
    public Guid? UpdatedByTenantUserId { get; protected set; }

    public static SalesPayment CreateCompletedPosPayment(
        Guid id,
        Guid tenantId,
        Guid salesOrderId,
        string paymentNumber,
        Guid paymentMethodId,
        Guid tillId,
        Guid tillSessionId,
        string currencyCode,
        decimal requestedAmount,
        decimal? tenderedAmount,
        decimal paidAmount,
        decimal changeAmount,
        Guid? createdByTenantUserId,
        DateTimeOffset now)
    {
        return new SalesPayment
        {
            Id = id,
            TenantId = tenantId,
            SalesOrderId = salesOrderId,
            PaymentNumber = paymentNumber.Trim(),
            PaymentMethodId = paymentMethodId,
            TillId = tillId,
            TillSessionId = tillSessionId,
            PaymentStatus = "PAID",
            CurrencyCode = currencyCode.Trim().ToUpperInvariant(),
            RequestedAmount = requestedAmount,
            TenderedAmount = tenderedAmount,
            PaidAmount = paidAmount,
            ChangeAmount = changeAmount,
            RefundedAmount = 0,
            InitiatedAt = now,
            PaidAt = now,
            CreatedByTenantUserId = createdByTenantUserId,
            UpdatedByTenantUserId = createdByTenantUserId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}

