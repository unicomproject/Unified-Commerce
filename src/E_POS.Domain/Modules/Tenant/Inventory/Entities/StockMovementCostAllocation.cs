using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.Inventory.Entities;

public class StockMovementCostAllocation : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid StockMovementId { get; protected set; }
    public Guid InventoryCostLayerId { get; protected set; }
    public decimal AllocatedQuantity { get; protected set; }
    public decimal UnitCost { get; protected set; }
    public decimal TotalCost { get; protected set; }

    protected StockMovementCostAllocation() { }

    public static StockMovementCostAllocation Create(
        Guid id,
        Guid tenantId,
        Guid stockMovementId,
        Guid inventoryCostLayerId,
        decimal allocatedQuantity,
        decimal unitCost,
        DateTimeOffset now)
    {
        return new StockMovementCostAllocation
        {
            Id = id,
            TenantId = tenantId,
            StockMovementId = stockMovementId,
            InventoryCostLayerId = inventoryCostLayerId,
            AllocatedQuantity = allocatedQuantity,
            UnitCost = unitCost,
            TotalCost = allocatedQuantity * unitCost,
            CreatedAt = now,
            UpdatedAt = now // Required by AuditableEntity base class but not stored
        };
    }
}
