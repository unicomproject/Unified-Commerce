using E_POS.Domain.Common.Entities;
using E_POS.Domain.Modules.Platform.PlatformAdmin.Constants;

namespace E_POS.Domain.Modules.Platform.PlatformAdmin.Entities;

public class PlatformAuthSession : AuditableEntity
{
    public Guid? PlatformUserId { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
    public string SessionTokenHash { get; protected set; } = string.Empty;
    public string? IpAddress { get; protected set; }
    public string? UserAgent { get; protected set; }
    public string? DeviceName { get; protected set; }
    public DateTimeOffset? LastSeenAt { get; protected set; }
    public DateTimeOffset? RevokedAt { get; protected set; }
    public Guid? RevokedByPlatformUserId { get; protected set; }
    public string? RevokeReason { get; protected set; }

    public static PlatformAuthSession Create(Guid id, Guid platformUserId, string sessionTokenHash, DateTimeOffset now)
    {
        return new PlatformAuthSession
        {
            Id = id,
            PlatformUserId = platformUserId,
            Status = PlatformAuthConstants.ActiveTokenStatus,
            SessionTokenHash = sessionTokenHash,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    /// <summary>
    /// Represents a pre-8B row shape (status only, no Second Brain timestamps) for migration backfill testing.
    /// </summary>
    public static PlatformAuthSession CreateLegacy(
        Guid id,
        Guid? platformUserId,
        string sessionTokenHash,
        string status,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
    {
        return new PlatformAuthSession
        {
            Id = id,
            PlatformUserId = platformUserId,
            Status = status,
            SessionTokenHash = sessionTokenHash,
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

    public void TouchLastSeen(DateTimeOffset now)
    {
        LastSeenAt = now;
        UpdatedAt = now;
    }

    public void RotateSessionToken(string sessionTokenHash, DateTimeOffset now)
    {
        SessionTokenHash = sessionTokenHash;
        UpdatedAt = now;
    }

    /// <summary>
    /// Applies Phase 8A backfill for Second Brain session columns without changing auth behavior.
    /// </summary>
    public void ApplyAlignmentBackfill()
    {
        if (Status == PlatformAuthConstants.RevokedTokenStatus && RevokedAt is null)
        {
            RevokedAt = UpdatedAt ?? CreatedAt;
        }
    }
}
