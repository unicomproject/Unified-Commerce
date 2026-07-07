using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.TenantAuth.Entities;

public class PasswordResetToken : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid UserId { get; protected set; }
    public string TokenHash { get; protected set; } = string.Empty;
    public string? RequestedIpAddress { get; protected set; }
    public string? RequestedUserAgent { get; protected set; }
    public DateTimeOffset ExpiresAt { get; protected set; }
    public DateTimeOffset? UsedAt { get; protected set; }
    public DateTimeOffset? RevokedAt { get; protected set; }
}

