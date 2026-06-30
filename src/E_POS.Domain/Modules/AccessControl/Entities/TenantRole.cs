using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.AccessControl.Entities;

public class TenantRole : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public string RoleCode { get; protected set; } = string.Empty;
    public string Name { get; protected set; } = string.Empty;
    public string? Description { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
    public Guid RoleTemplateVersionId { get; protected set; }
}
