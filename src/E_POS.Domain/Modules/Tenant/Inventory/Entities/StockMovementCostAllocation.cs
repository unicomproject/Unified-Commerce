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
}
