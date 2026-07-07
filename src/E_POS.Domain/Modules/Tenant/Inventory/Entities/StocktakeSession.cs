using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.Inventory.Entities;

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
    public Guid? CreatedByTenantUserId { get; protected set; }
    public Guid? UpdatedByTenantUserId { get; protected set; }

    protected StocktakeSession() { }

    public static StocktakeSession Create(
        Guid id,
        Guid tenantId,
        string stocktakeNumber,
        Guid inventoryLocationId,
        string stocktakeType,
        bool isBlindCount,
        DateTimeOffset snapshotAt,
        string? notes,
        Guid? createdByTenantUserId,
        DateTimeOffset now)
    {
        return new StocktakeSession
        {
            Id = id,
            TenantId = tenantId,
            StocktakeNumber = stocktakeNumber.Trim(),
            InventoryLocationId = inventoryLocationId,
            StocktakeType = stocktakeType.Trim(),
            StocktakeStatus = "DRAFT",
            IsBlindCount = isBlindCount,
            SnapshotAt = snapshotAt,
            Notes = notes?.Trim(),
            CreatedByTenantUserId = createdByTenantUserId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void Start(Guid startedByTenantUserId, DateTimeOffset now)
    {
        StocktakeStatus = "IN_PROGRESS";
        StartedByTenantUserId = startedByTenantUserId;
        StartedAt = now;
        UpdatedByTenantUserId = startedByTenantUserId;
        UpdatedAt = now;
    }

    public void Complete(Guid completedByTenantUserId, DateTimeOffset now)
    {
        StocktakeStatus = "COMPLETED";
        CompletedByTenantUserId = completedByTenantUserId;
        CompletedAt = now;
        UpdatedByTenantUserId = completedByTenantUserId;
        UpdatedAt = now;
    }

    public void Post(Guid postedByTenantUserId, Guid generatedStockAdjustmentId, DateTimeOffset now)
    {
        StocktakeStatus = "POSTED";
        PostedByTenantUserId = postedByTenantUserId;
        PostedAt = now;
        GeneratedStockAdjustmentId = generatedStockAdjustmentId;
        UpdatedByTenantUserId = postedByTenantUserId;
        UpdatedAt = now;
    }
}
