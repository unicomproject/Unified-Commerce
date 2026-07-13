using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;

public class ProductReview : AuditableEntity
{
    public Guid TenantId { get; private set; }
    public Guid ProductId { get; private set; }
    public Guid CustomerId { get; private set; }
    
    public int RatingValue { get; private set; }
    public string ReviewTitle { get; private set; } = string.Empty;
    public string ReviewText { get; private set; } = string.Empty;
    public string Status { get; private set; } = string.Empty;

    protected ProductReview() { } // EF Core

    public static ProductReview Create(
        Guid tenantId,
        Guid productId,
        Guid customerId,
        int ratingValue,
        string reviewTitle,
        string reviewText,
        string status)
    {
        return new ProductReview
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ProductId = productId,
            CustomerId = customerId,
            RatingValue = ratingValue,
            ReviewTitle = reviewTitle,
            ReviewText = reviewText,
            Status = status
        };
    }
}
