using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.CatalogProduct.Entities;

public class ProductOptionTemplateValue : AuditableEntity
{
    public string Name { get; protected set; } = string.Empty;
    public Guid ProductOptionTemplateId { get; protected set; }
    public int SortOrder { get; protected set; }
    public string ValueCode { get; protected set; } = string.Empty;
}
