using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.PricingTax.Entities;

public class PriceListChannel : AuditableEntity
{
    public string Status { get; protected set; } = string.Empty;
    public Guid PriceListId { get; protected set; }
    public Guid SalesChannelId { get; protected set; }
}
