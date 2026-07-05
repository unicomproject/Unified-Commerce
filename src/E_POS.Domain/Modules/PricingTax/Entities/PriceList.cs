using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.PricingTax.Entities;

public class PriceList : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public string PriceListCode { get; protected set; } = string.Empty;
    public string PriceListName { get; protected set; } = string.Empty;
    public string PriceListType { get; protected set; } = string.Empty;
    public string CurrencyCode { get; protected set; } = string.Empty;
    public bool PriceIncludesTax { get; protected set; }
    public bool IsDefaultPriceList { get; protected set; }
    public int Priority { get; protected set; }
    public DateTimeOffset? ValidFrom { get; protected set; }
    public DateTimeOffset? ValidUntil { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
    public Guid? CreatedByTenantUserId { get; protected set; }
    public Guid? UpdatedByTenantUserId { get; protected set; }
}
