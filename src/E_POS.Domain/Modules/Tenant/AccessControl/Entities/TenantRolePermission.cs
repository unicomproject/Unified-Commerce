using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.AccessControl.Entities;

public class TenantRolePermission : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid TenantRoleId { get; protected set; }
    public Guid PermissionDefinitionId { get; protected set; }
    public Guid? GrantedByTenantUserId { get; protected set; }
    public Guid? RevokedByTenantUserId { get; protected set; }
    public DateTimeOffset? GrantedAt { get; protected set; }
    public DateTimeOffset? RevokedAt { get; protected set; }
    public string? Notes { get; protected set; }

    public static TenantRolePermission Create(
        Guid id,
        Guid tenantId,
        Guid tenantRoleId,
        Guid permissionDefinitionId,
        Guid? grantedByTenantUserId,
        DateTimeOffset now)
    {
        return new TenantRolePermission
        {
            Id = id,
            TenantId = tenantId,
            TenantRoleId = tenantRoleId,
            PermissionDefinitionId = permissionDefinitionId,
            GrantedByTenantUserId = grantedByTenantUserId,
            GrantedAt = now,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void Revoke(Guid revokedByTenantUserId, DateTimeOffset now)
    {
        RevokedByTenantUserId = revokedByTenantUserId;
        RevokedAt = now;
        UpdatedAt = now;
    }
}
