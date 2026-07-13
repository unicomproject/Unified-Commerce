using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.CatalogProduct.Contracts;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos;
using E_POS.Application.Modules.Tenant.CatalogProduct.Services;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;
using Xunit;

namespace E_POS.UnitTests.CatalogProduct;

public sealed class PosProductCatalogServiceTests
{
    [Fact]
    public async Task ListProducts_WithSearchQueryAndMissingProductsSearch_ReturnsPermissionDenied()
    {
        var repository = new FakePosProductCatalogRepository();
        var service = new PosProductCatalogService(repository);
        var context = new TenantRequestContext(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new[] { ProductPosPermissions.View });

        var result = await service.ListProductsAsync(
            context,
            Guid.NewGuid(),
            null,
            "coffee",
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("pos_products.permission_denied", result.Error.Code);
        Assert.Equal(0, repository.ListProductsCallCount);
    }

    [Fact]
    public async Task ListProducts_WithSearchQueryAndProductsSearch_CallsRepository()
    {
        var repository = new FakePosProductCatalogRepository
        {
            ListProductsResult = new PosProductCatalogRepositoryResult(null, Array.Empty<PosProductSummaryResponseDto>())
        };
        var service = new PosProductCatalogService(repository);
        var context = new TenantRequestContext(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new[] { ProductPosPermissions.View, ProductPosPermissions.Search });

        var result = await service.ListProductsAsync(
            context,
            Guid.NewGuid(),
            null,
            "coffee",
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, repository.ListProductsCallCount);
    }

    private sealed class FakePosProductCatalogRepository : IPosProductCatalogRepository
    {
        public int ListProductsCallCount { get; private set; }

        public PosProductCatalogRepositoryResult ListProductsResult { get; init; } =
            new("pos_products.list_failed", Array.Empty<PosProductSummaryResponseDto>());

        public Task<PosProductCatalogRepositoryResult> ListProductsAsync(
            Guid tenantId,
            Guid deviceId,
            Guid? categoryId,
            string? search,
            CancellationToken cancellationToken)
        {
            ListProductsCallCount++;
            return Task.FromResult(ListProductsResult);
        }

        public Task<PosProductCatalogCategoriesRepositoryResult> ListCategoriesAsync(
            Guid tenantId,
            Guid deviceId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new PosProductCatalogCategoriesRepositoryResult(
                "pos_catalog.categories_failed",
                Array.Empty<PosCatalogCategoryResponseDto>()));
        }

        public Task<PosProductDetailRepositoryResult> GetProductDetailAsync(
            Guid tenantId,
            Guid deviceId,
            Guid productId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new PosProductDetailRepositoryResult(
                "pos_products.product_not_found",
                null));
        }
    }
}
