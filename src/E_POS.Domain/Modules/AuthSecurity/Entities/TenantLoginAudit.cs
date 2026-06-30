using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.AuthSecurity.Entities;

public class TenantLoginAudit : AuditableEntity
{
    public Guid? TenantUserId { get; protected set; }
    public string LoginResult { get; protected set; } = string.Empty;
}
