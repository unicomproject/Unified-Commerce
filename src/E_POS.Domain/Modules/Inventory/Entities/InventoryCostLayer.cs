using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Inventory.Entities;

public class InventoryCostLayer : AuditableEntity
{
    public Guid? ProductBatchId { get; protected set; }
    public decimal QuantityRemaining { get; protected set; }
    public decimal UnitCost { get; protected set; }
}
