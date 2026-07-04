using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.FulfilmentPickup.Entities;

public class FulfillmentOrderLine : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid FulfillmentOrderId { get; protected set; }
    public Guid SalesOrderLineId { get; protected set; }
    public Guid? SalesOrderLineComponentId { get; protected set; }
    public decimal RequestedQuantity { get; protected set; }
    public decimal PickedQuantity { get; protected set; }
    public decimal PackedQuantity { get; protected set; }
    public decimal FulfilledQuantity { get; protected set; }
    public decimal CancelledQuantity { get; protected set; }
    public string LineStatus { get; protected set; } = string.Empty;
    public Guid? PickedByTenantUserId { get; protected set; }
    public Guid? PackedByTenantUserId { get; protected set; }
}
