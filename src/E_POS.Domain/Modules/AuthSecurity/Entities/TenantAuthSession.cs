using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.AuthSecurity.Entities;

public class TenantAuthSession : AuditableEntity
{
    public Guid? TenantUserId { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
    public string SessionTokenHash { get; protected set; } = string.Empty;
}
