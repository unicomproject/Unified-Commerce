using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.AccessControl.Entities;

public class TenantUserTillAccess : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid TenantUserId { get; protected set; }
    public Guid TillId { get; protected set; }
    public Guid? AssignedByTenantUserId { get; protected set; }
    public Guid? RevokedByTenantUserId { get; protected set; }
    public DateTimeOffset AssignedAt { get; protected set; }
    public DateTimeOffset? RevokedAt { get; protected set; }

    public static TenantUserTillAccess Create(
        Guid id,
        Guid tenantId,
        Guid tenantUserId,
        Guid tillId,
        Guid? assignedByTenantUserId,
        DateTimeOffset now) =>
        new()
        {
            Id = id,
            TenantId = tenantId,
            TenantUserId = tenantUserId,
            TillId = tillId,
            AssignedByTenantUserId = assignedByTenantUserId,
            AssignedAt = now,
            CreatedAt = now,
            UpdatedAt = now
        };

    public void Revoke(Guid? revokedByTenantUserId, DateTimeOffset now)
    {
        RevokedByTenantUserId = revokedByTenantUserId;
        RevokedAt = now;
        UpdatedAt = now;
    }

    public void Reactivate(Guid? assignedByTenantUserId, DateTimeOffset now)
    {
        AssignedByTenantUserId = assignedByTenantUserId;
        RevokedByTenantUserId = null;
        AssignedAt = now;
        RevokedAt = null;
        UpdatedAt = now;
    }
}
