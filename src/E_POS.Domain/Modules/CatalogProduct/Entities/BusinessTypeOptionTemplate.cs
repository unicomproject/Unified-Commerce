using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.CatalogProduct.Entities;

public class BusinessTypeOptionTemplate : AuditableEntity
{
    public string? Description { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
    public Guid BusinessTypeId { get; protected set; }
    public Guid ProductOptionTemplateId { get; protected set; }
    public int SortOrder { get; protected set; }
}
