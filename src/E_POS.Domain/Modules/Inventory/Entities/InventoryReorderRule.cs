using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Inventory.Entities;

public class InventoryReorderRule : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid InventoryLocationId { get; protected set; }
    public Guid ProductId { get; protected set; }
    public Guid? ProductVariantId { get; protected set; }
    public string ReorderMethod { get; protected set; } = string.Empty;
    public decimal ReorderPointQuantity { get; protected set; }
    public decimal? ReorderQuantity { get; protected set; }
    public decimal? MinStockQuantity { get; protected set; }
    public decimal? MaxStockQuantity { get; protected set; }
    public decimal SafetyStockQuantity { get; protected set; }
    public int? LeadTimeDays { get; protected set; }
    public Guid? SupplierProductId { get; protected set; }
    public bool IsAutoReorder { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
}