using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Inventory.Entities;

public class InventoryChannelAllocation : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid InventoryLocationId { get; protected set; }
    public Guid ProductId { get; protected set; }
    public Guid? ProductVariantId { get; protected set; }
    public Guid SalesChannelId { get; protected set; }
    public decimal AllocationLimitQuantity { get; protected set; }
    public decimal SafetyStockQuantity { get; protected set; }
    public bool IsEnabled { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
}