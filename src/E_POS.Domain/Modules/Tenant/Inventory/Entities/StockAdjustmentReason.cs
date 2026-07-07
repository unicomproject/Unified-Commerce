using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.Inventory.Entities;

public class StockAdjustmentReason : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public string ReasonCode { get; protected set; } = string.Empty;
    public string ReasonName { get; protected set; } = string.Empty;
    public string Direction { get; protected set; } = string.Empty;
    public bool RequiresManagerApproval { get; protected set; }
    public bool IsSystemReason { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
    public Guid? CreatedByTenantUserId { get; protected set; }
    public Guid? UpdatedByTenantUserId { get; protected set; }

    protected StockAdjustmentReason() { }

    public static StockAdjustmentReason Create(
        Guid id,
        Guid tenantId,
        string reasonCode,
        string reasonName,
        string direction,
        bool requiresManagerApproval,
        bool isSystemReason,
        string status,
        Guid? createdByTenantUserId,
        DateTimeOffset now)
    {
        return new StockAdjustmentReason
        {
            Id = id,
            TenantId = tenantId,
            ReasonCode = reasonCode.Trim(),
            ReasonName = reasonName.Trim(),
            Direction = direction.Trim(),
            RequiresManagerApproval = requiresManagerApproval,
            IsSystemReason = isSystemReason,
            Status = status.Trim(),
            CreatedByTenantUserId = createdByTenantUserId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void Update(
        string reasonName,
        bool requiresManagerApproval,
        string status,
        Guid? updatedByTenantUserId,
        DateTimeOffset now)
    {
        ReasonName = reasonName.Trim();
        RequiresManagerApproval = requiresManagerApproval;
        Status = status.Trim();
        UpdatedByTenantUserId = updatedByTenantUserId;
        UpdatedAt = now;
    }
}
