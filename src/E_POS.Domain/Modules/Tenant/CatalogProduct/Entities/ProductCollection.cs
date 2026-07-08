using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;

public class ProductCollection : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid ProductId { get; protected set; }
    public Guid CollectionId { get; protected set; }
    public int SortOrder { get; protected set; }
    public Guid? CreatedByTenantUserId { get; protected set; }
    public Guid? UpdatedByTenantUserId { get; protected set; }

    public static ProductCollection Create(
        Guid id,
        Guid tenantId,
        Guid productId,
        Guid collectionId,
        int sortOrder,
        Guid? createdByTenantUserId,
        DateTimeOffset now)
    {
        return new ProductCollection
        {
            Id = id,
            TenantId = tenantId,
            ProductId = productId,
            CollectionId = collectionId,
            SortOrder = sortOrder,
            CreatedByTenantUserId = createdByTenantUserId,
            UpdatedByTenantUserId = createdByTenantUserId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}

