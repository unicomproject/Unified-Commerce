using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Inventory.Entities;

public class InventoryLocation : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid? OutletId { get; protected set; }
    public string Name { get; protected set; } = string.Empty;
    public string LocationCode { get; protected set; } = string.Empty;
}
