using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Inventory.Entities;

public class InventoryReorderRule : AuditableEntity
{
    public Guid InventoryLocationId { get; protected set; }
    public Guid ProductId { get; protected set; }
    public Guid ProductVariantId { get; protected set; }
    public decimal ReorderPointQuantity { get; protected set; }
}
