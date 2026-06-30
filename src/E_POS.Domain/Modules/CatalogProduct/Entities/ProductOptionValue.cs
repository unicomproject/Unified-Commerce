using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.CatalogProduct.Entities;

public class ProductOptionValue : AuditableEntity
{
    public string Name { get; protected set; } = string.Empty;
    public string OptionValueCode { get; protected set; } = string.Empty;
    public Guid ProductOptionId { get; protected set; }
    public int SortOrder { get; protected set; }
}
