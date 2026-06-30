using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.CatalogProduct.Entities;

public class ProductImage : AuditableEntity
{
    public string ImageUrl { get; protected set; } = string.Empty;
    public Guid ProductId { get; protected set; }
    public int SortOrder { get; protected set; }
}
