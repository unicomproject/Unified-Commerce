using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.AccessControl.Entities;

public class OutletUserRole : AuditableEntity
{
    public Guid? TenantUserId { get; protected set; }
    public Guid? OutletId { get; protected set; }
    public string? Description { get; protected set; }
    public Guid TenantRoleId { get; protected set; }
}

