using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Inventory.Entities;

public class StockAdjustment : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public string AdjustmentNumber { get; protected set; } = string.Empty;
    public string AdjustmentStatus { get; protected set; } = string.Empty;
}
