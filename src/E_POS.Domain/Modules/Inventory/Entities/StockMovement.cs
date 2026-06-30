using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Inventory.Entities;

public class StockMovement : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public string MovementNumber { get; protected set; } = string.Empty;
    public decimal MovementQuantity { get; protected set; }
}
