using E_POS.Application.Modules.ECommerce.ProductReviews.Dtos;
using E_POS.Domain.Modules.Platform.Subscription.Constants;
using E_POS.Domain.Modules.Platform.Subscription.Entities;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using E_POS.Domain.Modules.Tenant.Orders.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Constants;
using E_POS.Infrastructure.Modules.ECommerce.ProductReviews.Repositories;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;
using CustomerEntity = E_POS.Domain.Modules.ECommerce.Customer.Entities.Customer;
using TenantEntity = E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant;

namespace E_POS.IntegrationTests.ECommerce.ProductReviews;

public sealed class ProductReviewRepositoryTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 16, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task GetAsync_ReturnsOnlyCurrentTenantApprovedReviewsWithLiveSummary()
    {
        var tenantId = Guid.NewGuid();
        var otherTenantId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var firstCustomerId = Guid.NewGuid();
        var secondCustomerId = Guid.NewGuid();
        await using var dbContext = CreateDbContext();
        await SeedStorefrontAccessAsync(dbContext, tenantId);
        dbContext.Products.Add(CreateProduct(tenantId, productId));
        dbContext.Customers.AddRange(
            CreateCustomer(tenantId, firstCustomerId, "C-001"),
            CreateCustomer(tenantId, secondCustomerId, "C-002"));
        dbContext.ProductReviews.AddRange(
            ProductReview.CreateApproved(
                tenantId, productId, firstCustomerId, 3, "Good", "Useful", Now),
            ProductReview.CreateApproved(
                tenantId, productId, secondCustomerId, 5, "Excellent", "Perfect", Now.AddMinutes(1)),
            ProductReview.CreateApproved(
                otherTenantId, productId, Guid.NewGuid(), 1, "Other", "Hidden", Now.AddMinutes(2)));
        await dbContext.SaveChangesAsync();
        var repository = new ProductReviewRepository(dbContext);

        var result = await repository.GetAsync(
            tenantId, productId, 1, 10, "highest", Now.AddMinutes(3), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Page!.TotalCount);
        Assert.Collection(
            result.Page.Items,
            first =>
            {
                Assert.Equal(5, first.RatingValue);
                Assert.Equal(ProductReviewConstants.ApprovedStatus, first.Status);
                Assert.Equal("Verified customer", first.CustomerDisplayName);
                Assert.True(first.IsVerifiedPurchase);
            },
            second => Assert.Equal(3, second.RatingValue));
        Assert.Equal(4m, result.Page.Summary.AverageRating);
        Assert.Equal(2, result.Page.Summary.TotalReviews);
        Assert.Equal(1, result.Page.Summary.FiveStarCount);
        Assert.Equal(1, result.Page.Summary.ThreeStarCount);
    }

    [Fact]
    public async Task CreateAsync_WithoutCompletedFulfilledPurchase_IsRejected()
    {
        var tenantId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        await using var dbContext = CreateDbContext();
        await SeedStorefrontAccessAsync(dbContext, tenantId);
        dbContext.Customers.Add(CreateCustomer(tenantId, customerId));
        dbContext.Products.Add(CreateProduct(tenantId, productId));
        await dbContext.SaveChangesAsync();
        var repository = new ProductReviewRepository(dbContext);

        var result = await repository.CreateAsync(
            tenantId,
            customerId,
            productId,
            new CreateProductReviewRequest { RatingValue = 5 },
            Now,
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("product_reviews.purchase_required", result.ErrorCode);
        Assert.Empty(dbContext.ProductReviews);
    }

    [Fact]
    public async Task CreateAsync_VerifiedPurchase_IsImmediatelyVisibleAndDuplicateSafe()
    {
        var tenantId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        await using var dbContext = CreateDbContext();
        await SeedStorefrontAccessAsync(dbContext, tenantId);
        dbContext.Customers.Add(CreateCustomer(tenantId, customerId));
        dbContext.Products.Add(CreateProduct(tenantId, productId));
        AddCompletedPurchase(dbContext, tenantId, customerId, productId);
        await dbContext.SaveChangesAsync();
        var repository = new ProductReviewRepository(dbContext);
        var request = new CreateProductReviewRequest
        {
            RatingValue = 5,
            ReviewTitle = " Great jersey ",
            ReviewText = " Fits well "
        };

        var created = await repository.CreateAsync(
            tenantId, customerId, productId, request, Now.AddMinutes(1), CancellationToken.None);
        var duplicate = await repository.CreateAsync(
            tenantId, customerId, productId, request, Now.AddMinutes(2), CancellationToken.None);
        var visible = await repository.GetAsync(
            tenantId, productId, 1, 10, "newest", Now.AddMinutes(3), CancellationToken.None);

        Assert.True(created.IsSuccess);
        Assert.Equal(ProductReviewConstants.ApprovedStatus, created.Review!.Status);
        Assert.Equal("Great jersey", created.Review.ReviewTitle);
        Assert.Equal("Fits well", created.Review.ReviewText);
        Assert.Equal("product_reviews.duplicate_review", duplicate.ErrorCode);
        var item = Assert.Single(visible.Page!.Items);
        Assert.Equal(created.Review.Id, item.Id);
        Assert.Equal(5m, visible.Page.Summary.AverageRating);
        Assert.Equal(1, visible.Page.Summary.TotalReviews);
        Assert.Single(dbContext.ProductReviews);
        Assert.Single(dbContext.ProductRatingSummaries);
    }

    [Fact]
    public async Task UpdateAndDelete_EnforceOwnerAndRebuildSummary()
    {
        var tenantId = Guid.NewGuid();
        var firstCustomerId = Guid.NewGuid();
        var secondCustomerId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        await using var dbContext = CreateDbContext();
        await SeedStorefrontAccessAsync(dbContext, tenantId);
        dbContext.Customers.AddRange(
            CreateCustomer(tenantId, firstCustomerId, "C-001"),
            CreateCustomer(tenantId, secondCustomerId, "C-002"));
        dbContext.Products.Add(CreateProduct(tenantId, productId));
        var firstReview = ProductReview.CreateApproved(
            tenantId, productId, firstCustomerId, 5, null, null, Now);
        var secondReview = ProductReview.CreateApproved(
            tenantId, productId, secondCustomerId, 1, null, null, Now);
        dbContext.ProductReviews.AddRange(firstReview, secondReview);
        var summary = ProductRatingSummary.Create(tenantId, productId, Now);
        summary.Rebuild([5, 1], Now);
        dbContext.ProductRatingSummaries.Add(summary);
        await dbContext.SaveChangesAsync();
        var repository = new ProductReviewRepository(dbContext);

        var denied = await repository.UpdateAsync(
            tenantId,
            secondCustomerId,
            firstReview.Id,
            new UpdateProductReviewRequest { RatingValue = 2 },
            Now.AddMinutes(1),
            CancellationToken.None);
        var updated = await repository.UpdateAsync(
            tenantId,
            firstCustomerId,
            firstReview.Id,
            new UpdateProductReviewRequest { RatingValue = 3, ReviewText = "Updated" },
            Now.AddMinutes(2),
            CancellationToken.None);
        var deleted = await repository.DeleteAsync(
            tenantId, firstCustomerId, firstReview.Id, Now.AddMinutes(3), CancellationToken.None);

        Assert.Equal("product_reviews.review_not_found", denied.ErrorCode);
        Assert.True(updated.IsSuccess);
        Assert.Equal(3, updated.Review!.RatingValue);
        Assert.True(deleted.IsSuccess);
        Assert.Equal(ProductReviewConstants.DeletedStatus, firstReview.Status);
        Assert.Equal(1, summary.TotalReviews);
        Assert.Equal(1m, summary.AverageRating);
        Assert.Equal(1, summary.OneStarCount);
    }

    [Fact]
    public async Task GetAsync_OnlineStoreFeatureDisabled_IsForbiddenAtRepositoryBoundary()
    {
        var tenantId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        await using var dbContext = CreateDbContext();
        dbContext.Tenants.Add(CreateTenant(tenantId));
        dbContext.Products.Add(CreateProduct(tenantId, productId));
        await dbContext.SaveChangesAsync();
        var repository = new ProductReviewRepository(dbContext);

        var result = await repository.GetAsync(
            tenantId, productId, 1, 10, "newest", Now, CancellationToken.None);

        Assert.Equal("product_reviews.feature_disabled", result.ErrorCode);
    }

    [Fact]
    public async Task CreateAsync_ProductFromAnotherTenant_IsNotFound()
    {
        var tenantId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var otherTenantProductId = Guid.NewGuid();
        await using var dbContext = CreateDbContext();
        await SeedStorefrontAccessAsync(dbContext, tenantId);
        dbContext.Customers.Add(CreateCustomer(tenantId, customerId));
        dbContext.Products.Add(CreateProduct(Guid.NewGuid(), otherTenantProductId));
        await dbContext.SaveChangesAsync();
        var repository = new ProductReviewRepository(dbContext);

        var result = await repository.CreateAsync(
            tenantId,
            customerId,
            otherTenantProductId,
            new CreateProductReviewRequest { RatingValue = 4 },
            Now,
            CancellationToken.None);

        Assert.Equal("product_reviews.product_not_found", result.ErrorCode);
        Assert.Empty(dbContext.ProductReviews);
    }

    private static async Task SeedStorefrontAccessAsync(EPosDbContext dbContext, Guid tenantId)
    {
        var featureId = Guid.NewGuid();
        dbContext.Tenants.Add(CreateTenant(tenantId));
        dbContext.PlatformFeatures.Add(PlatformFeature.Create(
            featureId,
            Guid.NewGuid(),
            PlatformTenantFeatureCodes.OnlineStore,
            "Online Store",
            SubscriptionCatalogConstants.RecordStatus.Active,
            Now));
        dbContext.TenantFeatureEntitlements.Add(TenantFeatureEntitlement.Create(
            Guid.NewGuid(),
            tenantId,
            featureId,
            TenantEntitlementStatusConstants.Enabled,
            Now));
        await dbContext.SaveChangesAsync();
    }

    private static TenantEntity CreateTenant(Guid tenantId) => TenantEntity.Create(
        tenantId,
        $"T-{tenantId:N}",
        $"tenant-{tenantId:N}",
        "Test Tenant",
        TenantStatusConstants.Active,
        "GBP",
        "Europe/London",
        null,
        null,
        Now);

    private static CustomerEntity CreateCustomer(
        Guid tenantId,
        Guid customerId,
        string customerCode = "C-001") => CustomerEntity.CreatePosCustomer(
        customerId,
        tenantId,
        customerCode,
        "Test Customer",
        $"+94{customerId.ToString("N")[..9]}",
        $"{customerId:N}@example.com",
        Guid.NewGuid(),
        Now);

    private static Product CreateProduct(Guid tenantId, Guid productId) => Product.Create(
        productId,
        tenantId,
        $"P-{productId:N}",
        "Home Jersey",
        $"home-jersey-{productId:N}",
        "STANDARD",
        "SIMPLE",
        null,
        null,
        null,
        null,
        null,
        true,
        true,
        ProductConstants.ActiveStatus,
        null,
        Now);

    private static void AddCompletedPurchase(
        EPosDbContext dbContext,
        Guid tenantId,
        Guid customerId,
        Guid productId)
    {
        var orderId = Guid.NewGuid();
        dbContext.SalesOrders.Add(SalesOrder.CreateCompletedPosSale(
            orderId,
            tenantId,
            $"ORD-{orderId:N}",
            Guid.NewGuid(),
            customerId,
            "Test Customer",
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            "GBP",
            false,
            74.99m,
            0m,
            0m,
            74.99m,
            74.99m,
            null,
            Now));
        dbContext.SalesOrderLines.Add(SalesOrderLine.CreateForPosSale(
            Guid.NewGuid(),
            tenantId,
            orderId,
            1,
            productId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            "SKU-1",
            "Home Jersey",
            null,
            "EA",
            "Each",
            "STANDARD",
            "SIMPLE",
            1m,
            74.99m,
            74.99m,
            0m,
            0m,
            false,
            Now));
    }

    private static EPosDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<EPosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new EPosDbContext(options);
    }
}

