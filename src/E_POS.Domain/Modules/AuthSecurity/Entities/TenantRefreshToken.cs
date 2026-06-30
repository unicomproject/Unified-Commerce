using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.AuthSecurity.Entities;

public class TenantRefreshToken : AuditableEntity
{
    public string Status { get; protected set; } = string.Empty;
    public Guid TenantAuthSessionId { get; protected set; }
    public string TokenHash { get; protected set; } = string.Empty;
}
