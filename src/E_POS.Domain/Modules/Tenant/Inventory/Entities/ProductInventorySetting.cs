using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.Inventory.Entities;

public class ProductInventorySetting : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid ProductId { get; protected set; }
    public Guid? ProductVariantId { get; protected set; }
    public Guid InventoryUomId { get; protected set; }
    public bool IsStockTracked { get; protected set; }
    public bool AllowNegativeStock { get; protected set; }
    public bool RequiresBatchTracking { get; protected set; }
    public bool RequiresExpiryTracking { get; protected set; }
    public bool RequiresSerialTracking { get; protected set; }
    public string CostingMethod { get; protected set; } = string.Empty;
    public string Status { get; protected set; } = string.Empty;
}
