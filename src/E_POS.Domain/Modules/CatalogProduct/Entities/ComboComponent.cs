using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.CatalogProduct.Entities;

public class ComboComponent : AuditableEntity
{
    public Guid ComboDefinitionId { get; protected set; }
    public Guid ComponentProductId { get; protected set; }
    public Guid ComponentVariantId { get; protected set; }
    public decimal Quantity { get; protected set; }
    public int SortOrder { get; protected set; }
}
