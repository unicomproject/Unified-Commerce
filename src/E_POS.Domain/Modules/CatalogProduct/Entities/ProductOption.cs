using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.CatalogProduct.Entities;

public class ProductOption : AuditableEntity
{
    public string Name { get; protected set; } = string.Empty;
    public string OptionCode { get; protected set; } = string.Empty;
    public Guid ProductId { get; protected set; }
    public Guid ProductOptionTemplateId { get; protected set; }
    public int SortOrder { get; protected set; }
}
