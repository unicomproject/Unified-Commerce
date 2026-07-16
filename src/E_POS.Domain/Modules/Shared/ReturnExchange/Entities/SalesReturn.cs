using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Shared.ReturnExchange.Entities;

public class SalesReturn : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid? DocumentNumberSequenceId { get; protected set; }
    public Guid SalesOrderId { get; protected set; }
    public Guid? CustomerId { get; protected set; }
    public Guid? OutletId { get; protected set; }
    public string? ProcessingOutletCodeSnapshot { get; protected set; }
    public string? ProcessingOutletNameSnapshot { get; protected set; }
    public Guid? ReturnReasonId { get; protected set; }
    public string? ReturnReasonCodeSnapshot { get; protected set; }
    public string? ReturnReasonNameSnapshot { get; protected set; }
    public string ReturnNumber { get; protected set; } = string.Empty;
    public string ReturnChannel { get; protected set; } = string.Empty;
    public string ReturnStatus { get; protected set; } = string.Empty;
    public DateTimeOffset RequestedAt { get; protected set; }
    public DateTimeOffset? ApprovedAt { get; protected set; }
    public DateTimeOffset? ReceivedAt { get; protected set; }
    public DateTimeOffset? CompletedAt { get; protected set; }
    public DateTimeOffset? CancelledAt { get; protected set; }
    public decimal TotalRequestedQty { get; protected set; }
    public decimal TotalReceivedQty { get; protected set; }
    public decimal? TotalApprovedQty { get; protected set; }
    public decimal TotalRefundAmount { get; protected set; }
    public decimal TotalExchangeAmount { get; protected set; }
    public string? Notes { get; protected set; }
    public Guid? CreatedByTenantUserId { get; protected set; }
    public Guid? UpdatedByTenantUserId { get; protected set; }

    public static SalesReturn CreateCompleted(
        Guid id,
        Guid tenantId,
        Guid salesOrderId,
        Guid? customerId,
        Guid outletId,
        Guid returnReasonId,
        string returnNumber,
        decimal totalQuantity,
        decimal totalRefundAmount,
        string? notes,
        Guid tenantUserId,
        DateTimeOffset now) =>
        CreateCompleted(
            id,
            tenantId,
            salesOrderId,
            customerId,
            outletId,
            null,
            null,
            returnReasonId,
            null,
            null,
            returnNumber,
            totalQuantity,
            totalRefundAmount,
            notes,
            tenantUserId,
            now);

    public static SalesReturn CreateCompleted(
        Guid id,
        Guid tenantId,
        Guid salesOrderId,
        Guid? customerId,
        Guid outletId,
        string? processingOutletCodeSnapshot,
        string? processingOutletNameSnapshot,
        Guid returnReasonId,
        string? returnReasonCodeSnapshot,
        string? returnReasonNameSnapshot,
        string returnNumber,
        decimal totalQuantity,
        decimal totalRefundAmount,
        string? notes,
        Guid tenantUserId,
        DateTimeOffset now) => new()
        {
            Id = id,
            TenantId = tenantId,
            SalesOrderId = salesOrderId,
            CustomerId = customerId,
            OutletId = outletId,
            ProcessingOutletCodeSnapshot = processingOutletCodeSnapshot?.Trim(),
            ProcessingOutletNameSnapshot = processingOutletNameSnapshot?.Trim(),
            ReturnReasonId = returnReasonId,
            ReturnReasonCodeSnapshot = returnReasonCodeSnapshot?.Trim(),
            ReturnReasonNameSnapshot = returnReasonNameSnapshot?.Trim(),
            ReturnNumber = returnNumber.Trim().ToUpperInvariant(),
            ReturnChannel = "POS",
            ReturnStatus = "COMPLETED",
            RequestedAt = now,
            ApprovedAt = now,
            ReceivedAt = now,
            CompletedAt = now,
            TotalRequestedQty = totalQuantity,
            TotalReceivedQty = totalQuantity,
            TotalApprovedQty = totalQuantity,
            TotalRefundAmount = totalRefundAmount,
            TotalExchangeAmount = 0,
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            CreatedByTenantUserId = tenantUserId,
            UpdatedByTenantUserId = tenantUserId,
            CreatedAt = now,
            UpdatedAt = now
        };
}

