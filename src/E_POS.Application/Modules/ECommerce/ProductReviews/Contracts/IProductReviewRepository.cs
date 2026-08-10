using E_POS.Application.Modules.ECommerce.ProductReviews.Dtos;

namespace E_POS.Application.Modules.ECommerce.ProductReviews.Contracts;

public interface IProductReviewRepository
{
    Task<ProductReviewPageRepositoryResult> GetAsync(Guid tenantId, Guid? customerId, Guid productId, int page, int pageSize, string sort, int? rating, DateTimeOffset now, CancellationToken cancellationToken);
    Task<CustomerReviewPageRepositoryResult> GetCustomerReviewsAsync(
        Guid tenantId,
        Guid customerId,
        int page,
        int pageSize,
        string sort,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task<EligibleReviewsPageRepositoryResult> GetEligibleProductsForReviewAsync(
        Guid tenantId,
        Guid customerId,
        int page,
        int pageSize,
        DateTimeOffset now,
        CancellationToken cancellationToken);

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

public sealed record CustomerReviewPageRepositoryResult(string? ErrorCode, CustomerReviewsPageReadModel? Page)
{
    public bool IsSuccess => ErrorCode is null && Page is not null;
    public static CustomerReviewPageRepositoryResult Success(CustomerReviewsPageReadModel page) => new(null, page);
    public static CustomerReviewPageRepositoryResult Failure(string errorCode) => new(errorCode, null);
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

public sealed record EligibleReviewsPageRepositoryResult(string? ErrorCode, EligibleReviewsPageReadModel? Page)
{
    public bool IsSuccess => ErrorCode is null && Page is not null;
    public static EligibleReviewsPageRepositoryResult Success(EligibleReviewsPageReadModel page) => new(null, page);
    public static EligibleReviewsPageRepositoryResult Failure(string errorCode) => new(errorCode, null);
}
