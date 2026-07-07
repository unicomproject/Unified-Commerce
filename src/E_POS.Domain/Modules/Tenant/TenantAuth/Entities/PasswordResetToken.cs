using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.TenantAuth.Entities;

public class PasswordResetToken : AuditableEntity
{
    public Guid? TenantUserId { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
    public string TokenHash { get; protected set; } = string.Empty;
}

