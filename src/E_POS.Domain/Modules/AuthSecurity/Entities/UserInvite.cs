using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.AuthSecurity.Entities;

public class UserInvite : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public string InviteStatus { get; protected set; } = string.Empty;
    public string InviteTokenHash { get; protected set; } = string.Empty;
}
