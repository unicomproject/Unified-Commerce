using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.AccessControl.Entities;

public class RoleTemplateVersion : AuditableEntity
{
    public Guid RoleTemplateId { get; protected set; }
    public int VersionNumber { get; protected set; }
    public string? VersionLabel { get; protected set; }
    public bool IsActive { get; protected set; }
    public DateTimeOffset EffectiveFrom { get; protected set; }
    public DateTimeOffset? EffectiveUntil { get; protected set; }
    public Guid? CreatedByTenantUserId { get; protected set; }

    public static RoleTemplateVersion Create(
        Guid id,
        Guid roleTemplateId,
        int versionNumber,
        string? versionLabel,
        bool isActive,
        DateTimeOffset effectiveFrom,
        DateTimeOffset? effectiveUntil,
        Guid? createdByTenantUserId,
        DateTimeOffset now)
    {
        return new RoleTemplateVersion
        {
            Id = id,
            RoleTemplateId = roleTemplateId,
            VersionNumber = versionNumber,
            VersionLabel = versionLabel?.Trim(),
            IsActive = isActive,
            EffectiveFrom = effectiveFrom,
            EffectiveUntil = effectiveUntil,
            CreatedByTenantUserId = createdByTenantUserId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}
