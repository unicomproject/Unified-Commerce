using System.Reflection;
using System.Security.Claims;
using E_POS.Api.Controllers.V1.ECommerce.ProductReviews;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.ECommerce.ProductReviews.Contracts;
using E_POS.Application.Modules.ECommerce.ProductReviews.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Xunit;

namespace E_POS.ApiTests.ECommerce.ProductReviews;

public sealed class ProductReviewsControllerTests
{
    [Fact]
    public async Task GetReviews_PublicRequest_ForwardsVerifiedHeaderContextAndPaging()
    {
        var tenantId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var service = new FakeService
        {
            GetResult = ApplicationResult<ProductReviewsPageReadModel>.Success(
                new ProductReviewsPageReadModel { ProductId = productId })
        };
        var controller = CreateController(service);

        var result = await controller.GetReviews(
            tenantId, productId, 2, 15, "highest", CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal(tenantId, service.TenantId);
        Assert.Equal(productId, service.ResourceId);
        Assert.Equal(2, service.Page);
        Assert.Equal(15, service.PageSize);
        Assert.Equal("highest", service.Sort);
    }

    [Fact]
    public async Task CreateReview_AuthenticatedCustomer_UsesJwtTenantAndCustomerClaims()
    {
        var tenantId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var request = new CreateProductReviewRequest { RatingValue = 5 };
        var service = new FakeService
        {
            CreateResult = ApplicationResult<ProductReviewItemReadModel>.Success(
                new ProductReviewItemReadModel { Id = Guid.NewGuid(), ProductId = productId })
        };
        var controller = CreateController(service, tenantId, customerId);

        var result = await controller.CreateReview(
            productId, request, CancellationToken.None);

        var created = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status201Created, created.StatusCode);
        Assert.Equal(tenantId, service.TenantId);
        Assert.Equal(customerId, service.CustomerId);
        Assert.Equal(productId, service.ResourceId);
        Assert.Same(request, service.CreateRequest);
    }

    [Fact]
    public async Task CreateReview_MissingClaims_ReturnsUnauthorizedWithoutServiceCall()
    {
        var service = new FakeService();
        var controller = CreateController(service);

        var result = await controller.CreateReview(
            Guid.NewGuid(),
            new CreateProductReviewRequest { RatingValue = 4 },
            CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal(0, service.CallCount);
    }

    [Fact]
    public async Task CreateReview_DuplicateReview_ReturnsConflict()
    {
        var service = new FakeService
        {
            CreateResult = ApplicationResult<ProductReviewItemReadModel>.Failure(
                new ApplicationError(
                    "product_reviews.duplicate_review",
                    "You have already reviewed this product."))
        };
        var controller = CreateController(service, Guid.NewGuid(), Guid.NewGuid());

        var result = await controller.CreateReview(
            Guid.NewGuid(),
            new CreateProductReviewRequest { RatingValue = 4 },
            CancellationToken.None);

        Assert.IsType<ConflictObjectResult>(result);
    }

    [Fact]
    public async Task DeleteReview_Success_ReturnsNoContent()
    {
        var service = new FakeService();
        var controller = CreateController(service, Guid.NewGuid(), Guid.NewGuid());

        var result = await controller.DeleteReview(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public void Controller_ExposesPublicReadAndCustomerOnlyMutationRoutes()
    {
        var route = Assert.Single(
            typeof(ProductReviewsController).GetCustomAttributes<RouteAttribute>());
        Assert.Equal("api/v1/ecommerce/storefront", route.Template);

        var get = typeof(ProductReviewsController).GetMethod(
            nameof(ProductReviewsController.GetReviews))!;
        Assert.Single(get.GetCustomAttributes<AllowAnonymousAttribute>());
        Assert.Equal(
            "catalog/products/{productId:guid}/reviews",
            Assert.Single(get.GetCustomAttributes<HttpGetAttribute>()).Template);

        AssertCustomerOnlyRoute<HttpPostAttribute>(
            nameof(ProductReviewsController.CreateReview),
            "catalog/products/{productId:guid}/reviews");
        AssertCustomerOnlyRoute<HttpPatchAttribute>(
            nameof(ProductReviewsController.UpdateReview),
            "reviews/{reviewId:guid}");
        AssertCustomerOnlyRoute<HttpDeleteAttribute>(
            nameof(ProductReviewsController.DeleteReview),
            "reviews/{reviewId:guid}");
    }

    private static void AssertCustomerOnlyRoute<TAttribute>(string methodName, string template)
        where TAttribute : HttpMethodAttribute
    {
        var method = typeof(ProductReviewsController).GetMethod(methodName)!;
        Assert.Equal(
            "CustomerOnly",
            Assert.Single(method.GetCustomAttributes<AuthorizeAttribute>()).Policy);
        Assert.Equal(template, Assert.Single(method.GetCustomAttributes<TAttribute>()).Template);
    }

    private static ProductReviewsController CreateController(
        FakeService service,
        Guid? tenantId = null,
        Guid? customerId = null)
    {
        var context = new DefaultHttpContext();
        if (tenantId.HasValue && customerId.HasValue)
        {
            context.User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim("tenant_id", tenantId.Value.ToString()),
                new Claim("sub", customerId.Value.ToString())
            ], "Test"));
        }

        return new ProductReviewsController(service)
        {
            ControllerContext = new ControllerContext { HttpContext = context }
        };
    }

    private sealed class FakeService : IProductReviewService
    {
        public ApplicationResult<ProductReviewsPageReadModel> GetResult { get; init; } =
            ApplicationResult<ProductReviewsPageReadModel>.Success(new ProductReviewsPageReadModel());
        public ApplicationResult<ProductReviewItemReadModel> CreateResult { get; init; } =
            ApplicationResult<ProductReviewItemReadModel>.Success(new ProductReviewItemReadModel());
        public ApplicationResult<ProductReviewItemReadModel> UpdateResult { get; init; } =
            ApplicationResult<ProductReviewItemReadModel>.Success(new ProductReviewItemReadModel());
        public ApplicationResult DeleteResult { get; init; } = ApplicationResult.Success();
        public int CallCount { get; private set; }
        public Guid? TenantId { get; private set; }
        public Guid? CustomerId { get; private set; }
        public Guid? ResourceId { get; private set; }
        public int? Page { get; private set; }
        public int? PageSize { get; private set; }
        public string? Sort { get; private set; }
        public CreateProductReviewRequest? CreateRequest { get; private set; }

        public Task<ApplicationResult<ProductReviewsPageReadModel>> GetAsync(
            Guid tenantId, Guid productId, int page, int pageSize, string? sort,
            CancellationToken cancellationToken)
        {
            Capture(tenantId, null, productId);
            Page = page;
            PageSize = pageSize;
            Sort = sort;
            return Task.FromResult(GetResult);
        }

        public Task<ApplicationResult<ProductReviewItemReadModel>> CreateAsync(
            Guid tenantId, Guid customerId, Guid productId,
            CreateProductReviewRequest request, CancellationToken cancellationToken)
        {
            Capture(tenantId, customerId, productId);
            CreateRequest = request;
            return Task.FromResult(CreateResult);
        }

        public Task<ApplicationResult<ProductReviewItemReadModel>> UpdateAsync(
            Guid tenantId, Guid customerId, Guid reviewId,
            UpdateProductReviewRequest request, CancellationToken cancellationToken)
        {
            Capture(tenantId, customerId, reviewId);
            return Task.FromResult(UpdateResult);
        }

        public Task<ApplicationResult> DeleteAsync(
            Guid tenantId, Guid customerId, Guid reviewId,
            CancellationToken cancellationToken)
        {
            Capture(tenantId, customerId, reviewId);
            return Task.FromResult(DeleteResult);
        }

        private void Capture(Guid tenantId, Guid? customerId, Guid resourceId)
        {
            CallCount++;
            TenantId = tenantId;
            CustomerId = customerId;
            ResourceId = resourceId;
        }
    }
}
