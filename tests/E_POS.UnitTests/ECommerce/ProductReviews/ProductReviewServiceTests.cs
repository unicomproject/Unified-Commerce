using E_POS.Application.Common.Contracts;
using E_POS.Application.Modules.ECommerce.ProductReviews.Contracts;
using E_POS.Application.Modules.ECommerce.ProductReviews.Dtos;
using E_POS.Application.Modules.ECommerce.ProductReviews.Services;
using Xunit;

namespace E_POS.UnitTests.ECommerce.ProductReviews;

public sealed class ProductReviewServiceTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid CustomerId = Guid.NewGuid();
    private static readonly Guid ProductId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 7, 16, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task GetAsync_InvalidPaging_DoesNotCallRepository()
    {
        var repository = new FakeRepository();
        var service = CreateService(repository);

        var result = await service.GetAsync(
            TenantId, ProductId, 0, 51, "newest", CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("product_reviews.invalid_paging", result.Error.Code);
        Assert.Equal(0, repository.CallCount);
    }

    [Fact]
    public async Task GetAsync_ValidRequest_NormalizesSortAndForwardsClock()
    {
        var page = new ProductReviewsPageReadModel { ProductId = ProductId };
        var repository = new FakeRepository
        {
            GetResult = ProductReviewPageRepositoryResult.Success(page)
        };
        var service = CreateService(repository);

        var result = await service.GetAsync(
            TenantId, ProductId, 2, 20, " HIGHEST ", CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Same(page, result.Value);
        Assert.Equal(TenantId, repository.TenantId);
        Assert.Equal(ProductId, repository.ResourceId);
        Assert.Equal(2, repository.Page);
        Assert.Equal(20, repository.PageSize);
        Assert.Equal("highest", repository.Sort);
        Assert.Equal(Now, repository.Now);
    }

    [Fact]
    public async Task CreateAsync_InvalidRating_DoesNotCallRepository()
    {
        var repository = new FakeRepository();
        var service = CreateService(repository);

        var result = await service.CreateAsync(
            TenantId,
            CustomerId,
            ProductId,
            new CreateProductReviewRequest { RatingValue = 6 },
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("product_reviews.invalid_rating", result.Error.Code);
        Assert.Equal(0, repository.CallCount);
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_ForwardsJwtContextAndMapsPurchaseRequired()
    {
        var request = new CreateProductReviewRequest
        {
            RatingValue = 5,
            ReviewTitle = "Excellent",
            ReviewText = "Great product"
        };
        var repository = new FakeRepository
        {
            CreateResult = ProductReviewMutationRepositoryResult.Failure(
                "product_reviews.purchase_required")
        };
        var service = CreateService(repository);

        var result = await service.CreateAsync(
            TenantId, CustomerId, ProductId, request, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("product_reviews.purchase_required", result.Error.Code);
        Assert.Equal(TenantId, repository.TenantId);
        Assert.Equal(CustomerId, repository.CustomerId);
        Assert.Equal(ProductId, repository.ResourceId);
        Assert.Same(request, repository.CreateRequest);
        Assert.Equal(Now, repository.Now);
    }

    [Fact]
    public async Task UpdateAsync_TitleTooLong_DoesNotCallRepository()
    {
        var repository = new FakeRepository();
        var service = CreateService(repository);

        var result = await service.UpdateAsync(
            TenantId,
            CustomerId,
            Guid.NewGuid(),
            new UpdateProductReviewRequest
            {
                RatingValue = 4,
                ReviewTitle = new string('x', 151)
            },
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("product_reviews.title_too_long", result.Error.Code);
        Assert.Equal(0, repository.CallCount);
    }

    [Fact]
    public async Task DeleteAsync_RepositoryNotFound_MapsSafeError()
    {
        var repository = new FakeRepository
        {
            DeleteResult = ProductReviewDeleteRepositoryResult.Failure(
                "product_reviews.review_not_found")
        };
        var service = CreateService(repository);
        var reviewId = Guid.NewGuid();

        var result = await service.DeleteAsync(
            TenantId, CustomerId, reviewId, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("product_reviews.review_not_found", result.Error.Code);
        Assert.Equal(reviewId, repository.ResourceId);
    }

    private static ProductReviewService CreateService(FakeRepository repository) =>
        new(repository, new FakeDateTimeProvider());

    private sealed class FakeDateTimeProvider : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => Now;
    }

    private sealed class FakeRepository : IProductReviewRepository
    {
        public ProductReviewPageRepositoryResult GetResult { get; init; } =
            ProductReviewPageRepositoryResult.Success(new ProductReviewsPageReadModel());
        public ProductReviewMutationRepositoryResult CreateResult { get; init; } =
            ProductReviewMutationRepositoryResult.Success(new ProductReviewItemReadModel());
        public ProductReviewMutationRepositoryResult UpdateResult { get; init; } =
            ProductReviewMutationRepositoryResult.Success(new ProductReviewItemReadModel());
        public ProductReviewDeleteRepositoryResult DeleteResult { get; init; } =
            ProductReviewDeleteRepositoryResult.Success();
        public int CallCount { get; private set; }
        public Guid? TenantId { get; private set; }
        public Guid? CustomerId { get; private set; }
        public Guid? ResourceId { get; private set; }
        public int? Page { get; private set; }
        public int? PageSize { get; private set; }
        public string? Sort { get; private set; }
        public DateTimeOffset? Now { get; private set; }
        public CreateProductReviewRequest? CreateRequest { get; private set; }

        public Task<ProductReviewPageRepositoryResult> GetAsync(
            Guid tenantId, Guid productId, int page, int pageSize, string sort,
            DateTimeOffset now, CancellationToken cancellationToken)
        {
            Capture(tenantId, null, productId, now);
            Page = page;
            PageSize = pageSize;
            Sort = sort;
            return Task.FromResult(GetResult);
        }

        public Task<ProductReviewMutationRepositoryResult> CreateAsync(
            Guid tenantId, Guid customerId, Guid productId,
            CreateProductReviewRequest request, DateTimeOffset now,
            CancellationToken cancellationToken)
        {
            Capture(tenantId, customerId, productId, now);
            CreateRequest = request;
            return Task.FromResult(CreateResult);
        }

        public Task<ProductReviewMutationRepositoryResult> UpdateAsync(
            Guid tenantId, Guid customerId, Guid reviewId,
            UpdateProductReviewRequest request, DateTimeOffset now,
            CancellationToken cancellationToken)
        {
            Capture(tenantId, customerId, reviewId, now);
            return Task.FromResult(UpdateResult);
        }

        public Task<ProductReviewDeleteRepositoryResult> DeleteAsync(
            Guid tenantId, Guid customerId, Guid reviewId,
            DateTimeOffset now, CancellationToken cancellationToken)
        {
            Capture(tenantId, customerId, reviewId, now);
            return Task.FromResult(DeleteResult);
        }

        private void Capture(
            Guid tenantId, Guid? customerId, Guid resourceId, DateTimeOffset now)
        {
            CallCount++;
            TenantId = tenantId;
            CustomerId = customerId;
            ResourceId = resourceId;
            Now = now;
        }
    }
}
