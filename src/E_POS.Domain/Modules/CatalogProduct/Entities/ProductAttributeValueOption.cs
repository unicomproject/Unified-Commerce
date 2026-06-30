using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.CatalogProduct.Entities;

public class ProductAttributeValueOption : AuditableEntity
{
    public Guid AttributeOptionId { get; protected set; }
    public Guid ProductAttributeValueId { get; protected set; }
    public int SortOrder { get; protected set; }
}
