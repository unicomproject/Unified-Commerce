using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Platform.PlatformAdmin.Entities;

public class PlatformUserPermission : AuditableEntity
{
    public Guid? PlatformUserId { get; protected set; }
    public string? Description { get; protected set; }
    public Guid PlatformPermissionId { get; protected set; }
    public DateTimeOffset? AssignedAt { get; protected set; }
    public Guid? AssignedByPlatformUserId { get; protected set; }
    public DateTimeOffset? RevokedAt { get; protected set; }
    public Guid? RevokedByPlatformUserId { get; protected set; }
    public string? RevokedReason { get; protected set; }

    public static PlatformUserPermission Create(
        Guid id,
        Guid platformUserId,
        Guid platformPermissionId,
        string? description,
        DateTimeOffset now,
        Guid? assignedByPlatformUserId = null)
    {
        return new PlatformUserPermission
        {
            Id = id,
            PlatformUserId = platformUserId,
            PlatformPermissionId = platformPermissionId,
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
