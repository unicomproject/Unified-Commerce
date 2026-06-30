using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.FulfilmentPickup.Entities;

public class PickupSlotReservation : AuditableEntity
{
    public string Status { get; protected set; } = string.Empty;
    public Guid CheckoutSessionId { get; protected set; }
    public Guid PickupSlotId { get; protected set; }
    public int ReservedCapacity { get; protected set; }
    public Guid SalesOrderId { get; protected set; }
}
