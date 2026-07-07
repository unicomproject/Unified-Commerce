using E_POS.Domain.Common.Entities;
using E_POS.Domain.Modules.Platform.PlatformAdmin.Constants;

namespace E_POS.Domain.Modules.Platform.PlatformAdmin.Entities;

public class PlatformAuthSession : AuditableEntity
{
    public Guid? PlatformUserId { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
    public string SessionTokenHash { get; protected set; } = string.Empty;

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

    public void Revoke(DateTimeOffset now)
    {
        if (Status == PlatformAuthConstants.RevokedTokenStatus)
        {
            return;
        }

        Status = PlatformAuthConstants.RevokedTokenStatus;
        UpdatedAt = now;
    }

    public void RotateSessionToken(string sessionTokenHash, DateTimeOffset now)
    {
        SessionTokenHash = sessionTokenHash;
        UpdatedAt = now;
    }
}
