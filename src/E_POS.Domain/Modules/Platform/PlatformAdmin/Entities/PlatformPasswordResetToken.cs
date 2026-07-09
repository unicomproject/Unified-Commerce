using E_POS.Domain.Common.Entities;
using E_POS.Domain.Modules.Platform.PlatformAdmin.Constants;

namespace E_POS.Domain.Modules.Platform.PlatformAdmin.Entities;

public class PlatformPasswordResetToken : AuditableEntity
{
    public Guid? PlatformUserId { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
    public string TokenHash { get; protected set; } = string.Empty;
    public DateTimeOffset? RequestedAt { get; protected set; }
    public DateTimeOffset? ExpiresAt { get; protected set; }
    public DateTimeOffset? UsedAt { get; protected set; }
    public DateTimeOffset? RevokedAt { get; protected set; }

    public static PlatformPasswordResetToken CreatePending(
        Guid id,
        Guid platformUserId,
        string tokenHash,
        DateTimeOffset expiresAt,
        DateTimeOffset now)
    {
        return new PlatformPasswordResetToken
        {
            Id = id,
            PlatformUserId = platformUserId,
            TokenHash = tokenHash,
            Status = PlatformAuthConstants.PendingTokenStatus,
            RequestedAt = now,
            ExpiresAt = expiresAt,
            CreatedAt = now,
            UpdatedAt = now
        };
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

    public void Revoke(DateTimeOffset now)
    {
        if (Status == PlatformAuthConstants.RevokedTokenStatus)
        {
            return;
        }

        Status = PlatformAuthConstants.RevokedTokenStatus;
        RevokedAt = now;
        UpdatedAt = now;
    }

    /// <summary>
    /// Represents a pre-8A row shape for migration backfill testing.
    /// </summary>
    public static PlatformPasswordResetToken CreateLegacy(
        Guid id,
        Guid? platformUserId,
        string tokenHash,
        string status,
        DateTimeOffset createdAt)
    {
        return new PlatformPasswordResetToken
        {
            Id = id,
            PlatformUserId = platformUserId,
            TokenHash = tokenHash,
            Status = status,
            CreatedAt = createdAt,
            UpdatedAt = createdAt
        };
    }

    /// <summary>
    /// Applies Phase 8A backfill for Second Brain password-reset columns without inventing expiry.
    /// </summary>
    public void ApplyAlignmentBackfill()
    {
        if (RequestedAt is null)
        {
            RequestedAt = CreatedAt;
        }

        if (Status == PlatformAuthConstants.UsedTokenStatus && UsedAt is null)
        {
            UsedAt = UpdatedAt ?? CreatedAt;
        }

        if (Status == PlatformAuthConstants.RevokedTokenStatus && RevokedAt is null)
        {
            RevokedAt = UpdatedAt ?? CreatedAt;
        }
    }
}
