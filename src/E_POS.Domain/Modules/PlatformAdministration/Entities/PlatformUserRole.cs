using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.PlatformAdministration.Entities;

public class PlatformUserRole : AuditableEntity
{
    public Guid? PlatformUserId { get; protected set; }
    public string? Description { get; protected set; }
    public Guid PlatformRoleId { get; protected set; }
}
