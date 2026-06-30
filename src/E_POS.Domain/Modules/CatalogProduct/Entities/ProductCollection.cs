using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.CatalogProduct.Entities;

public class ProductCollection : AuditableEntity
{
    public Guid CollectionId { get; protected set; }
    public Guid ProductId { get; protected set; }
}
