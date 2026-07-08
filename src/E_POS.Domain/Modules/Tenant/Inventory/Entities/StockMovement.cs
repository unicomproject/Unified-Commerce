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
    public Guid? CreatedByTenantUserId { get; protected set; }

    protected StockMovement() { }

    public static StockMovement Create(
        Guid id,
        Guid tenantId,
        string movementNumber,
        Guid inventoryBalanceId,
        string movementType,
        decimal quantityBefore,
        decimal quantityChange,
        decimal? unitCost,
        decimal? totalCost,
        string? idempotencyKey,
        string? movementNote,
        DateTimeOffset occurredAt,
        Guid? createdByTenantUserId,
        DateTimeOffset now)
    {
        return new StockMovement
        {
            Id = id,
            TenantId = tenantId,
            MovementNumber = movementNumber.Trim(),
            InventoryBalanceId = inventoryBalanceId,
            MovementType = movementType.Trim(),
            QuantityBefore = quantityBefore,
            QuantityChange = quantityChange,
            QuantityAfter = quantityBefore + quantityChange,
            UnitCost = unitCost,
            TotalCost = totalCost,
            IdempotencyKey = idempotencyKey?.Trim(),
            MovementNote = movementNote?.Trim(),
            OccurredAt = occurredAt,
            CreatedByTenantUserId = createdByTenantUserId,
            CreatedAt = now,
            UpdatedAt = now // AuditableEntity requires UpdatedAt, even if not stored
        };
    }
}
