using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.AccessControl.Entities;

public class PermissionDefinition : AuditableEntity
{
    public string PermissionCode { get; protected set; } = string.Empty;
    public Guid ModuleId { get; protected set; }
    public Guid FeatureId { get; protected set; }
    public string ActionType { get; protected set; } = string.Empty;
    public string? Description { get; protected set; }
    public bool IsSystem { get; protected set; }
    public bool IsActive { get; protected set; }

    public static PermissionDefinition Create(
        Guid id,
        string permissionCode,
        Guid moduleId,
        Guid featureId,
        string actionType,
        string? description,
        bool isSystem,
        bool isActive,
        DateTimeOffset now)
    {
        return new PermissionDefinition
        {
            Id = id,
            PermissionCode = permissionCode.Trim(),
            ModuleId = moduleId,
            FeatureId = featureId,
            ActionType = actionType.Trim(),
            Description = description?.Trim(),
            IsSystem = isSystem,
            IsActive = isActive,
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}
