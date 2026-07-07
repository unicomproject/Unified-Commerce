using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.AccessControl.Entities;

public class TenantRole : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public string RoleCode { get; protected set; } = string.Empty;
    public string Name { get; protected set; } = string.Empty;
    public string? Description { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
    public Guid RoleTemplateVersionId { get; protected set; }

    public static TenantRole Create(
        Guid id,
        Guid tenantId,
        string roleCode,
        string name,
        string? description,
        string status,
        Guid roleTemplateVersionId,
        DateTimeOffset now)
    {
        return new TenantRole
        {
            Id = id,
            TenantId = tenantId,
            RoleCode = roleCode.Trim(),
            Name = name.Trim(),
            Description = description?.Trim(),
            Status = status,
            RoleTemplateVersionId = roleTemplateVersionId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}

