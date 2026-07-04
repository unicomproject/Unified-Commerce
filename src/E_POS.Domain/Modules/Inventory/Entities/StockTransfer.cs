using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Inventory.Entities;

public class StockTransfer : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public string TransferNumber { get; protected set; } = string.Empty;
    public Guid SourceLocationId { get; protected set; }
    public Guid DestinationLocationId { get; protected set; }
    public string TransferStatus { get; protected set; } = string.Empty;
    public DateTimeOffset? RequestedAt { get; protected set; }
    public DateTimeOffset? ApprovedAt { get; protected set; }
    public DateTimeOffset? ShippedAt { get; protected set; }
    public DateTimeOffset? ReceivedAt { get; protected set; }
    public DateTimeOffset? CancelledAt { get; protected set; }
    public Guid? ApprovedByTenantUserId { get; protected set; }
    public Guid? ShippedByTenantUserId { get; protected set; }
    public Guid? ReceivedByTenantUserId { get; protected set; }
    public Guid? CancelledByTenantUserId { get; protected set; }
    public string? TransferNote { get; protected set; }
    public string? CancellationReason { get; protected set; }
}