using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.AccessControl.Entities;

public class RoleTemplateVersion : AuditableEntity
{
    public Guid RoleTemplateId { get; protected set; }
    public int VersionNumber { get; protected set; }
}
