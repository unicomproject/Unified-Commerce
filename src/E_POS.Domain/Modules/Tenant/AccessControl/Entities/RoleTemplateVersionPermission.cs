using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.AccessControl.Entities;

public class RoleTemplateVersionPermission : AuditableEntity
{
    public Guid RoleTemplateVersionId { get; protected set; }
    public Guid PermissionDefinitionId { get; protected set; }
    public bool IsActive { get; protected set; }

    public static RoleTemplateVersionPermission Create(
        Guid id,
        Guid roleTemplateVersionId,
        Guid permissionDefinitionId,
        bool isActive,
        DateTimeOffset now)
    {
        return new RoleTemplateVersionPermission
        {
            Id = id,
            RoleTemplateVersionId = roleTemplateVersionId,
            PermissionDefinitionId = permissionDefinitionId,
            IsActive = isActive,
            CreatedAt = now,
            UpdatedAt = now // Required by AuditableEntity but ignored in DB
        };
    }
}
