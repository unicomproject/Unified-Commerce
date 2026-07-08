using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.PricingTax.Entities;

public class PriceListChannel : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid PriceListId { get; protected set; }
    public Guid SalesChannelId { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
    public Guid? CreatedByTenantUserId { get; protected set; }
    public Guid? UpdatedByTenantUserId { get; protected set; }

    public static PriceListChannel Create(
        Guid id,
        Guid tenantId,
        Guid priceListId,
        Guid salesChannelId,
        string status,
        Guid? createdByTenantUserId,
        DateTimeOffset now)
    {
        return new PriceListChannel
        {
            Id = id,
            TenantId = tenantId,
            PriceListId = priceListId,
            SalesChannelId = salesChannelId,
            Status = status.Trim().ToUpperInvariant(),
            CreatedByTenantUserId = createdByTenantUserId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}

