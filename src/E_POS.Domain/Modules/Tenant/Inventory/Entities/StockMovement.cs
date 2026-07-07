using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.Inventory.Entities;

public class StockMovement : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public string MovementNumber { get; protected set; } = string.Empty;
    public Guid InventoryBalanceId { get; protected set; }
    public string MovementType { get; protected set; } = string.Empty;
    public decimal QuantityBefore { get; protected set; }
    public decimal QuantityChange { get; protected set; }
    public decimal QuantityAfter { get; protected set; }
    public decimal? UnitCost { get; protected set; }
    public decimal? TotalCost { get; protected set; }
    public string? IdempotencyKey { get; protected set; }
    public string? MovementNote { get; protected set; }
    public DateTimeOffset OccurredAt { get; protected set; }
}
