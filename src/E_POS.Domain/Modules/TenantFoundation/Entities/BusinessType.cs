using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.TenantFoundation.Entities;

public class BusinessType : AuditableEntity
{
    public string Name { get; protected set; } = string.Empty;
    public string? Description { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
    public string BusinessTypeCode { get; protected set; } = string.Empty;
}
