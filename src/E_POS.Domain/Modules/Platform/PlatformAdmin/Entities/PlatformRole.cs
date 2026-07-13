using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Platform.PlatformAdmin.Entities;

public class PlatformRole : AuditableEntity
{
    public string RoleCode { get; protected set; } = string.Empty;
    public string Name { get; protected set; } = string.Empty;
    public string? Description { get; protected set; }
    public bool IsSystemRole { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
    public Guid? CreatedByPlatformUserId { get; protected set; }
    public Guid? UpdatedByPlatformUserId { get; protected set; }

    public static PlatformRole Create(
        Guid id,
        string roleCode,
        string name,
        string? description,
        string status,
        DateTimeOffset now,
        bool isSystemRole = false,
        Guid? createdByPlatformUserId = null)
    {
        return new PlatformRole
        {
            Id = id,
            RoleCode = roleCode,
            Name = name,
            Description = description,
            IsSystemRole = isSystemRole,
            Status = status,
            CreatedByPlatformUserId = createdByPlatformUserId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void UpdateDetails(string name, string? description, string status, DateTimeOffset now)
    {
        Name = name;
        Description = description;
        Status = status;
        UpdatedAt = now;
    }

    public void TouchUpdatedAt(DateTimeOffset now)
    {
        UpdatedAt = now;
    }
}
