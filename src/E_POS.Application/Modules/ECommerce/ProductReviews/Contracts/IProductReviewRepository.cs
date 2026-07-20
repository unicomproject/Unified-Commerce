using E_POS.Application.Modules.ECommerce.ProductReviews.Dtos;

namespace E_POS.Application.Modules.ECommerce.ProductReviews.Contracts;

public interface IProductReviewRepository
{
    Task<ProductReviewPageRepositoryResult> GetAsync(Guid tenantId, Guid productId, int page, int pageSize, string sort, DateTimeOffset now, CancellationToken cancellationToken);
    Task<ProductReviewMutationRepositoryResult> CreateAsync(Guid tenantId, Guid customerId, Guid productId, CreateProductReviewRequest request, DateTimeOffset now, CancellationToken cancellationToken);
    Task<ProductReviewMutationRepositoryResult> UpdateAsync(Guid tenantId, Guid customerId, Guid reviewId, UpdateProductReviewRequest request, DateTimeOffset now, CancellationToken cancellationToken);
    Task<ProductReviewDeleteRepositoryResult> DeleteAsync(Guid tenantId, Guid customerId, Guid reviewId, DateTimeOffset now, CancellationToken cancellationToken);
}

public sealed record ProductReviewPageRepositoryResult(string? ErrorCode, ProductReviewsPageReadModel? Page)
{
    public bool IsSuccess => ErrorCode is null && Page is not null;
    public static ProductReviewPageRepositoryResult Success(ProductReviewsPageReadModel page) => new(null, page);
    public static ProductReviewPageRepositoryResult Failure(string errorCode) => new(errorCode, null);
}

public sealed record ProductReviewMutationRepositoryResult(string? ErrorCode, ProductReviewItemReadModel? Review)
{
    public bool IsSuccess => ErrorCode is null && Review is not null;
    public static ProductReviewMutationRepositoryResult Success(ProductReviewItemReadModel review) => new(null, review);
    public static ProductReviewMutationRepositoryResult Failure(string errorCode) => new(errorCode, null);
}

public sealed record ProductReviewDeleteRepositoryResult(string? ErrorCode)
{
    public bool IsSuccess => ErrorCode is null;
    public static ProductReviewDeleteRepositoryResult Success() => new((string?)null);
    public static ProductReviewDeleteRepositoryResult Failure(string errorCode) => new(errorCode);
}
