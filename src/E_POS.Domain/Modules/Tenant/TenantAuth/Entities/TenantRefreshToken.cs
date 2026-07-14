using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.TenantAuth.Entities;

public class TenantRefreshToken : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid TenantAuthSessionId { get; protected set; }
    public Guid UserId { get; protected set; }
    public string TokenHash { get; protected set; } = string.Empty;
    public Guid TokenFamilyId { get; protected set; }
    public Guid? ReplacedByTokenId { get; protected set; }
    public DateTimeOffset? UsedAt { get; protected set; }
    public DateTimeOffset ExpiresAt { get; protected set; }
    public DateTimeOffset? RevokedAt { get; protected set; }
    public Guid? RevokedByTenantUserId { get; protected set; }
    public Guid? RevokedByPlatformUserId { get; protected set; }
    public string? RevokeReason { get; protected set; }

    public static TenantRefreshToken Create(
        Guid id,
        Guid tenantId,
        Guid tenantAuthSessionId,
        Guid userId,
        string tokenHash,
        Guid tokenFamilyId,
        DateTimeOffset expiresAt,
        DateTimeOffset now)
    {
        return new TenantRefreshToken
        {
            Id = id,
            TenantId = tenantId,
            TenantAuthSessionId = tenantAuthSessionId,
            UserId = userId,
            TokenHash = tokenHash,
            TokenFamilyId = tokenFamilyId,
            ExpiresAt = expiresAt,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void Revoke(DateTimeOffset now, Guid? revokedByTenantUserId = null, Guid? revokedByPlatformUserId = null, string? reason = null)
    {
        if (RevokedAt.HasValue) return;
        RevokedAt = now;
        RevokedByTenantUserId = revokedByTenantUserId;
        RevokedByPlatformUserId = revokedByPlatformUserId;
        RevokeReason = reason;
        UpdatedAt = now;
    }

    public void MarkRotated(Guid replacementTokenId, DateTimeOffset now)
    {
        if (UsedAt.HasValue || RevokedAt.HasValue || ReplacedByTokenId.HasValue)
        {
            throw new InvalidOperationException("Refresh token has already been consumed.");
        }

        UsedAt = now;
        ReplacedByTokenId = replacementTokenId;
        UpdatedAt = now;
    }
}
