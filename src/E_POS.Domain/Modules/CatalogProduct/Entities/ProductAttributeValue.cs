using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.CatalogProduct.Entities;

public class ProductAttributeValue : AuditableEntity
{
    public Guid AttributeDefinitionId { get; protected set; }
    public Guid ProductId { get; protected set; }
}
