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

    public static PriceListItem Create(
        Guid id,
        Guid tenantId,
        Guid priceListId,
        Guid productId,
        Guid? productVariantId,
        Guid? uomId,
        decimal sellingPrice,
        decimal? compareAtPrice,
        decimal minQuantity,
        DateTimeOffset? validFrom,
        DateTimeOffset? validUntil,
        string status,
        Guid? createdByTenantUserId,
        DateTimeOffset now)
    {
        return new PriceListItem
        {
            Id = id,
            TenantId = tenantId,
            PriceListId = priceListId,
            ProductId = productId,
            ProductVariantId = productVariantId,
            UomId = uomId,
            SellingPrice = sellingPrice,
            CompareAtPrice = compareAtPrice,
            MinQuantity = minQuantity,
            ValidFrom = validFrom,
            ValidUntil = validUntil,
            Status = status.Trim().ToUpperInvariant(),
            CreatedByTenantUserId = createdByTenantUserId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void UpdateProfile(
        decimal sellingPrice,
        decimal? compareAtPrice,
        decimal minQuantity,
        DateTimeOffset? validFrom,
        DateTimeOffset? validUntil,
        string status,
        Guid? updatedByTenantUserId,
        DateTimeOffset now)
    {
        SellingPrice = sellingPrice;
        CompareAtPrice = compareAtPrice;
        MinQuantity = minQuantity;
        ValidFrom = validFrom;
        ValidUntil = validUntil;
        Status = status.Trim().ToUpperInvariant();
        UpdatedByTenantUserId = updatedByTenantUserId;
        UpdatedAt = now;
    }

    public void SoftDelete(Guid? deletedByUserId, DateTimeOffset now)
    {
        Status = "DELETED";
        UpdatedByTenantUserId = deletedByUserId;
        UpdatedAt = now;
    }
}
