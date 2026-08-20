using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Platform.PlatformAdmin.Entities;

public class PlatformUserInvitation : AuditableEntity
{
    public Guid PlatformUserId { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public string Status { get; private set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? SentAt { get; private set; }

    public static PlatformUserInvitation Create(
        Guid id,
        Guid platformUserId,
        string tokenHash,
        DateTimeOffset expiresAt,
        DateTimeOffset now)
    {
        return new PlatformUserInvitation
        {
            Id = id,
            PlatformUserId = platformUserId,
            TokenHash = tokenHash,
            Status = "PENDING",
            ExpiresAt = expiresAt,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void MarkSent(DateTimeOffset now)
    {
        Status = "SENT";
        SentAt = now;
        UpdatedAt = now;
    }
}
