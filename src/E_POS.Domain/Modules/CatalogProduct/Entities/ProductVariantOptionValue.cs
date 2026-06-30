using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.CatalogProduct.Entities;

public class ProductVariantOptionValue : AuditableEntity
{
    public Guid ProductOptionValueId { get; protected set; }
    public Guid? ProductVariantId { get; protected set; }
}
