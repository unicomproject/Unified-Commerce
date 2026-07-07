using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.AccessControl.Entities;

public class RoleTemplate : AuditableEntity
{
    public string TemplateCode { get; protected set; } = string.Empty;
    public string TemplateName { get; protected set; } = string.Empty;
    public string? Description { get; protected set; }
    public bool IsDefault { get; protected set; }
    public bool IsActive { get; protected set; }
    public Guid? CreatedByTenantUserId { get; protected set; }
    public Guid? UpdatedByTenantUserId { get; protected set; }

    public static RoleTemplate Create(
        Guid id,
        string templateCode,
        string templateName,
        string? description,
        bool isDefault,
        bool isActive,
        Guid? createdByTenantUserId,
        DateTimeOffset now)
    {
        return new RoleTemplate
        {
            Id = id,
            TemplateCode = templateCode.Trim(),
            TemplateName = templateName.Trim(),
            Description = description?.Trim(),
            IsDefault = isDefault,
            IsActive = isActive,
            CreatedByTenantUserId = createdByTenantUserId,
            UpdatedByTenantUserId = createdByTenantUserId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void UpdateAudit(Guid? updatedBy, DateTimeOffset now)
    {
        UpdatedByTenantUserId = updatedBy;
        UpdatedAt = now;
    }
}
