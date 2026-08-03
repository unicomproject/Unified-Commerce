using E_POS.Domain.Common.Entities;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;

namespace E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;

public class ProductRecommendationLink : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid SourceProductId { get; protected set; }
    public Guid? SourceVariantId { get; protected set; }
    public Guid RecommendedProductId { get; protected set; }
    public Guid? RecommendedVariantId { get; protected set; }
    public string RecommendationType { get; protected set; } = string.Empty;
    public Guid? OutletId { get; protected set; }
    public Guid? SalesChannelId { get; protected set; }
    public int SortOrder { get; protected set; }
    public DateTimeOffset? ValidFrom { get; protected set; }
    public DateTimeOffset? ValidUntil { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
    public Guid? CreatedByTenantUserId { get; protected set; }
    public Guid? UpdatedByTenantUserId { get; protected set; }

    protected ProductRecommendationLink() { }

    public static ProductRecommendationLink Create(
        Guid id, Guid tenantId, Guid sourceProductId, Guid? sourceVariantId,
        Guid recommendedProductId, Guid? recommendedVariantId,
        string recommendationType, Guid? outletId, Guid? salesChannelId,
        int sortOrder, DateTimeOffset? validFrom, DateTimeOffset? validUntil,
        string status, Guid? createdByTenantUserId, DateTimeOffset now)
    {
        if (sourceProductId == recommendedProductId)
            throw new InvalidOperationException("A product cannot recommend itself.");
        if (sortOrder < 0)
            throw new ArgumentOutOfRangeException(nameof(sortOrder));
        if (validFrom.HasValue && validUntil.HasValue && validUntil < validFrom)
            throw new ArgumentException("Valid until cannot be earlier than valid from.", nameof(validUntil));

        return new ProductRecommendationLink
        {
            Id = id,
            TenantId = tenantId,
            SourceProductId = sourceProductId,
            SourceVariantId = sourceVariantId,
            RecommendedProductId = recommendedProductId,
            RecommendedVariantId = recommendedVariantId,
            RecommendationType = ProductRecommendationConstants.NormalizeType(recommendationType),
            OutletId = outletId,
            SalesChannelId = salesChannelId,
            SortOrder = sortOrder,
            ValidFrom = validFrom,
            ValidUntil = validUntil,
            Status = ProductRecommendationConstants.NormalizeStatus(status),
            CreatedByTenantUserId = createdByTenantUserId,
            UpdatedByTenantUserId = createdByTenantUserId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}
