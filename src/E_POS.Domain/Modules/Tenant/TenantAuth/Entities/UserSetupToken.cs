using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.TenantAuth.Entities;

public class UserSetupToken : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid UserId { get; protected set; }
    public string TokenHash { get; protected set; } = string.Empty;
    public string Purpose { get; protected set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; protected set; }
    public DateTimeOffset? UsedAt { get; protected set; }
    public DateTimeOffset? RevokedAt { get; protected set; }
}

