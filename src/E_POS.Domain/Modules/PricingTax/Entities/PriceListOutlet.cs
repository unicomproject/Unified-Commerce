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

    public static PriceListOutlet Create(
        Guid id,
        Guid tenantId,
        Guid priceListId,
        Guid outletId,
        string status,
        Guid? createdByTenantUserId,
        DateTimeOffset now)
    {
        return new PriceListOutlet
        {
            Id = id,
            TenantId = tenantId,
            PriceListId = priceListId,
            OutletId = outletId,
            Status = status.Trim().ToUpperInvariant(),
            CreatedByTenantUserId = createdByTenantUserId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}
