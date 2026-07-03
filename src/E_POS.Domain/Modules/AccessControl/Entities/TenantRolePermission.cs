using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.AccessControl.Entities;

public class TenantRolePermission : AuditableEntity
{
    public string? Description { get; protected set; }
    public Guid PermissionDefinitionId { get; protected set; }
    public Guid TenantRoleId { get; protected set; }

    public static TenantRolePermission Create(
        Guid id,
        Guid tenantRoleId,
        Guid permissionDefinitionId,
        string? description,
        DateTimeOffset now)
    {
        return new TenantRolePermission
        {
            Id = id,
            TenantRoleId = tenantRoleId,
            PermissionDefinitionId = permissionDefinitionId,
            Description = description?.Trim(),
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}
