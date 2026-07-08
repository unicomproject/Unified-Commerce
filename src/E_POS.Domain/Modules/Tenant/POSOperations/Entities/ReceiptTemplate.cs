using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.POSOperations.Entities;

public class ReceiptTemplate : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public string TemplateCode { get; protected set; } = string.Empty;
    public string TemplateName { get; protected set; } = string.Empty;
    public string TemplateType { get; protected set; } = string.Empty;
    public string? Description { get; protected set; }
    public bool IsBaseTemplate { get; protected set; }
    public Guid? ParentTemplateId { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
}

