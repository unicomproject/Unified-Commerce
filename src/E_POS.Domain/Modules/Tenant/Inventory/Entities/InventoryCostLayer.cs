using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.Inventory.Entities;

public class InventoryCostLayer : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid InventoryBalanceId { get; protected set; }
    public Guid SourceStockMovementId { get; protected set; }
    public decimal ReceivedQuantity { get; protected set; }
    public decimal RemainingQuantity { get; protected set; }
    public decimal UnitCost { get; protected set; }
    public decimal TotalCost { get; protected set; }
    public DateTimeOffset ReceivedAt { get; protected set; }
    public string Status { get; protected set; } = string.Empty;

    protected InventoryCostLayer() { }

    public static InventoryCostLayer Create(
        Guid id,
        Guid tenantId,
        Guid inventoryBalanceId,
        Guid sourceStockMovementId,
        decimal receivedQuantity,
        decimal unitCost,
        DateTimeOffset receivedAt,
        string status,
        DateTimeOffset now)
    {
        return new InventoryCostLayer
        {
            Id = id,
            TenantId = tenantId,
            InventoryBalanceId = inventoryBalanceId,
            SourceStockMovementId = sourceStockMovementId,
            ReceivedQuantity = receivedQuantity,
            RemainingQuantity = receivedQuantity,
            UnitCost = unitCost,
            TotalCost = receivedQuantity * unitCost,
            ReceivedAt = receivedAt,
            Status = status.Trim(),
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void ConsumeQuantity(decimal quantityToConsume, DateTimeOffset now)
    {
        RemainingQuantity -= quantityToConsume;
        UpdatedAt = now;
    }

    public void UpdateStatus(string status, DateTimeOffset now)
    {
        Status = status.Trim();
        UpdatedAt = now;
    }
}
