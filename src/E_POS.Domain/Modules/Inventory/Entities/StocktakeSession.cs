using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Inventory.Entities;

public class StocktakeSession : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public string StocktakeNumber { get; protected set; } = string.Empty;
    public Guid InventoryLocationId { get; protected set; }
    public string StocktakeType { get; protected set; } = string.Empty;
    public string StocktakeStatus { get; protected set; } = string.Empty;
    public bool IsBlindCount { get; protected set; }
    public DateTimeOffset SnapshotAt { get; protected set; }
    public DateTimeOffset? StartedAt { get; protected set; }
    public DateTimeOffset? CompletedAt { get; protected set; }
    public DateTimeOffset? PostedAt { get; protected set; }
    public Guid? StartedByTenantUserId { get; protected set; }
    public Guid? CompletedByTenantUserId { get; protected set; }
    public Guid? PostedByTenantUserId { get; protected set; }
    public Guid? GeneratedStockAdjustmentId { get; protected set; }
    public string? Notes { get; protected set; }
}