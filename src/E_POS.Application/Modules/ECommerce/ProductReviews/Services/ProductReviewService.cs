using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.ECommerce.ProductReviews.Contracts;
using E_POS.Application.Modules.ECommerce.ProductReviews.Dtos;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;

namespace E_POS.Application.Modules.ECommerce.ProductReviews.Services;

public sealed class ProductReviewService : IProductReviewService
{
    private static readonly HashSet<string> AllowedSorts =
        new(StringComparer.OrdinalIgnoreCase) { "newest", "oldest", "highest", "lowest" };

    private readonly IProductReviewRepository _repository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ProductReviewService(IProductReviewRepository repository, IDateTimeProvider dateTimeProvider)
    {
        _repository = repository;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<ApplicationResult<ProductReviewsPageReadModel>> GetAsync(
        Guid tenantId,
        Guid productId,
        int page,
        int pageSize,
        string? sort,
        CancellationToken cancellationToken)
    {
        if (tenantId == Guid.Empty)
            return PageFailure("product_reviews.invalid_tenant_context", "A valid storefront tenant context is required.");
        if (productId == Guid.Empty)
            return PageFailure("product_reviews.invalid_product_id", "A valid product id is required.");
        if (page < 1 || pageSize < 1 || pageSize > 50)
            return PageFailure("product_reviews.invalid_paging", "Page must be at least 1 and pageSize must be between 1 and 50.");

        var normalizedSort = string.IsNullOrWhiteSpace(sort) ? "newest" : sort.Trim().ToLowerInvariant();
        if (!AllowedSorts.Contains(normalizedSort))
            return PageFailure("product_reviews.invalid_sort", "Sort must be newest, oldest, highest, or lowest.");

        var result = await _repository.GetAsync(
            tenantId, productId, page, pageSize, normalizedSort,
            _dateTimeProvider.UtcNow, cancellationToken);
        return result.IsSuccess
            ? ApplicationResult<ProductReviewsPageReadModel>.Success(result.Page!)
            : ApplicationResult<ProductReviewsPageReadModel>.Failure(MapError(result.ErrorCode!));
    }

    public async Task<ApplicationResult<ProductReviewItemReadModel>> CreateAsync(
        Guid tenantId,
        Guid customerId,
        Guid productId,
        CreateProductReviewRequest request,
        CancellationToken cancellationToken)
    {
        var validation = ValidateMutation(
            tenantId, customerId, productId, request.RatingValue,
            request.ReviewTitle, request.ReviewText, isReviewId: false);
        if (validation is not null)
            return ApplicationResult<ProductReviewItemReadModel>.Failure(validation);

        var result = await _repository.CreateAsync(
            tenantId, customerId, productId, request,
            _dateTimeProvider.UtcNow, cancellationToken);
        return result.IsSuccess
            ? ApplicationResult<ProductReviewItemReadModel>.Success(result.Review!)
            : ApplicationResult<ProductReviewItemReadModel>.Failure(MapError(result.ErrorCode!));
    }

    public async Task<ApplicationResult<ProductReviewItemReadModel>> UpdateAsync(
        Guid tenantId,
        Guid customerId,
        Guid reviewId,
        UpdateProductReviewRequest request,
        CancellationToken cancellationToken)
    {
        var validation = ValidateMutation(
            tenantId, customerId, reviewId, request.RatingValue,
            request.ReviewTitle, request.ReviewText, isReviewId: true);
        if (validation is not null)
            return ApplicationResult<ProductReviewItemReadModel>.Failure(validation);

        var result = await _repository.UpdateAsync(
            tenantId, customerId, reviewId, request,
            _dateTimeProvider.UtcNow, cancellationToken);
        return result.IsSuccess
            ? ApplicationResult<ProductReviewItemReadModel>.Success(result.Review!)
            : ApplicationResult<ProductReviewItemReadModel>.Failure(MapError(result.ErrorCode!));
    }

    public async Task<ApplicationResult> DeleteAsync(
        Guid tenantId,
        Guid customerId,
        Guid reviewId,
        CancellationToken cancellationToken)
    {
        if (tenantId == Guid.Empty || customerId == Guid.Empty)
            return ApplicationResult.Failure(MapError("product_reviews.invalid_customer_context"));
        if (reviewId == Guid.Empty)
            return ApplicationResult.Failure(MapError("product_reviews.invalid_review_id"));

        var result = await _repository.DeleteAsync(
            tenantId, customerId, reviewId, _dateTimeProvider.UtcNow, cancellationToken);
        return result.IsSuccess
            ? ApplicationResult.Success()
            : ApplicationResult.Failure(MapError(result.ErrorCode!));
    }

    private static ApplicationError? ValidateMutation(
        Guid tenantId,
        Guid customerId,
        Guid resourceId,
        int ratingValue,
        string? reviewTitle,
        string? reviewText,
        bool isReviewId)
    {
        if (tenantId == Guid.Empty || customerId == Guid.Empty)
            return MapError("product_reviews.invalid_customer_context");
        if (resourceId == Guid.Empty)
            return MapError(isReviewId
                ? "product_reviews.invalid_review_id"
                : "product_reviews.invalid_product_id");
        if (ratingValue is < ProductReviewConstants.MinimumRating or > ProductReviewConstants.MaximumRating)
            return Error("product_reviews.invalid_rating", "Rating must be between 1 and 5.");
        if (reviewTitle?.Trim().Length > ProductReviewConstants.MaximumTitleLength)
            return Error("product_reviews.title_too_long", "Review title must not exceed 150 characters.");
        if (reviewText?.Trim().Length > ProductReviewConstants.MaximumTextLength)
            return Error("product_reviews.text_too_long", "Review text must not exceed 5000 characters.");
        return null;
    }

    private static ApplicationResult<ProductReviewsPageReadModel> PageFailure(string code, string message) =>
        ApplicationResult<ProductReviewsPageReadModel>.Failure(Error(code, message));

    private static ApplicationError MapError(string code) => code switch
    {
        "product_reviews.invalid_tenant_context" => Error(code, "A valid storefront tenant context is required."),
        "product_reviews.invalid_customer_context" => Error(code, "A valid customer session is required."),
        "product_reviews.invalid_product_id" => Error(code, "A valid product id is required."),
        "product_reviews.invalid_review_id" => Error(code, "A valid review id is required."),
        "product_reviews.tenant_unavailable" => Error(code, "Storefront tenant not found or unavailable."),
        "product_reviews.feature_disabled" => Error(code, "The online store feature is not enabled for this tenant."),
        "product_reviews.customer_not_found" => Error(code, "Customer not found or unavailable."),
        "product_reviews.product_not_found" => Error(code, "Product not found or unavailable."),
        "product_reviews.purchase_required" => Error(code, "Only customers who purchased this product can review it."),
        "product_reviews.duplicate_review" => Error(code, "You have already reviewed this product."),
        "product_reviews.review_not_found" => Error(code, "Review not found or access denied."),
        _ => Error(code, "The product review operation could not be completed.")
    };

    private static ApplicationError Error(string code, string message) => new(code, message);
}
