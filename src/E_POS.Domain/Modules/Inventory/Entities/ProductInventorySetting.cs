using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Inventory.Entities;

public class ProductInventorySetting : AuditableEntity
{
    public Guid ProductId { get; protected set; }
    public Guid? ProductVariantId { get; protected set; }
}
