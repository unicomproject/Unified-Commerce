using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.ECommerce.FulfilmentPickup.Entities;

public class FulfillmentOrder : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid? DocumentNumberSequenceId { get; protected set; }
    public Guid SalesOrderId { get; protected set; }
    public string FulfillmentNumber { get; protected set; } = string.Empty;
    public Guid FulfillmentMethodOutletId { get; protected set; }
    public Guid? SourceInventoryLocationId { get; protected set; }
    public string FulfillmentStatus { get; protected set; } = string.Empty;
    public DateOnly? RequestedFulfillmentDate { get; protected set; }
    public DateTimeOffset? ScheduledAt { get; protected set; }
    public DateTimeOffset? PickedAt { get; protected set; }
    public DateTimeOffset? PackedAt { get; protected set; }
    public DateTimeOffset? ReadyAt { get; protected set; }
    public DateTimeOffset? FulfilledAt { get; protected set; }
    public DateTimeOffset? CancelledAt { get; protected set; }
    public string? CancellationReason { get; protected set; }
    public Guid? AssignedToTenantUserId { get; protected set; }
    public string? FulfillmentNote { get; protected set; }
    public Guid? CreatedByTenantUserId { get; protected set; }
    public Guid? UpdatedByTenantUserId { get; protected set; }
    public long RowVersion { get; protected set; }

    protected FulfillmentOrder() { }

    public static FulfillmentOrder StartForClickAndCollect(
        Guid id,
        Guid tenantId,
        Guid salesOrderId,
        string fulfillmentNumber,
        Guid fulfillmentMethodOutletId,
        Guid sourceInventoryLocationId,
        Guid assignedToTenantUserId,
        DateTimeOffset now) => new()
    {
        Id = id,
        TenantId = tenantId,
        SalesOrderId = salesOrderId,
        FulfillmentNumber = fulfillmentNumber.Trim(),
        FulfillmentMethodOutletId = fulfillmentMethodOutletId,
        SourceInventoryLocationId = sourceInventoryLocationId,
        FulfillmentStatus = "PICKING",
        AssignedToTenantUserId = assignedToTenantUserId,
        CreatedByTenantUserId = assignedToTenantUserId,
        UpdatedByTenantUserId = assignedToTenantUserId,
        CreatedAt = now,
        UpdatedAt = now
    };

    public void MarkPicked(Guid userId, DateTimeOffset now)
    {
        if (FulfillmentStatus == "PICKED") return;
        if (FulfillmentStatus is not ("PICKING" or "PICKED"))
            throw new InvalidOperationException("Fulfilment is not in a pickable state.");
        FulfillmentStatus = "PICKED";
        PickedAt ??= now;
        UpdatedByTenantUserId = userId;
        UpdatedAt = now;
        RowVersion++;
    }

    public void MarkPacked(Guid userId, DateTimeOffset now)
    {
        if (FulfillmentStatus == "PACKED") return;
        if (FulfillmentStatus is not ("PICKED" or "PACKED"))
            throw new InvalidOperationException("All required items must be picked before packing.");
        FulfillmentStatus = "PACKED";
        PackedAt ??= now;
        UpdatedByTenantUserId = userId;
        UpdatedAt = now;
        RowVersion++;
    }

    public void MarkReady(Guid userId, DateTimeOffset now)
    {
        if (FulfillmentStatus == "READY") return;
        if (FulfillmentStatus is not ("PACKED" or "READY"))
            throw new InvalidOperationException("The order must be packed before it can be marked ready.");
        FulfillmentStatus = "READY";
        ReadyAt ??= now;
        UpdatedByTenantUserId = userId;
        UpdatedAt = now;
        RowVersion++;
    }
}

