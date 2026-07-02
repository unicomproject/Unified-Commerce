using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.PlatformAdministration.Entities;

public class PlatformRolePermission : AuditableEntity
{
    public string? Description { get; protected set; }
    public Guid PlatformPermissionId { get; protected set; }
    public Guid PlatformRoleId { get; protected set; }

    public static PlatformRolePermission Create(
        Guid id,
        Guid platformRoleId,
        Guid platformPermissionId,
        string? description,
        DateTimeOffset now)
    {
        return new PlatformRolePermission
        {
            Id = id,
            PlatformRoleId = platformRoleId,
            PlatformPermissionId = platformPermissionId,
            Description = description,
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}
