using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.CatalogProduct.Entities;

public class ComboGroupItem : AuditableEntity
{
    public Guid ComboGroupId { get; protected set; }
    public Guid ProductId { get; protected set; }
    public Guid ProductVariantId { get; protected set; }
    public int SortOrder { get; protected set; }
}
