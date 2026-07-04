using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.PricingTax.Entities;

public class ProductTaxAssignment : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid ProductId { get; protected set; }
    public Guid? ProductVariantId { get; protected set; }
    public Guid TaxClassId { get; protected set; }
    public DateTimeOffset? AppliesFrom { get; protected set; }
    public DateTimeOffset? AppliesUntil { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
}
