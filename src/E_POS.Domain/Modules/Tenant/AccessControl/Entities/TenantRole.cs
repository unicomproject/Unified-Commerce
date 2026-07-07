using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.AccessControl.Entities;

public class TenantRole : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid? SourceRoleTemplateId { get; protected set; }
    public Guid? SourceRoleTemplateVersionId { get; protected set; }
    public string RoleName { get; protected set; } = string.Empty;
    public string RoleCode { get; protected set; } = string.Empty;
    public string? RoleDescription { get; protected set; }
    public bool? IsCustom { get; protected set; }
    public bool IsActive { get; protected set; }
    public Guid? CreatedByTenantUserId { get; protected set; }
    public Guid? UpdatedByTenantUserId { get; protected set; }

    public static TenantRole Create(
        Guid id,
        Guid tenantId,
        Guid? sourceRoleTemplateId,
        Guid? sourceRoleTemplateVersionId,
        string roleCode,
        string roleName,
        string? roleDescription,
        bool? isCustom,
        bool isActive,
        Guid? createdByTenantUserId,
        DateTimeOffset now)
    {
        return new TenantRole
        {
            Id = id,
            TenantId = tenantId,
            SourceRoleTemplateId = sourceRoleTemplateId,
            SourceRoleTemplateVersionId = sourceRoleTemplateVersionId,
            RoleCode = roleCode.Trim(),
            RoleName = roleName.Trim(),
            RoleDescription = roleDescription?.Trim(),
            IsCustom = isCustom,
            IsActive = isActive,
            CreatedByTenantUserId = createdByTenantUserId,
            UpdatedByTenantUserId = createdByTenantUserId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void UpdateAudit(Guid updatedBy, DateTimeOffset now)
    {
        UpdatedByTenantUserId = updatedBy;
        UpdatedAt = now;
    }
}
