using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.AccessControl.Entities;

public class TenantUserPermission : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid TenantUserId { get; protected set; }
    public Guid PermissionDefinitionId { get; protected set; }
    public Guid? AssignedByTenantUserId { get; protected set; }
    public DateTimeOffset? AssignedAt { get; protected set; }
    public DateTimeOffset? RevokedAt { get; protected set; }

    public static TenantUserPermission Create(
        Guid id,
        Guid tenantId,
        Guid tenantUserId,
        Guid permissionDefinitionId,
        Guid? assignedByTenantUserId,
        DateTimeOffset now)
    {
        return new TenantUserPermission
        {
            Id = id,
            TenantId = tenantId,
            TenantUserId = tenantUserId,
            PermissionDefinitionId = permissionDefinitionId,
            AssignedByTenantUserId = assignedByTenantUserId,
            AssignedAt = now,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void Revoke(DateTimeOffset now)
    {
        RevokedAt = now;
        UpdatedAt = now;
    }
}
