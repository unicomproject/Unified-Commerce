using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Platform.PlatformAdmin.Entities;

public class PlatformUserPermission : AuditableEntity
{
    public Guid? PlatformUserId { get; protected set; }
    public string? Description { get; protected set; }
    public Guid PlatformPermissionId { get; protected set; }

    public static PlatformUserPermission Create(
        Guid id,
        Guid platformUserId,
        Guid platformPermissionId,
        string? description,
        DateTimeOffset now)
    {
        return new PlatformUserPermission
        {
            Id = id,
            PlatformUserId = platformUserId,
            PlatformPermissionId = platformPermissionId,
            Description = description,
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}

