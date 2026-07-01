using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.PlatformAdministration.Entities;

public class PlatformPermission : AuditableEntity
{
    public string PermissionCode { get; protected set; } = string.Empty;
    public string Name { get; protected set; } = string.Empty;
    public string? Description { get; protected set; }
    public string Status { get; protected set; } = string.Empty;

    public static PlatformPermission Create(
        Guid id,
        string permissionCode,
        string name,
        string? description,
        string status,
        DateTimeOffset now)
    {
        return new PlatformPermission
        {
            Id = id,
            PermissionCode = permissionCode,
            Name = name,
            Description = description,
            Status = status,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void Activate(DateTimeOffset now)
    {
        Status = "ACTIVE";
        UpdatedAt = now;
    }
}
