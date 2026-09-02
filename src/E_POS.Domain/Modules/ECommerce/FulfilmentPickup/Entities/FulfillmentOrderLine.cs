using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.ECommerce.FulfilmentPickup.Entities;

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

    protected FulfillmentOrderLine() { }

    public void Pick(decimal quantity, Guid tenantUserId, DateTimeOffset now)
    {
        if (quantity <= 0)
            throw new InvalidOperationException("FULFILLMENT_PICK_QUANTITY_INVALID");

        var remaining = RequestedQuantity - CancelledQuantity - PickedQuantity;
        if (remaining <= 0 || quantity > remaining)
            throw new InvalidOperationException("FULFILLMENT_PICK_QUANTITY_EXCEEDED");

        PickedQuantity += quantity;
        PickedByTenantUserId = tenantUserId;
        LineStatus = PickedQuantity + CancelledQuantity >= RequestedQuantity
            ? "PICKED"
            : "PARTIALLY_PICKED";
        UpdatedAt = now;
    }
}

