using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.PlatformAdministration.Entities;

public class PlatformRole : AuditableEntity
{
    public string RoleCode { get; protected set; } = string.Empty;
    public string Name { get; protected set; } = string.Empty;
    public string? Description { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
}
