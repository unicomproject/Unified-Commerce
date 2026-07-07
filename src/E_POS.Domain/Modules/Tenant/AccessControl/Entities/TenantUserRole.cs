using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.AccessControl.Entities;

public class TenantUserRole : AuditableEntity
{
    public Guid? TenantUserId { get; protected set; }
    public string? Description { get; protected set; }
    public Guid TenantRoleId { get; protected set; }

    public static TenantUserRole Create(
        Guid id,
        Guid tenantUserId,
        Guid tenantRoleId,
        string? description,
        DateTimeOffset now)
    {
        return new TenantUserRole
        {
            Id = id,
            TenantUserId = tenantUserId,
            TenantRoleId = tenantRoleId,
            Description = description?.Trim(),
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}

