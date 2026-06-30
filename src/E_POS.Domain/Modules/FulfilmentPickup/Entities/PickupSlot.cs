using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.FulfilmentPickup.Entities;

public class PickupSlot : AuditableEntity
{
    public int Capacity { get; protected set; }
    public Guid FulfillmentMethodOutletId { get; protected set; }
    public int ReservedCount { get; protected set; }
    public DateOnly SlotDate { get; protected set; }
    public TimeOnly WindowEnd { get; protected set; }
    public TimeOnly WindowStart { get; protected set; }
}
