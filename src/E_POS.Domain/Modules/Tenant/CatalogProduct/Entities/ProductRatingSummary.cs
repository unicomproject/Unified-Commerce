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

    public static ProductRatingSummary Create(
        Guid tenantId,
        Guid productId,
        DateTimeOffset now)
    {
        return new ProductRatingSummary
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ProductId = productId,
            AverageRating = 0,
            TotalReviews = 0,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public static ProductRatingSummary Create(Guid tenantId, Guid productId) =>
        Create(tenantId, productId, DateTimeOffset.UtcNow);

    public void Rebuild(IEnumerable<int> approvedRatings, DateTimeOffset now)
    {
        var ratings = approvedRatings.ToList();
        if (ratings.Any(x => x is < 1 or > 5))
            throw new ArgumentOutOfRangeException(nameof(approvedRatings), "Ratings must be between 1 and 5.");

        FiveStarCount = ratings.Count(x => x == 5);
        FourStarCount = ratings.Count(x => x == 4);
        ThreeStarCount = ratings.Count(x => x == 3);
        TwoStarCount = ratings.Count(x => x == 2);
        OneStarCount = ratings.Count(x => x == 1);
        TotalReviews = ratings.Count;
        AverageRating = TotalReviews == 0
            ? 0m
            : decimal.Round(ratings.Average(x => (decimal)x), 2, MidpointRounding.AwayFromZero);
        UpdatedAt = now;
    }
}
