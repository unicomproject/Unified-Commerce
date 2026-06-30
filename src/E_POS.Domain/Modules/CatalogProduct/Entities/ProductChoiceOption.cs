using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.CatalogProduct.Entities;

public class ProductChoiceOption : AuditableEntity
{
    public Guid ChoiceOptionId { get; protected set; }
    public Guid ProductChoiceGroupId { get; protected set; }
    public int SortOrder { get; protected set; }
}
