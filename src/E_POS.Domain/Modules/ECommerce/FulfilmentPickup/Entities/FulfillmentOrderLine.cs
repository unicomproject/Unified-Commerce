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

    public static FulfillmentOrderLine Create(
        Guid id,
        Guid tenantId,
        Guid fulfillmentOrderId,
        Guid salesOrderLineId,
        decimal requestedQuantity,
        decimal cancelledQuantity,
        DateTimeOffset now)
    {
        if (requestedQuantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(requestedQuantity));

        return new FulfillmentOrderLine
        {
            Id = id,
            TenantId = tenantId,
            FulfillmentOrderId = fulfillmentOrderId,
            SalesOrderLineId = salesOrderLineId,
            RequestedQuantity = requestedQuantity,
            PickedQuantity = 0,
            PackedQuantity = 0,
            FulfilledQuantity = 0,
            CancelledQuantity = cancelledQuantity,
            LineStatus = "PENDING",
            CreatedAt = now,
            UpdatedAt = now
        };
    }

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

    public void Pack(Guid tenantUserId, DateTimeOffset now)
    {
        var effectiveRequired = RequestedQuantity - CancelledQuantity;
        if (effectiveRequired < 0)
            throw new InvalidOperationException("FULFILLMENT_PACK_QUANTITY_INVALID");

        if (PickedQuantity < effectiveRequired)
            throw new InvalidOperationException("FULFILLMENT_PACK_NOT_READY");

        if (PackedQuantity > 0 && PackedQuantity >= PickedQuantity)
            throw new InvalidOperationException("FULFILLMENT_ALREADY_PACKED");

        PackedQuantity = PickedQuantity;
        if (PackedQuantity > effectiveRequired)
            throw new InvalidOperationException("FULFILLMENT_PACK_QUANTITY_EXCEEDED");

        PackedByTenantUserId = tenantUserId;
        LineStatus = "PACKED";
        UpdatedAt = now;
    }
}

