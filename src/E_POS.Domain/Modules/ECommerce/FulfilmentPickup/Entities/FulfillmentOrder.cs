using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.ECommerce.FulfilmentPickup.Entities;

public class FulfillmentOrder : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid? DocumentNumberSequenceId { get; protected set; }
    public Guid SalesOrderId { get; protected set; }
    public string FulfillmentNumber { get; protected set; } = string.Empty;
    public Guid FulfillmentMethodOutletId { get; protected set; }
    public Guid? SourceInventoryLocationId { get; protected set; }
    public string FulfillmentStatus { get; protected set; } = string.Empty;
    public DateOnly? RequestedFulfillmentDate { get; protected set; }
    public DateTimeOffset? ScheduledAt { get; protected set; }
    public DateTimeOffset? PickedAt { get; protected set; }
    public DateTimeOffset? PackedAt { get; protected set; }
    public DateTimeOffset? ReadyAt { get; protected set; }
    public DateTimeOffset? FulfilledAt { get; protected set; }
    public DateTimeOffset? CancelledAt { get; protected set; }
    public string? CancellationReason { get; protected set; }
    public Guid? AssignedToTenantUserId { get; protected set; }
    public string? FulfillmentNote { get; protected set; }
    public Guid? CreatedByTenantUserId { get; protected set; }
    public Guid? UpdatedByTenantUserId { get; protected set; }
}

