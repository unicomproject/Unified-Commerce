using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.Inventory.Entities;

public class InventoryReservation : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public string ReservationNumber { get; protected set; } = string.Empty;
    public string ReservationSource { get; protected set; } = string.Empty;
    public Guid? SourceReferenceId { get; protected set; }
    public string? SourceReferenceNumber { get; protected set; }
    public Guid? SalesChannelId { get; protected set; }
    public Guid? FulfillmentOutletId { get; protected set; }
    public Guid? CustomerId { get; protected set; }
    public string ReservationStatus { get; protected set; } = string.Empty;
    public DateTimeOffset ReservedAt { get; protected set; }
    public DateTimeOffset? ExpiresAt { get; protected set; }
    public DateTimeOffset? ReleasedAt { get; protected set; }
    public string? ReleaseReason { get; protected set; }
}
