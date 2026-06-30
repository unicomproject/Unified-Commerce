using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.POSOperations.Entities;

public class ReceiptTemplate : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public string TemplateCode { get; protected set; } = string.Empty;
    public string Name { get; protected set; } = string.Empty;
    public string? Description { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
    public Guid? ParentTemplateId { get; protected set; }
    public int SortOrder { get; protected set; }
}
