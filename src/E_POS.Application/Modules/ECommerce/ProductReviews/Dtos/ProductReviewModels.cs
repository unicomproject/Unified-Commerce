namespace E_POS.Application.Modules.ECommerce.ProductReviews.Dtos;

public sealed class CreateProductReviewRequest
{
    public int RatingValue { get; set; }
    public string? ReviewTitle { get; set; }
    public string? ReviewText { get; set; }
}

public sealed class UpdateProductReviewRequest
{
    public int RatingValue { get; set; }
    public string? ReviewTitle { get; set; }
    public string? ReviewText { get; set; }
}

public sealed class ProductReviewSummaryReadModel
{
    public decimal AverageRating { get; set; }
    public int TotalReviews { get; set; }
    public int FiveStarCount { get; set; }
    public int FourStarCount { get; set; }
    public int ThreeStarCount { get; set; }
    public int TwoStarCount { get; set; }
    public int OneStarCount { get; set; }
}

public sealed class ProductReviewItemReadModel
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public int RatingValue { get; set; }
    public string? ReviewTitle { get; set; }
    public string? ReviewText { get; set; }
    public string CustomerDisplayName { get; set; } = string.Empty;
    public bool IsVerifiedPurchase { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}

public sealed class ProductReviewsPageReadModel
{
    public Guid ProductId { get; set; }
    public ProductReviewSummaryReadModel Summary { get; set; } = new();
    public IReadOnlyList<ProductReviewItemReadModel> Items { get; set; } = [];
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
}
