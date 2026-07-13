using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;

public class ProductRatingSummary : AuditableEntity
{
    public Guid TenantId { get; private set; }
    public Guid ProductId { get; private set; }
    
    public decimal AverageRating { get; private set; }
    public int TotalReviews { get; private set; }
    public int FiveStarCount { get; private set; }
    public int FourStarCount { get; private set; }
    public int ThreeStarCount { get; private set; }
    public int TwoStarCount { get; private set; }
    public int OneStarCount { get; private set; }

    protected ProductRatingSummary() { } // EF Core

    public static ProductRatingSummary Create(Guid tenantId, Guid productId)
    {
        return new ProductRatingSummary
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ProductId = productId,
            AverageRating = 0,
            TotalReviews = 0
        };
    }

    public void UpdateRating(int ratingValue, bool isNewReview = true)
    {
        // Complex rating calculation logic would go here in domain
        // Simplified for entity structure
    }
}
