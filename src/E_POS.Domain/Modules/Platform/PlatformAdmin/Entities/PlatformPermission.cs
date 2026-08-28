using E_POS.Domain.Common.Entities;
using E_POS.Domain.Modules.Platform.Subscription.Entities;

namespace E_POS.Domain.Modules.Platform.PlatformAdmin.Entities;

public class PlatformPermission : AuditableEntity
{
    public string PermissionCode { get; protected set; } = string.Empty;
    public string Name { get; protected set; } = string.Empty;
    public string ModuleKey { get; protected set; } = string.Empty;
    public Guid PlatformModuleId { get; protected set; }
    public Guid PlatformFeatureId { get; protected set; }
    public PlatformModule? PlatformModule { get; protected set; }
    public PlatformFeature? PlatformFeature { get; protected set; }
    public string? Description { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
    public Guid? CreatedByPlatformUserId { get; protected set; }
    public Guid? UpdatedByPlatformUserId { get; protected set; }

    public static PlatformPermission Create(
        Guid id,
        string permissionCode,
        string name,
        string? description,
        string status,
        DateTimeOffset now,
        string? moduleKey = null,
        Guid? platformModuleId = null,
        Guid? platformFeatureId = null)
    {
        return new PlatformPermission
        {
            Id = id,
            PermissionCode = permissionCode,
            Name = name,
            ModuleKey = moduleKey ?? DeriveModuleKey(permissionCode),
            PlatformModuleId = platformModuleId ?? Guid.Empty,
            PlatformFeatureId = platformFeatureId ?? Guid.Empty,
            Description = description,
            Status = status,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void SetModuleAndFeature(Guid platformModuleId, Guid platformFeatureId, DateTimeOffset now)
    {
        PlatformModuleId = platformModuleId;
        PlatformFeatureId = platformFeatureId;
        UpdatedAt = now;
    }

    public void Activate(DateTimeOffset now)
    {
        Status = "ACTIVE";
        UpdatedAt = now;
    }

    public static string DeriveModuleKey(string permissionCode)
    {
        var segments = permissionCode.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length >= 2 &&
            string.Equals(segments[0], "platform", StringComparison.Ordinal))
        {
            return segments[1];
        }

        return "unknown";
    }
}

