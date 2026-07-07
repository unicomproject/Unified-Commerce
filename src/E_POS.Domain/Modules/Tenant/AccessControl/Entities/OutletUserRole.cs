using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.AccessControl.Entities;

public class OutletUserRole : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid OutletId { get; protected set; }
    public Guid TenantUserId { get; protected set; }
    public Guid TenantRoleId { get; protected set; }
    public Guid? AssignedByTenantUserId { get; protected set; }
    public Guid? RevokedByTenantUserId { get; protected set; }
    public DateTimeOffset? AssignedAt { get; protected set; }
    public DateTimeOffset? RevokedAt { get; protected set; }

    public static OutletUserRole Create(
        Guid id,
        Guid tenantId,
        Guid outletId,
        Guid tenantUserId,
        Guid tenantRoleId,
        Guid? assignedByTenantUserId,
        DateTimeOffset now)
    {
        return new OutletUserRole
        {
            Id = id,
            TenantId = tenantId,
            OutletId = outletId,
            TenantUserId = tenantUserId,
            TenantRoleId = tenantRoleId,
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
