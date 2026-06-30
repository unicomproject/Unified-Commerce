using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Inventory.Entities;

public class StockAdjustmentReason : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public string ReasonCode { get; protected set; } = string.Empty;
    public string Name { get; protected set; } = string.Empty;
    public string? Description { get; protected set; }
}
