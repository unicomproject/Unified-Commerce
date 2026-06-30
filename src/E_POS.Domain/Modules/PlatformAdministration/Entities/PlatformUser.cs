using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.PlatformAdministration.Entities;

public class PlatformUser : AuditableEntity
{
    public string Email { get; protected set; } = string.Empty;
    public string NormalizedEmail { get; protected set; } = string.Empty;
    public string Status { get; protected set; } = string.Empty;
}
