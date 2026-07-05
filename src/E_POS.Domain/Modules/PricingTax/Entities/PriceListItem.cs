using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.PricingTax.Entities;

public class PriceListItem : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid PriceListId { get; protected set; }
    public Guid ProductId { get; protected set; }
    public Guid? ProductVariantId { get; protected set; }
    public Guid? UomId { get; protected set; }
    public decimal SellingPrice { get; protected set; }
    public decimal? CompareAtPrice { get; protected set; }
    public decimal MinQuantity { get; protected set; }
    public DateTimeOffset? ValidFrom { get; protected set; }
    public DateTimeOffset? ValidUntil { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
    public Guid? CreatedByTenantUserId { get; protected set; }
    public Guid? UpdatedByTenantUserId { get; protected set; }
}
