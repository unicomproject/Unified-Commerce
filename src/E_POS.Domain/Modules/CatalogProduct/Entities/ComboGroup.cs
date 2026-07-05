using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.CatalogProduct.Entities;

public class ComboGroup : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid ComboDefinitionId { get; protected set; }
    public string GroupCode { get; protected set; } = string.Empty;
    public string GroupName { get; protected set; } = string.Empty;
    public int MinSelect { get; protected set; }
    public int MaxSelect { get; protected set; }
    public int SortOrder { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
    public Guid? CreatedByTenantUserId { get; protected set; }
    public Guid? UpdatedByTenantUserId { get; protected set; }

    public static ComboGroup Create(
        Guid id,
        Guid tenantId,
        Guid comboDefinitionId,
        string groupCode,
        string groupName,
        int minSelect,
        int maxSelect,
        int sortOrder,
        string status,
        Guid? createdByTenantUserId,
        DateTimeOffset now)
    {
        return new ComboGroup
        {
            Id = id,
            TenantId = tenantId,
            ComboDefinitionId = comboDefinitionId,
            GroupCode = groupCode.Trim(),
            GroupName = groupName.Trim(),
            MinSelect = minSelect,
            MaxSelect = maxSelect,
            SortOrder = sortOrder,
            Status = status.Trim().ToUpperInvariant(),
            CreatedByTenantUserId = createdByTenantUserId,
            UpdatedByTenantUserId = createdByTenantUserId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}
