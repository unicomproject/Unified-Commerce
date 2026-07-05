using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.PricingTax.Entities;

public class PriceListOutlet : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid PriceListId { get; protected set; }
    public Guid OutletId { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
    public Guid? CreatedByTenantUserId { get; protected set; }
    public Guid? UpdatedByTenantUserId { get; protected set; }
}
