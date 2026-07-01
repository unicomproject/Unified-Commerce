using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.PlatformAdministration.Entities;

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
            Status = "ACTIVE",
            SessionTokenHash = sessionTokenHash,
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}
