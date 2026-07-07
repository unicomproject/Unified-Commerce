using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.Inventory.Entities;

public class InventoryCostLayer : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid InventoryBalanceId { get; protected set; }
    public Guid SourceStockMovementId { get; protected set; }
    public decimal ReceivedQuantity { get; protected set; }
    public decimal RemainingQuantity { get; protected set; }
    public decimal UnitCost { get; protected set; }
    public decimal TotalCost { get; protected set; }
    public DateTimeOffset ReceivedAt { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
}
