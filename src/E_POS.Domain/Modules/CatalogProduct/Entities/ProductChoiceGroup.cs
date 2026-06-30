using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.CatalogProduct.Entities;

public class ProductChoiceGroup : AuditableEntity
{
    public Guid ChoiceGroupId { get; protected set; }
    public Guid ProductId { get; protected set; }
}
