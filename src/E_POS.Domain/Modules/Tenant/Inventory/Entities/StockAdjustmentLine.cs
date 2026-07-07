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

    protected StockAdjustmentLine() { }

    public static StockAdjustmentLine Create(
        Guid id,
        Guid tenantId,
        Guid stockAdjustmentId,
        int lineNumber,
        Guid productId,
        Guid? productVariantId,
        Guid? productBatchId,
        decimal quantityBefore,
        decimal quantityChange,
        decimal? unitCost,
        string? lineNote,
        DateTimeOffset now)
    {
        return new StockAdjustmentLine
        {
            Id = id,
            TenantId = tenantId,
            StockAdjustmentId = stockAdjustmentId,
            LineNumber = lineNumber,
            ProductId = productId,
            ProductVariantId = productVariantId,
            ProductBatchId = productBatchId,
            QuantityBefore = quantityBefore,
            QuantityChange = quantityChange,
            QuantityAfter = quantityBefore + quantityChange,
            UnitCost = unitCost,
            LineNote = lineNote?.Trim(),
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}
