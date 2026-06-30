using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.PlatformAdministration.Entities;

public class PlatformRolePermission : AuditableEntity
{
    public string? Description { get; protected set; }
    public Guid PlatformPermissionId { get; protected set; }
    public Guid PlatformRoleId { get; protected set; }
}
