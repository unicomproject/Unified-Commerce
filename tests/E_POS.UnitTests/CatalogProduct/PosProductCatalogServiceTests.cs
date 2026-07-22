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

    [Fact]
    public async Task GetProductByBarcode_TrimsOnlyAndCallsRepository()
    {
        var repository = new FakePosProductCatalogRepository
        {
            BarcodeResult = new PosBarcodeProductRepositoryResult(
                null,
                new PosBarcodeProductResponseDto(
                    Guid.NewGuid(), Guid.NewGuid(), "00200000000114", "EAN13", "Cap", "Blue",
                    "CAP-BLU", 1m, 2500, 10m, "in_stock")),
        };
        var service = new PosProductCatalogService(repository);
        var context = new TenantRequestContext(
            Guid.NewGuid(), Guid.NewGuid(),
            new[] { ProductPosPermissions.View, ProductPosPermissions.Search });

        var result = await service.GetProductByBarcodeAsync(
            context, Guid.NewGuid(), "  00200000000114\r\n", CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("00200000000114", repository.Barcode);
    }

    [Fact]
    public async Task GetProductByBarcode_EmptyBarcode_DoesNotCallRepository()
    {
        var repository = new FakePosProductCatalogRepository();
        var service = new PosProductCatalogService(repository);
        var context = new TenantRequestContext(
            Guid.NewGuid(), Guid.NewGuid(),
            new[] { ProductPosPermissions.View, ProductPosPermissions.Search });

        var result = await service.GetProductByBarcodeAsync(
            context, Guid.NewGuid(), " \r\n ", CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("pos_barcode.invalid", result.Error.Code);
        Assert.Null(repository.Barcode);
    }

    private sealed class FakePosProductCatalogRepository : IPosProductCatalogRepository
    {
        public int ListProductsCallCount { get; private set; }

        public PosProductCatalogRepositoryResult ListProductsResult { get; init; } =
            new("pos_products.list_failed", Array.Empty<PosProductSummaryResponseDto>());

        public PosBarcodeProductRepositoryResult BarcodeResult { get; init; } =
            new("pos_barcode.not_found", null);
        public string? Barcode { get; private set; }

        public Task<PosProductCatalogRepositoryResult> ListProductsAsync(
            Guid tenantId,
            Guid deviceId,
            Guid? categoryId,
            string? search,
            CancellationToken cancellationToken,
            Guid? outletId = null)
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

        public Task<PosBarcodeProductRepositoryResult> GetProductByBarcodeAsync(
            Guid tenantId,
            Guid deviceId,
            string barcode,
            CancellationToken cancellationToken)
        {
            Barcode = barcode;
            return Task.FromResult(BarcodeResult);
        }
    }
}
