using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Inventory.Entities;

public class StockAdjustmentLine : AuditableEntity
{
    public decimal AdjustmentQuantity { get; protected set; }
    public string LineNumber { get; protected set; } = string.Empty;
    public Guid ProductId { get; protected set; }
    public Guid StockAdjustmentId { get; protected set; }
}
