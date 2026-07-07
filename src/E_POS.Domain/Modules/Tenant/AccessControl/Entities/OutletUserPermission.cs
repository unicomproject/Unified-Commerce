using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.AccessControl.Entities;

public class OutletUserPermission : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid OutletId { get; protected set; }
    public Guid TenantUserId { get; protected set; }
    public Guid PermissionDefinitionId { get; protected set; }
    public Guid? AssignedByTenantUserId { get; protected set; }
    public Guid? RevokedByTenantUserId { get; protected set; }
    public DateTimeOffset? AssignedAt { get; protected set; }
    public DateTimeOffset? RevokedAt { get; protected set; }

    public static OutletUserPermission Create(
        Guid id,
        Guid tenantId,
        Guid outletId,
        Guid tenantUserId,
        Guid permissionDefinitionId,
        Guid? assignedByTenantUserId,
        DateTimeOffset now)
    {
        return new OutletUserPermission
        {
            Id = id,
            TenantId = tenantId,
            OutletId = outletId,
            TenantUserId = tenantUserId,
            PermissionDefinitionId = permissionDefinitionId,
            AssignedByTenantUserId = assignedByTenantUserId,
            AssignedAt = now,
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
