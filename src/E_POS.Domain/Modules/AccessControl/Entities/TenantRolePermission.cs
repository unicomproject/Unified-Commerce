using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.AccessControl.Entities;

public class TenantRolePermission : AuditableEntity
{
    public string? Description { get; protected set; }
    public Guid PermissionDefinitionId { get; protected set; }
    public Guid TenantRoleId { get; protected set; }
}
