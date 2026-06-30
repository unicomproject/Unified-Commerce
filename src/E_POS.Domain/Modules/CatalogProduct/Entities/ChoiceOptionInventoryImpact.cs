using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.CatalogProduct.Entities;

public class ChoiceOptionInventoryImpact : AuditableEntity
{
    public Guid IngredientProductId { get; protected set; }
    public Guid ProductChoiceOptionId { get; protected set; }
    public decimal QuantityDelta { get; protected set; }
}
