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
    public Guid? InventoryReservationLineId { get; protected set; }

    protected FulfillmentOrderLine() { }

    public static FulfillmentOrderLine CreateForPicking(
        Guid id,
        Guid tenantId,
        Guid fulfillmentOrderId,
        Guid salesOrderLineId,
        decimal requestedQuantity,
        DateTimeOffset now,
        Guid? inventoryReservationLineId = null) => new()
    {
        Id = id,
        TenantId = tenantId,
        FulfillmentOrderId = fulfillmentOrderId,
        SalesOrderLineId = salesOrderLineId,
        RequestedQuantity = requestedQuantity,
        PickedQuantity = 0m,
        PackedQuantity = 0m,
        FulfilledQuantity = 0m,
        CancelledQuantity = 0m,
        LineStatus = "PICKING",
        InventoryReservationLineId = inventoryReservationLineId,
        CreatedAt = now,
        UpdatedAt = now
    };

    public void Pick(decimal quantity, Guid userId, DateTimeOffset now)
    {
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity));
        if (PickedQuantity + quantity > RequestedQuantity)
            throw new InvalidOperationException("Picked quantity cannot exceed the requested quantity.");
        PickedQuantity += quantity;
        PickedByTenantUserId = userId;
        LineStatus = PickedQuantity == RequestedQuantity ? "PICKED" : "PICKING";
        UpdatedAt = now;
    }

    public void Pack(Guid userId, DateTimeOffset now)
    {
        if (PickedQuantity != RequestedQuantity)
            throw new InvalidOperationException("Only fully picked lines can be packed.");
        PackedQuantity = PickedQuantity;
        PackedByTenantUserId = userId;
        LineStatus = "PACKED";
        UpdatedAt = now;
    }
}

