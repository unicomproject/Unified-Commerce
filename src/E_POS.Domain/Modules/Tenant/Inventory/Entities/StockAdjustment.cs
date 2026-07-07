using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.Inventory.Entities;

public class StockAdjustment : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public string AdjustmentNumber { get; protected set; } = string.Empty;
    public string AdjustmentStatus { get; protected set; } = string.Empty;
    public Guid? CreatedByTenantUserId { get; protected set; }
    public Guid? UpdatedByTenantUserId { get; protected set; }

    protected StockAdjustment() { }

    public static StockAdjustment Create(
        Guid id,
        Guid tenantId,
        string adjustmentNumber,
        string adjustmentStatus,
        Guid? createdByTenantUserId,
        DateTimeOffset now)
    {
        return new StockAdjustment
        {
            Id = id,
            TenantId = tenantId,
            AdjustmentNumber = adjustmentNumber.Trim(),
            AdjustmentStatus = adjustmentStatus.Trim(),
            CreatedByTenantUserId = createdByTenantUserId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void UpdateStatus(string status, Guid? updatedByTenantUserId, DateTimeOffset now)
    {
        AdjustmentStatus = status.Trim();
        UpdatedByTenantUserId = updatedByTenantUserId;
        UpdatedAt = now;
    }
}

