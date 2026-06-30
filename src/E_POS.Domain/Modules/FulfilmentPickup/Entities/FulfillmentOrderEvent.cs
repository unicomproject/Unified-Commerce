using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.FulfilmentPickup.Entities;

public class FulfillmentOrderEvent : AuditableEntity
{
    public Guid FulfillmentOrderId { get; protected set; }
    public int SequenceNumber { get; protected set; }
}
