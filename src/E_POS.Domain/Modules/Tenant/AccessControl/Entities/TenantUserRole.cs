using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.AccessControl.Entities;

public class TenantUserRole : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid TenantUserId { get; protected set; }
    public Guid TenantRoleId { get; protected set; }
    public Guid? AssignedByTenantUserId { get; protected set; }
    public DateTimeOffset? AssignedAt { get; protected set; }
    public DateTimeOffset? RevokedAt { get; protected set; }

    public static TenantUserRole Create(
        Guid id,
        Guid tenantId,
        Guid tenantUserId,
        Guid tenantRoleId,
        Guid? assignedByTenantUserId,
        DateTimeOffset now)
    {
        return new TenantUserRole
        {
            Id = id,
            TenantId = tenantId,
            TenantUserId = tenantUserId,
            TenantRoleId = tenantRoleId,
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
