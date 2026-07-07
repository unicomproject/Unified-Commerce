using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.Inventory.Entities;

public class InventoryLocation : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid OutletId { get; protected set; }
    public Guid? ParentInventoryLocationId { get; protected set; }
    public string LocationCode { get; protected set; } = string.Empty;
    public string LocationName { get; protected set; } = string.Empty;
    public string LocationType { get; protected set; } = string.Empty;
    public bool IsSellableLocation { get; protected set; }
    public bool IsReturnLocation { get; protected set; }
    public bool IsReceivingLocation { get; protected set; }
    public bool IsQuarantineLocation { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
}
