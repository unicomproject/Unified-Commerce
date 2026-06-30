using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.CatalogProduct.Entities;

public class ProductChannelVisibility : AuditableEntity
{
    public Guid ProductId { get; protected set; }
    public Guid SalesChannelId { get; protected set; }
}
