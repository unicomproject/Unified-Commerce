using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.AccessControl.Entities;

public class RoleTemplateVersionPermission : AuditableEntity
{
    public string? Description { get; protected set; }
    public Guid PermissionDefinitionId { get; protected set; }
    public Guid RoleTemplateVersionId { get; protected set; }
}
