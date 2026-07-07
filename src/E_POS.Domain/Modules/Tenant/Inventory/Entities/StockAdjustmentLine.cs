using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.Inventory.Entities;

public class StockAdjustmentLine : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid StockAdjustmentId { get; protected set; }
    public int LineNumber { get; protected set; }
    public Guid ProductId { get; protected set; }
    public Guid? ProductVariantId { get; protected set; }
    public Guid? ProductBatchId { get; protected set; }
    public decimal QuantityBefore { get; protected set; }
    public decimal QuantityChange { get; protected set; }
    public decimal QuantityAfter { get; protected set; }
    public decimal? UnitCost { get; protected set; }
    public string? LineNote { get; protected set; }
}
