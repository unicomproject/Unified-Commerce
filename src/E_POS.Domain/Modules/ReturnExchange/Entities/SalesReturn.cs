using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.ReturnExchange.Entities;

public class SalesReturn : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid? DocumentNumberSequenceId { get; protected set; }
    public Guid SalesOrderId { get; protected set; }
    public Guid? CustomerId { get; protected set; }
    public Guid? OutletId { get; protected set; }
    public Guid? ReturnReasonId { get; protected set; }
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
    public decimal TotalRefundAmount { get; protected set; }
    public decimal TotalExchangeAmount { get; protected set; }
    public string? Notes { get; protected set; }
    public Guid? CreatedByTenantUserId { get; protected set; }
    public Guid? UpdatedByTenantUserId { get; protected set; }
}
