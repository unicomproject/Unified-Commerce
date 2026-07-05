using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.CatalogProduct.Entities;

public class ProductVariantOptionValue : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid ProductId { get; protected set; }
    public Guid ProductVariantId { get; protected set; }
    public Guid ProductOptionId { get; protected set; }
    public Guid ProductOptionValueId { get; protected set; }
    public Guid? CreatedByTenantUserId { get; protected set; }
    public Guid? UpdatedByTenantUserId { get; protected set; }

    public static ProductVariantOptionValue Create(
        Guid id,
        Guid tenantId,
        Guid productId,
        Guid productVariantId,
        Guid productOptionId,
        Guid productOptionValueId,
        Guid? createdByTenantUserId,
        DateTimeOffset now)
    {
        return new ProductVariantOptionValue
        {
            Id = id,
            TenantId = tenantId,
            ProductId = productId,
            ProductVariantId = productVariantId,
            ProductOptionId = productOptionId,
            ProductOptionValueId = productOptionValueId,
            CreatedByTenantUserId = createdByTenantUserId,
            UpdatedByTenantUserId = createdByTenantUserId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}
