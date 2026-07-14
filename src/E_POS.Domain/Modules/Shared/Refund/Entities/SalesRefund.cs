using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Shared.Refund.Entities;

public class SalesRefund : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid? DocumentNumberSequenceId { get; protected set; }
    public Guid SalesOrderId { get; protected set; }
    public Guid? SalesReturnId { get; protected set; }
    public string RefundNumber { get; protected set; } = string.Empty;
    public string RefundMode { get; protected set; } = string.Empty;
    public string RefundStatus { get; protected set; } = string.Empty;
    public string CurrencyCode { get; protected set; } = string.Empty;
    public decimal RequestedAmount { get; protected set; }
    public decimal ApprovedAmount { get; protected set; }
    public decimal RefundedAmount { get; protected set; }
    public string? RefundReason { get; protected set; }
    public DateTimeOffset RequestedAt { get; protected set; }
    public DateTimeOffset? ApprovedAt { get; protected set; }
    public DateTimeOffset? CompletedAt { get; protected set; }
    public DateTimeOffset? CancelledAt { get; protected set; }
    public string? CancellationReason { get; protected set; }
    public Guid? ApprovedByTenantUserId { get; protected set; }
    public Guid? CreatedByTenantUserId { get; protected set; }
    public Guid? UpdatedByTenantUserId { get; protected set; }

    public static SalesRefund CreateCompleted(
        Guid id,
        Guid tenantId,
        Guid salesOrderId,
        Guid salesReturnId,
        string refundNumber,
        string refundMode,
        string currencyCode,
        decimal amount,
        string reason,
        Guid tenantUserId,
        DateTimeOffset now) => new()
        {
            Id = id,
            TenantId = tenantId,
            SalesOrderId = salesOrderId,
            SalesReturnId = salesReturnId,
            RefundNumber = refundNumber.Trim().ToUpperInvariant(),
            RefundMode = refundMode.Trim().ToUpperInvariant(),
            RefundStatus = "COMPLETED",
            CurrencyCode = currencyCode.Trim().ToUpperInvariant(),
            RequestedAmount = amount,
            ApprovedAmount = amount,
            RefundedAmount = amount,
            RefundReason = reason.Trim(),
            RequestedAt = now,
            ApprovedAt = now,
            CompletedAt = now,
            ApprovedByTenantUserId = tenantUserId,
            CreatedByTenantUserId = tenantUserId,
            UpdatedByTenantUserId = tenantUserId,
            CreatedAt = now,
            UpdatedAt = now
        };
}

