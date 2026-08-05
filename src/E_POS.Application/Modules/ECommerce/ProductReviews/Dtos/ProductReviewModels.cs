namespace E_POS.Application.Modules.ECommerce.ProductReviews.Dtos;

public sealed class CreateProductReviewRequest
{
    public int RatingValue { get; set; }
    public string? ReviewTitle { get; set; }
    public string? ReviewText { get; set; }
    public bool? IsRecommended { get; set; }
}

public sealed class UpdateProductReviewRequest
{
    public int RatingValue { get; set; }
    public string? ReviewTitle { get; set; }
    public string? ReviewText { get; set; }
    public bool? IsRecommended { get; set; }
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
    public bool? IsRecommended { get; set; }
    public string CustomerDisplayName { get; set; } = string.Empty;
    public bool IsVerifiedPurchase { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}

public sealed class ProductReviewsPageReadModel
{
    public Guid ProductId { get; set; }
    public bool CanWriteReview { get; set; }
    public ProductReviewSummaryReadModel Summary { get; set; } = new();
    public IReadOnlyList<ProductReviewItemReadModel> Items { get; set; } = [];
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
}

public sealed class CustomerReviewItemReadModel
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ProductThumbnailUrl { get; set; }
    public int RatingValue { get; set; }
    public string? ReviewTitle { get; set; }
    public string? ReviewText { get; set; }
    public bool? IsRecommended { get; set; }
    public bool IsVerifiedPurchase { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}

public sealed class CustomerReviewsPageReadModel
{
    public IReadOnlyList<CustomerReviewItemReadModel> Items { get; set; } = [];
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
}
