using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.ECommerce.Customer.Entities;

public class CustomerWishlistItem : AuditableEntity
{
    public Guid TenantId { get; private set; }
    public Guid WishlistId { get; private set; }
    public Guid ProductId { get; private set; }
    public Guid? ProductVariantId { get; private set; }
    public DateTimeOffset AddedAt { get; private set; }

    protected CustomerWishlistItem() { } // EF Core

    internal static CustomerWishlistItem Create(
        Guid tenantId,
        Guid wishlistId,
        Guid productId,
        Guid? productVariantId,
        DateTimeOffset now)
    {
        return new CustomerWishlistItem
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            WishlistId = wishlistId,
            ProductId = productId,
            ProductVariantId = productVariantId,
            AddedAt = now,
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}
