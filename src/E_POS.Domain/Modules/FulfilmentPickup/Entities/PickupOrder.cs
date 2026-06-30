using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.FulfilmentPickup.Entities;

public class PickupOrder : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
    public Guid FulfillmentOrderId { get; protected set; }
    public string PickupNumber { get; protected set; } = string.Empty;
    public Guid PickupSlotReservationId { get; protected set; }
}
