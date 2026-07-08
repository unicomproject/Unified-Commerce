using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.TenantAuth.Entities;

public class EmailVerificationToken : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid UserId { get; protected set; }
    public string EmailToVerify { get; protected set; } = string.Empty;
    public string NormalizedEmailToVerify { get; protected set; } = string.Empty;
    public string TokenHash { get; protected set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; protected set; }
    public DateTimeOffset? VerifiedAt { get; protected set; }
    public DateTimeOffset? RevokedAt { get; protected set; }
}

