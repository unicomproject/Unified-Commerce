using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.Inventory.Entities;

public class InventoryReservationLine : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid InventoryReservationId { get; protected set; }
    public int LineNumber { get; protected set; }
    public Guid ProductId { get; protected set; }
    public Guid? ProductVariantId { get; protected set; }
    public decimal RequestedQuantity { get; protected set; }
    public decimal ReservedQuantity { get; protected set; }
    public decimal ReleasedQuantity { get; protected set; }
    public decimal FulfilledQuantity { get; protected set; }
    public string LineStatus { get; protected set; } = string.Empty;

    protected InventoryReservationLine() { }

    public static InventoryReservationLine Create(
        Guid id,
        Guid tenantId,
        Guid inventoryReservationId,
        int lineNumber,
        Guid productId,
        Guid? productVariantId,
        decimal requestedQuantity,
        string lineStatus,
        DateTimeOffset now)
    {
        return new InventoryReservationLine
        {
            Id = id,
            TenantId = tenantId,
            InventoryReservationId = inventoryReservationId,
            LineNumber = lineNumber,
            ProductId = productId,
            ProductVariantId = productVariantId,
            RequestedQuantity = requestedQuantity,
            ReservedQuantity = 0,
            ReleasedQuantity = 0,
            FulfilledQuantity = 0,
            LineStatus = lineStatus.Trim(),
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void UpdateQuantities(
        decimal reservedDelta,
        decimal releasedDelta,
        decimal fulfilledDelta,
        DateTimeOffset now)
    {
        ReservedQuantity += reservedDelta;
        ReleasedQuantity += releasedDelta;
        FulfilledQuantity += fulfilledDelta;
        UpdatedAt = now;
    }

    public void UpdateStatus(string status, DateTimeOffset now)
    {
        LineStatus = status.Trim();
        UpdatedAt = now;
    }
}
