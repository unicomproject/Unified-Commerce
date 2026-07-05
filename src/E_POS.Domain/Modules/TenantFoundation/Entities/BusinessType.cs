using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.TenantFoundation.Entities;

public class BusinessType : AuditableEntity
{
    public string BusinessTypeKey { get; protected set; } = string.Empty;
    public string BusinessTypeName { get; protected set; } = string.Empty;
    public string? Description { get; protected set; }
    public bool IsSystemType { get; protected set; }
    public int SortOrder { get; protected set; }
    public string Status { get; protected set; } = string.Empty;

    public static BusinessType Create(
        Guid id,
        string businessTypeKey,
        string businessTypeName,
        string? description,
        bool isSystemType,
        int sortOrder,
        string status,
        DateTimeOffset now)
    {
        return new BusinessType
        {
            Id = id,
            BusinessTypeKey = businessTypeKey.Trim(),
            BusinessTypeName = businessTypeName.Trim(),
            Description = description?.Trim(),
            IsSystemType = isSystemType,
            SortOrder = sortOrder,
            Status = status.Trim().ToUpperInvariant(),
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}