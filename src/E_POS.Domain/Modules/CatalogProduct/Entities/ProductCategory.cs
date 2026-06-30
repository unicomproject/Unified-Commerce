using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.CatalogProduct.Entities;

public class ProductCategory : AuditableEntity
{
    public Guid CategoryId { get; protected set; }
    public Guid ProductId { get; protected set; }
    public int SortOrder { get; protected set; }
}
