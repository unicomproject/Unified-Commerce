using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.PlatformAdministration.Entities;

public class PlatformRefreshToken : AuditableEntity
{
    public string Status { get; protected set; } = string.Empty;
    public Guid PlatformAuthSessionId { get; protected set; }
    public string TokenHash { get; protected set; } = string.Empty;
}
