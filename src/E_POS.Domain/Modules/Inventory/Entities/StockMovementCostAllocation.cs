using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Inventory.Entities;

public class StockMovementCostAllocation : AuditableEntity
{
    public decimal AllocatedCostAmount { get; protected set; }
    public decimal AllocatedQuantity { get; protected set; }
    public Guid InventoryCostLayerId { get; protected set; }
    public Guid StockMovementId { get; protected set; }
}
