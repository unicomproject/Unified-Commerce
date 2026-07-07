using E_POS.Domain.Common.Entities;
using E_POS.Domain.Modules.Platform.PlatformAdmin.Constants;

namespace E_POS.Domain.Modules.Platform.PlatformAdmin.Entities;

public class PlatformRefreshToken : AuditableEntity
{
    public string Status { get; protected set; } = string.Empty;
    public Guid PlatformAuthSessionId { get; protected set; }
    public string TokenHash { get; protected set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; protected set; }

    public static PlatformRefreshToken Create(
        Guid id,
        Guid platformAuthSessionId,
        string tokenHash,
        DateTimeOffset expiresAt,
        DateTimeOffset now)
    {
        return new PlatformRefreshToken
        {
            Id = id,
            PlatformAuthSessionId = platformAuthSessionId,
            Status = PlatformAuthConstants.ActiveTokenStatus,
            TokenHash = tokenHash,
            ExpiresAt = expiresAt,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void Revoke(DateTimeOffset now)
    {
        if (Status == PlatformAuthConstants.RevokedTokenStatus)
        {
            return;
        }

        Status = PlatformAuthConstants.RevokedTokenStatus;
        UpdatedAt = now;
    }

    public void MarkUsed(DateTimeOffset now)
    {
        if (Status == PlatformAuthConstants.UsedTokenStatus)
        {
            return;
        }

        Status = PlatformAuthConstants.UsedTokenStatus;
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
}
