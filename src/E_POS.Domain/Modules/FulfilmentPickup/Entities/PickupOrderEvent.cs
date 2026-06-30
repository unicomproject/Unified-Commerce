using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.FulfilmentPickup.Entities;

public class PickupOrderEvent : AuditableEntity
{
    public Guid PickupOrderId { get; protected set; }
    public int SequenceNumber { get; protected set; }
}
