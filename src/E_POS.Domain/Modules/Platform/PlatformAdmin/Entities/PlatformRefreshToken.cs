using E_POS.Domain.Common.Entities;
using E_POS.Domain.Modules.Platform.PlatformAdmin.Constants;

namespace E_POS.Domain.Modules.Platform.PlatformAdmin.Entities;

public class PlatformRefreshToken : AuditableEntity
{
    public string Status { get; protected set; } = string.Empty;
    public Guid PlatformAuthSessionId { get; protected set; }
    public string TokenHash { get; protected set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; protected set; }
    public Guid? PlatformUserId { get; protected set; }
    public Guid? TokenFamilyId { get; protected set; }
    public Guid? ReplacedByTokenId { get; protected set; }
    public DateTimeOffset? UsedAt { get; protected set; }
    public DateTimeOffset? RevokedAt { get; protected set; }
    public Guid? RevokedByPlatformUserId { get; protected set; }
    public string? RevokeReason { get; protected set; }

    public static PlatformRefreshToken Create(
        Guid id,
        Guid platformAuthSessionId,
        string tokenHash,
        DateTimeOffset expiresAt,
        DateTimeOffset now,
        Guid? platformUserId = null,
        Guid? tokenFamilyId = null)
    {
        return new PlatformRefreshToken
        {
            Id = id,
            PlatformAuthSessionId = platformAuthSessionId,
            Status = PlatformAuthConstants.ActiveTokenStatus,
            TokenHash = tokenHash,
            ExpiresAt = expiresAt,
            PlatformUserId = platformUserId,
            TokenFamilyId = tokenFamilyId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    /// <summary>
    /// Represents a pre-8B row shape (status only, no Second Brain timestamps) for migration backfill testing.
    /// </summary>
    public static PlatformRefreshToken CreateLegacy(
        Guid id,
        Guid platformAuthSessionId,
        string tokenHash,
        string status,
        DateTimeOffset expiresAt,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
    {
        return new PlatformRefreshToken
        {
            Id = id,
            PlatformAuthSessionId = platformAuthSessionId,
            Status = status,
            TokenHash = tokenHash,
            ExpiresAt = expiresAt,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };
    }

    public void Revoke(DateTimeOffset now, Guid? revokedByPlatformUserId = null, string? reason = null)
    {
        if (Status == PlatformAuthConstants.RevokedTokenStatus)
        {
            return;
        }

        Status = PlatformAuthConstants.RevokedTokenStatus;
        RevokedAt = now;
        RevokedByPlatformUserId = revokedByPlatformUserId;
        RevokeReason = reason;
        UpdatedAt = now;
    }

    public void MarkUsed(DateTimeOffset now)
    {
        if (Status == PlatformAuthConstants.UsedTokenStatus)
        {
            return;
        }

        Status = PlatformAuthConstants.UsedTokenStatus;
        UsedAt = now;
        UpdatedAt = now;
    }

    public void MarkExpired(DateTimeOffset now)
    {
        if (Status == PlatformAuthConstants.ExpiredTokenStatus)
        {
            return;
        }

        Status = PlatformAuthConstants.ExpiredTokenStatus;
        UpdatedAt = now;
    }

    public void LinkReplacement(Guid replacedByTokenId, DateTimeOffset now)
    {
        ReplacedByTokenId = replacedByTokenId;
        UpdatedAt = now;
    }

    /// <summary>
    /// Applies Phase 8A backfill for Second Brain refresh-token columns without changing auth behavior.
    /// </summary>
    public void ApplyAlignmentBackfill(Guid? platformUserIdFromSession)
    {
        if (PlatformUserId is null && platformUserIdFromSession is not null)
        {
            PlatformUserId = platformUserIdFromSession;
        }

        if (TokenFamilyId is null)
        {
            TokenFamilyId = Id;
        }

        if (Status == PlatformAuthConstants.UsedTokenStatus && UsedAt is null)
        {
            UsedAt = UpdatedAt ?? CreatedAt;
        }

        if ((Status == PlatformAuthConstants.RevokedTokenStatus ||
             Status == PlatformAuthConstants.ExpiredTokenStatus) &&
            RevokedAt is null)
        {
            RevokedAt = UpdatedAt ?? CreatedAt;
        }
    }
}
