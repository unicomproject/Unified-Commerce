using E_POS.Application.Modules.ECommerce.ProductReviews.Contracts;
using E_POS.Application.Modules.ECommerce.ProductReviews.Dtos;
using CustomerEntity = E_POS.Domain.Modules.ECommerce.Customer.Entities.Customer;
using E_POS.Domain.Modules.Platform.Subscription.Constants;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Constants;
using E_POS.Infrastructure.Modules.Platform.Subscription.Entitlements;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace E_POS.Infrastructure.Modules.ECommerce.ProductReviews.Repositories;

public sealed class ProductReviewRepository : IProductReviewRepository
{
    private const string ActiveCustomerStatus = "ACTIVE";
    private const string CompletedOrderStatus = "COMPLETED";
    private const string FulfilledStatus = "FULFILLED";
    private const string UniqueReviewConstraint = "ux_product_reviews_tenant_product_customer";

    private readonly EPosDbContext _dbContext;

    public ProductReviewRepository(EPosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ProductReviewPageRepositoryResult> GetAsync(
        Guid tenantId,
        Guid productId,
        int page,
        int pageSize,
        string sort,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var accessError = await GetAccessErrorAsync(tenantId, now, cancellationToken);
        if (accessError is not null)
            return ProductReviewPageRepositoryResult.Failure(accessError);

        if (!await IsActiveProductAsync(tenantId, productId, cancellationToken))
            return ProductReviewPageRepositoryResult.Failure("product_reviews.product_not_found");

        var query = _dbContext.ProductReviews
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId &&
                x.ProductId == productId &&
                x.Status == ProductReviewConstants.ApprovedStatus);

        var totalCount = await query.CountAsync(cancellationToken);
        var orderedQuery = sort switch
        {
            "oldest" => query.OrderBy(x => x.CreatedAt).ThenBy(x => x.Id),
            "highest" => query.OrderByDescending(x => x.RatingValue).ThenByDescending(x => x.CreatedAt),
            "lowest" => query.OrderBy(x => x.RatingValue).ThenByDescending(x => x.CreatedAt),
            _ => query.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id)
        };

        var reviews = await orderedQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        var customers = await LoadCustomersAsync(
            tenantId,
            reviews.Select(x => x.CustomerId),
            cancellationToken);
        var summary = await ReadSummaryAsync(tenantId, productId, totalCount, cancellationToken);

        return ProductReviewPageRepositoryResult.Success(new ProductReviewsPageReadModel
        {
            ProductId = productId,
            Summary = summary,
            Items = reviews.Select(x => MapReview(x, customers.GetValueOrDefault(x.CustomerId), true)).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        });
    }

    public async Task<ProductReviewMutationRepositoryResult> CreateAsync(
        Guid tenantId,
        Guid customerId,
        Guid productId,
        CreateProductReviewRequest request,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var context = await ValidateMutationContextAsync(
            tenantId, customerId, productId, now, cancellationToken);
        if (context.ErrorCode is not null)
            return ProductReviewMutationRepositoryResult.Failure(context.ErrorCode);

        if (!await HasPurchasedAsync(tenantId, customerId, productId, cancellationToken))
            return ProductReviewMutationRepositoryResult.Failure("product_reviews.purchase_required");

        var reviews = await _dbContext.ProductReviews
            .Where(x => x.TenantId == tenantId && x.ProductId == productId)
            .ToListAsync(cancellationToken);
        var existing = reviews.SingleOrDefault(x => x.CustomerId == customerId);
        ProductReview review;

        if (existing is not null && existing.Status != ProductReviewConstants.DeletedStatus)
            return ProductReviewMutationRepositoryResult.Failure("product_reviews.duplicate_review");

        if (existing is not null)
        {
            existing.RestoreApproved(
                request.RatingValue, request.ReviewTitle, request.ReviewText, customerId, now);
            review = existing;
        }
        else
        {
            review = ProductReview.CreateApproved(
                tenantId,
                productId,
                customerId,
                request.RatingValue,
                request.ReviewTitle,
                request.ReviewText,
                now);
            reviews.Add(review);
            _dbContext.ProductReviews.Add(review);
        }

        await RebuildSummaryAsync(tenantId, productId, reviews, now, cancellationToken);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueReviewViolation(exception))
        {
            return ProductReviewMutationRepositoryResult.Failure("product_reviews.duplicate_review");
        }

        return ProductReviewMutationRepositoryResult.Success(MapReview(review, context.Customer, true));
    }

    public async Task<ProductReviewMutationRepositoryResult> UpdateAsync(
        Guid tenantId,
        Guid customerId,
        Guid reviewId,
        UpdateProductReviewRequest request,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var accessError = await GetAccessErrorAsync(tenantId, now, cancellationToken);
        if (accessError is not null)
            return ProductReviewMutationRepositoryResult.Failure(accessError);

        var customer = await GetActiveCustomerAsync(tenantId, customerId, cancellationToken);
        if (customer is null)
            return ProductReviewMutationRepositoryResult.Failure("product_reviews.customer_not_found");

        var review = await _dbContext.ProductReviews.SingleOrDefaultAsync(
            x =>
                x.Id == reviewId &&
                x.TenantId == tenantId &&
                x.CustomerId == customerId &&
                x.Status != ProductReviewConstants.DeletedStatus,
            cancellationToken);
        if (review is null)
            return ProductReviewMutationRepositoryResult.Failure("product_reviews.review_not_found");

        if (!await IsActiveProductAsync(tenantId, review.ProductId, cancellationToken))
            return ProductReviewMutationRepositoryResult.Failure("product_reviews.product_not_found");

        review.UpdateApproved(
            request.RatingValue, request.ReviewTitle, request.ReviewText, customerId, now);
        var reviews = await _dbContext.ProductReviews
            .Where(x => x.TenantId == tenantId && x.ProductId == review.ProductId)
            .ToListAsync(cancellationToken);
        await RebuildSummaryAsync(tenantId, review.ProductId, reviews, now, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return ProductReviewMutationRepositoryResult.Success(MapReview(review, customer, true));
    }

    public async Task<ProductReviewDeleteRepositoryResult> DeleteAsync(
        Guid tenantId,
        Guid customerId,
        Guid reviewId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var accessError = await GetAccessErrorAsync(tenantId, now, cancellationToken);
        if (accessError is not null)
            return ProductReviewDeleteRepositoryResult.Failure(accessError);

        if (await GetActiveCustomerAsync(tenantId, customerId, cancellationToken) is null)
            return ProductReviewDeleteRepositoryResult.Failure("product_reviews.customer_not_found");

        var review = await _dbContext.ProductReviews.SingleOrDefaultAsync(
            x =>
                x.Id == reviewId &&
                x.TenantId == tenantId &&
                x.CustomerId == customerId &&
                x.Status != ProductReviewConstants.DeletedStatus,
            cancellationToken);
        if (review is null)
            return ProductReviewDeleteRepositoryResult.Failure("product_reviews.review_not_found");

        review.SoftDelete(customerId, now);
        var reviews = await _dbContext.ProductReviews
            .Where(x => x.TenantId == tenantId && x.ProductId == review.ProductId)
            .ToListAsync(cancellationToken);
        await RebuildSummaryAsync(tenantId, review.ProductId, reviews, now, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return ProductReviewDeleteRepositoryResult.Success();
    }

    private async Task<string?> GetAccessErrorAsync(
        Guid tenantId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var tenantAvailable = await _dbContext.Tenants.AsNoTracking().AnyAsync(
            x => x.Id == tenantId && x.Status == TenantStatusConstants.Active,
            cancellationToken);
        if (!tenantAvailable)
            return "product_reviews.tenant_unavailable";

        var entitlementRows = await (
            from entitlement in _dbContext.TenantFeatureEntitlements.AsNoTracking()
            join feature in _dbContext.PlatformFeatures.AsNoTracking()
                on entitlement.PlatformFeatureId equals feature.Id
            where entitlement.TenantId == tenantId &&
                  feature.FeatureCode == PlatformTenantFeatureCodes.OnlineStore &&
                  feature.Status == SubscriptionCatalogConstants.RecordStatus.Active
            select new
            {
                entitlement.EntitlementStatus,
                entitlement.IsEnabled,
                entitlement.RevokedAt,
                entitlement.EffectiveFrom,
                entitlement.EffectiveUntil
            }).ToListAsync(cancellationToken);

        return entitlementRows.Any(x => TenantEntitlementEffectivePredicate.IsEnabled(
            x.EntitlementStatus,
            x.IsEnabled,
            x.RevokedAt,
            x.EffectiveFrom,
            x.EffectiveUntil,
            now))
            ? null
            : "product_reviews.feature_disabled";
    }

    private async Task<(string? ErrorCode, CustomerEntity? Customer)> ValidateMutationContextAsync(
        Guid tenantId,
        Guid customerId,
        Guid productId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var accessError = await GetAccessErrorAsync(tenantId, now, cancellationToken);
        if (accessError is not null)
            return (accessError, null);

        var customer = await GetActiveCustomerAsync(tenantId, customerId, cancellationToken);
        if (customer is null)
            return ("product_reviews.customer_not_found", null);
        if (!await IsActiveProductAsync(tenantId, productId, cancellationToken))
            return ("product_reviews.product_not_found", null);
        return (null, customer);
    }

    private Task<CustomerEntity?> GetActiveCustomerAsync(
        Guid tenantId,
        Guid customerId,
        CancellationToken cancellationToken) =>
        _dbContext.Customers.AsNoTracking().SingleOrDefaultAsync(
            x => x.Id == customerId && x.TenantId == tenantId && x.Status == ActiveCustomerStatus,
            cancellationToken);

    private Task<bool> IsActiveProductAsync(
        Guid tenantId,
        Guid productId,
        CancellationToken cancellationToken) =>
        _dbContext.Products.AsNoTracking().AnyAsync(
            x =>
                x.Id == productId &&
                x.TenantId == tenantId &&
                x.Status == ProductConstants.ActiveStatus &&
                x.IsSellable,
            cancellationToken);

    private Task<bool> HasPurchasedAsync(
        Guid tenantId,
        Guid customerId,
        Guid productId,
        CancellationToken cancellationToken) =>
        _dbContext.SalesOrders.AsNoTracking().AnyAsync(
            order =>
                order.TenantId == tenantId &&
                order.CustomerId == customerId &&
                order.Status == CompletedOrderStatus &&
                order.FulfillmentStatus == FulfilledStatus &&
                _dbContext.SalesOrderLines.AsNoTracking().Any(line =>
                    line.TenantId == tenantId &&
                    line.SalesOrderId == order.Id &&
                    line.ProductId == productId &&
                    line.FulfilledQuantity > 0),
            cancellationToken);

    private async Task<Dictionary<Guid, CustomerEntity>> LoadCustomersAsync(
        Guid tenantId,
        IEnumerable<Guid> customerIds,
        CancellationToken cancellationToken)
    {
        var ids = customerIds.Distinct().ToList();
        if (ids.Count == 0)
            return [];
        return await _dbContext.Customers.AsNoTracking()
            .Where(x => x.TenantId == tenantId && ids.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);
    }

    private async Task<ProductReviewSummaryReadModel> ReadSummaryAsync(
        Guid tenantId,
        Guid productId,
        int approvedCount,
        CancellationToken cancellationToken)
    {
        var summary = await _dbContext.ProductRatingSummaries.AsNoTracking().SingleOrDefaultAsync(
            x => x.TenantId == tenantId && x.ProductId == productId,
            cancellationToken);
        if (summary is not null && summary.TotalReviews == approvedCount)
            return MapSummary(summary);

        var ratings = await _dbContext.ProductReviews.AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId &&
                x.ProductId == productId &&
                x.Status == ProductReviewConstants.ApprovedStatus)
            .Select(x => x.RatingValue)
            .ToListAsync(cancellationToken);
        return BuildSummary(ratings);
    }

    private async Task RebuildSummaryAsync(
        Guid tenantId,
        Guid productId,
        IReadOnlyCollection<ProductReview> reviews,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var summary = await _dbContext.ProductRatingSummaries.SingleOrDefaultAsync(
            x => x.TenantId == tenantId && x.ProductId == productId,
            cancellationToken);
        if (summary is null)
        {
            summary = ProductRatingSummary.Create(tenantId, productId, now);
            _dbContext.ProductRatingSummaries.Add(summary);
        }

        summary.Rebuild(
            reviews.Where(x => x.Status == ProductReviewConstants.ApprovedStatus)
                .Select(x => x.RatingValue),
            now);
    }

    private static ProductReviewItemReadModel MapReview(
        ProductReview review,
        CustomerEntity? customer,
        bool isVerifiedPurchase) => new()
    {
        Id = review.Id,
        ProductId = review.ProductId,
        RatingValue = review.RatingValue,
        ReviewTitle = review.ReviewTitle,
        ReviewText = review.ReviewText,
        CustomerDisplayName = BuildCustomerDisplayName(customer),
        IsVerifiedPurchase = isVerifiedPurchase,
        Status = review.Status,
        CreatedAt = review.CreatedAt,
        UpdatedAt = review.UpdatedAt
    };

    private static string BuildCustomerDisplayName(CustomerEntity? customer)
    {
        if (customer is null)
            return "Verified customer";
        var firstName = customer.FirstName?.Trim();
        var lastName = customer.LastName?.Trim();
        if (!string.IsNullOrWhiteSpace(firstName) && !string.IsNullOrWhiteSpace(lastName))
            return $"{firstName} {char.ToUpperInvariant(lastName[0])}.";
        return string.IsNullOrWhiteSpace(firstName) ? "Verified customer" : firstName;
    }

    private static ProductReviewSummaryReadModel MapSummary(ProductRatingSummary summary) => new()
    {
        AverageRating = summary.AverageRating,
        TotalReviews = summary.TotalReviews,
        FiveStarCount = summary.FiveStarCount,
        FourStarCount = summary.FourStarCount,
        ThreeStarCount = summary.ThreeStarCount,
        TwoStarCount = summary.TwoStarCount,
        OneStarCount = summary.OneStarCount
    };

    private static ProductReviewSummaryReadModel BuildSummary(IReadOnlyCollection<int> ratings) => new()
    {
        AverageRating = ratings.Count == 0
            ? 0m
            : decimal.Round(ratings.Average(x => (decimal)x), 2, MidpointRounding.AwayFromZero),
        TotalReviews = ratings.Count,
        FiveStarCount = ratings.Count(x => x == 5),
        FourStarCount = ratings.Count(x => x == 4),
        ThreeStarCount = ratings.Count(x => x == 3),
        TwoStarCount = ratings.Count(x => x == 2),
        OneStarCount = ratings.Count(x => x == 1)
    };

    private static bool IsUniqueReviewViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: UniqueReviewConstraint
        };
}
