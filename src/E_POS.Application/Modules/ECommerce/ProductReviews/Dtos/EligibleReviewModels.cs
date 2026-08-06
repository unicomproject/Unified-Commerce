namespace E_POS.Application.Modules.ECommerce.ProductReviews.Dtos;

public sealed record EligibleReviewItemReadModel
{
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public string? ProductThumbnailUrl { get; init; }
}

public sealed record EligibleReviewsPageReadModel
{
    public IReadOnlyCollection<EligibleReviewItemReadModel> Items { get; init; } = Array.Empty<EligibleReviewItemReadModel>();
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
}
