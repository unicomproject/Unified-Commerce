using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Platform.PlatformAdmin.Entities;

public class PlatformLoginAudit : AuditableEntity
{
    public Guid? PlatformUserId { get; protected set; }
    public string LoginResult { get; protected set; } = string.Empty;

    public static PlatformLoginAudit Create(Guid id, Guid? platformUserId, string loginResult, DateTimeOffset now)
    {
        return new PlatformLoginAudit
        {
            Id = id,
            PlatformUserId = platformUserId,
            LoginResult = loginResult,
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}

