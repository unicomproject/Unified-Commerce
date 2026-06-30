using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.FulfilmentPickup.Entities;

public class FulfillmentOrderLine : AuditableEntity
{
    public Guid FulfillmentOrderId { get; protected set; }
    public decimal RequestedQuantity { get; protected set; }
    public Guid? SalesOrderLineId { get; protected set; }
}
