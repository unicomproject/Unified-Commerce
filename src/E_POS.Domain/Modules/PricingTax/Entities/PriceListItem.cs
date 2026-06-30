using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.PricingTax.Entities;

public class PriceListItem : AuditableEntity
{
    public decimal PriceAmount { get; protected set; }
    public Guid PriceListId { get; protected set; }
    public Guid ProductId { get; protected set; }
    public Guid? ProductVariantId { get; protected set; }
}
