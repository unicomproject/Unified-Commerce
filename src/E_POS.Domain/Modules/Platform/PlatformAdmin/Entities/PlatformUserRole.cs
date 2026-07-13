using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Platform.PlatformAdmin.Entities;

public class PlatformUserRole : AuditableEntity
{
    public Guid? PlatformUserId { get; protected set; }
    public string? Description { get; protected set; }
    public Guid PlatformRoleId { get; protected set; }
    public DateTimeOffset? AssignedAt { get; protected set; }
    public Guid? AssignedByPlatformUserId { get; protected set; }
    public DateTimeOffset? RevokedAt { get; protected set; }
    public Guid? RevokedByPlatformUserId { get; protected set; }
    public string? RevokedReason { get; protected set; }

    public static PlatformUserRole Create(
        Guid id,
        Guid platformUserId,
        Guid platformRoleId,
        string? description,
        DateTimeOffset now,
        Guid? assignedByPlatformUserId = null)
    {
        return new PlatformUserRole
        {
            Id = id,
            PlatformUserId = platformUserId,
            PlatformRoleId = platformRoleId,
            Description = description,
            AssignedAt = now,
            AssignedByPlatformUserId = assignedByPlatformUserId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void Revoke(Guid? revokedByPlatformUserId, string? revokedReason, DateTimeOffset now)
    {
        if (RevokedAt is not null)
        {
            return;
        }

        RevokedAt = now;
        RevokedByPlatformUserId = revokedByPlatformUserId;
        RevokedReason = revokedReason;
        UpdatedAt = now;
    }
}
