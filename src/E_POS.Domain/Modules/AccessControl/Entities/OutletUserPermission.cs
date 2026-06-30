using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.AccessControl.Entities;

public class OutletUserPermission : AuditableEntity
{
    public Guid? TenantUserId { get; protected set; }
    public Guid? OutletId { get; protected set; }
    public string? Description { get; protected set; }
    public Guid PermissionDefinitionId { get; protected set; }
}
