using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.ECommerce.FulfilmentPickup.Entities;

public class PickupSlotReservation : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid PickupSlotId { get; protected set; }
    public Guid? CheckoutSessionId { get; protected set; }
    public Guid? SalesOrderId { get; protected set; }
    public int ReservedCapacity { get; protected set; }
    public string ReservationStatus { get; protected set; } = string.Empty;
    public DateTimeOffset? ExpiresAt { get; protected set; }
    public DateTimeOffset? ConfirmedAt { get; protected set; }
    public DateTimeOffset? ReleasedAt { get; protected set; }
    public string? ReleaseReason { get; protected set; }
}

