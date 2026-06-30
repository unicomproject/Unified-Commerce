using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.PricingTax.Entities;

public class PriceListOutlet : AuditableEntity
{
    public Guid? OutletId { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
    public Guid PriceListId { get; protected set; }
}
