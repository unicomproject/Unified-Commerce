using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.CatalogProduct.Contracts;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos;
using E_POS.Application.Modules.Tenant.CatalogProduct.Services;
using E_POS.Application.Modules.Tenant.CatalogProduct.Validators;
using E_POS.Domain.Modules.Shared.Media.Entities;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using E_POS.Domain.Modules.Tenant.PricingTax.Entities;
using Xunit;

namespace E_POS.UnitTests.CatalogProduct;

public sealed class ProductServiceTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 7, 4, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task CreateAsync_WithoutCreatePermission_ReturnsPermissionDenied()
    {
        var service = new ProductService(new FakeProductRepository(), new ProductRequestValidator(), new FakeDateTimeProvider());

        var result = await service.CreateAsync(
            CreateContext([]),
            new ProductCreateRequest(
                Name: "Product 1", 
                Description: null, 
                ShortDescription: null, 
                LongDescription: null, 
                Status: "ACTIVE", 
                ProductCode: "P-1", 
                ProductType: null, 
                ProductStructure: null, 
                BusinessTypeId: null, 
                BrandId: null, 
                ReturnPolicyId: null, 
                IsSellable: null, 
                IsTaxable: null, 
                Sku: "SKU-1", 
                StockUomId: null, 
                SalesUomId: null, 
                Barcode: null, 
                Price: null, 
                CategoryIds: null, 
                CollectionIds: null, 
                ImageUrls: null, 
                SalesChannelIds: null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("product.permission_denied", result.Error.Code);
    }

    [Fact]
    public async Task CreateAsync_WithCreatePermission_NormalizesCodeAndPersists()
    {
        var repository = new FakeProductRepository();
        var service = new ProductService(repository, new ProductRequestValidator(), new FakeDateTimeProvider());

        var result = await service.CreateAsync(
            CreateContext([ProductConstants.CreatePermission]),
            new ProductCreateRequest(
                Name: "Product 1", 
                Description: null, 
                ShortDescription: null, 
                LongDescription: null, 
                Status: "ACTIVE", 
                ProductCode: " p-1 ", 
                ProductType: null, 
                ProductStructure: null, 
                BusinessTypeId: null, 
                BrandId: null, 
                ReturnPolicyId: null, 
                IsSellable: null, 
                IsTaxable: null, 
                Sku: "SKU-1", 
                StockUomId: null, 
                SalesUomId: null, 
                Barcode: null, 
                Price: null, 
                CategoryIds: null, 
                CollectionIds: null, 
                ImageUrls: null, 
                SalesChannelIds: null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("P-1", repository.AddedProduct?.ProductCode);
        Assert.Equal("SKU-1", repository.AddedVariant?.Sku);
    }

    [Fact]
    public async Task CreateAsync_WithLegacyImageUrls_CreatesMediaAssetsAndLinksProductImages()
    {
        var repository = new FakeProductRepository();
        var service = new ProductService(repository, new ProductRequestValidator(), new FakeDateTimeProvider());

        var result = await service.CreateAsync(
            CreateContext([ProductConstants.CreatePermission]),
            new ProductCreateRequest(
                Name: "Product 1", 
                Description: null, 
                ShortDescription: null, 
                LongDescription: null, 
                Status: "ACTIVE", 
                ProductCode: "P-1", 
                ProductType: null, 
                ProductStructure: null, 
                BusinessTypeId: null, 
                BrandId: null, 
                ReturnPolicyId: null, 
                IsSellable: null, 
                IsTaxable: null, 
                Sku: "SKU-1", 
                StockUomId: null, 
                SalesUomId: null, 
                Barcode: null, 
                Price: null, 
                CategoryIds: null, 
                CollectionIds: null, 
                ImageUrls: ["https://cdn.example.test/product.png"], 
                SalesChannelIds: null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var mediaAsset = Assert.Single(repository.AddedMediaAssets);
        var productImage = Assert.Single(repository.AddedImages);
        Assert.Equal(mediaAsset.Id, productImage.MediaAssetId);
        Assert.Equal("https://cdn.example.test/product.png", mediaAsset.PublicUrl);
        Assert.Equal("https://cdn.example.test/product.png", productImage.ImageUrl);
    }
    [Fact]
    public async Task DeleteAsync_WithLinkedMediaAssets_MarksMediaAssetsInactive()
    {
        var repository = new FakeProductRepository();
        var service = new ProductService(repository, new ProductRequestValidator(), new FakeDateTimeProvider());

        var createResult = await service.CreateAsync(
            CreateContext([ProductConstants.CreatePermission]),
            new ProductCreateRequest(
                Name: "Product 1", 
                Description: null, 
                ShortDescription: null, 
                LongDescription: null, 
                Status: "ACTIVE", 
                ProductCode: "P-1", 
                ProductType: null, 
                ProductStructure: null, 
                BusinessTypeId: null, 
                BrandId: null, 
                ReturnPolicyId: null, 
                IsSellable: null, 
                IsTaxable: null, 
                Sku: "SKU-1", 
                StockUomId: null, 
                SalesUomId: null, 
                Barcode: null, 
                Price: null, 
                CategoryIds: null, 
                CollectionIds: null, 
                ImageUrls: ["https://cdn.example.test/product.png"], 
                SalesChannelIds: null),
            CancellationToken.None);

        Assert.True(createResult.IsSuccess);

        var result = await service.DeleteAsync(
            CreateContext([ProductConstants.DeletePermission]),
            repository.AddedProduct!.Id,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var mediaAssetId = Assert.Single(repository.AddedMediaAssets).Id;
        Assert.Equal([mediaAssetId], repository.InactivatedMediaAssetIds);
        Assert.Equal(ProductConstants.DeletedStatus, repository.AddedProduct.Status);
    }
    private static TenantRequestContext CreateContext(IReadOnlyCollection<string> permissions)
    {
        return new TenantRequestContext(TenantId, UserId, permissions);
    }

    private sealed class FakeDateTimeProvider : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => Now;
    }

    private sealed class FakeProductRepository : IProductRepository
    {
        public Product? AddedProduct { get; private set; }
        public ProductVariant? AddedVariant { get; private set; }
        public List<ProductImage> AddedImages { get; } = [];
        public List<MediaAsset> AddedMediaAssets { get; } = [];
        public List<Guid> InactivatedMediaAssetIds { get; } = [];

        public Task<bool> ProductCodeExistsAsync(Guid tenantId, string productCode, Guid? excludeProductId, CancellationToken cancellationToken)
        {
            return Task.FromResult(false);
        }

        public Task<bool> SkuExistsAsync(Guid tenantId, string sku, Guid? excludeProductId, CancellationToken cancellationToken)
        {
            return Task.FromResult(false);
        }

        public Task<bool> BarcodeExistsAsync(Guid tenantId, string barcodeValue, Guid? excludeProductId, CancellationToken cancellationToken)
        {
            return Task.FromResult(false);
        }

        public Task<ProductListResponse> ListAsync(Guid tenantId, int pageNumber, int pageSize, string? search, CancellationToken cancellationToken)
        {
            return Task.FromResult(new ProductListResponse([], pageNumber, pageSize, 0));
        }

        public Task<ProductResponse?> GetByIdAsync(Guid tenantId, Guid productId, bool includeDeleted, CancellationToken cancellationToken)
        {
            return Task.FromResult<ProductResponse?>(new ProductResponse(
                productId,
                AddedProduct?.ProductCode ?? "",
                AddedProduct?.ProductName ?? "",
                AddedProduct?.ShortDescription,
                AddedProduct?.Status ?? "",
                AddedVariant?.Sku ?? "",
                null,
                null,
                [],
                [],
                [],
                [],
                Now,
                Now));
        }

        public Task<Product?> GetEditableAsync(Guid tenantId, Guid productId, CancellationToken cancellationToken)
        {
            return Task.FromResult(AddedProduct);
        }

        public Task AddAsync(Product product, CancellationToken cancellationToken)
        {
            AddedProduct = product;
            return Task.CompletedTask;
        }

        public Task AddVariantAsync(ProductVariant variant, CancellationToken cancellationToken)
        {
            AddedVariant = variant;
            return Task.CompletedTask;
        }

        public Task AddBarcodeAsync(ProductBarcode barcode, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task AddCategoryLinksAsync(IEnumerable<ProductCategory> links, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task AddCollectionLinksAsync(IEnumerable<ProductCollection> links, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task AddImagesAsync(IEnumerable<ProductImage> images, CancellationToken cancellationToken)
        {
            AddedImages.AddRange(images);
            return Task.CompletedTask;
        }

        public Task AddMediaAssetsAsync(IEnumerable<MediaAsset> mediaAssets, CancellationToken cancellationToken)
        {
            AddedMediaAssets.AddRange(mediaAssets);
            return Task.CompletedTask;
        }

        public Task AddChannelVisibilitiesAsync(IEnumerable<ProductChannelVisibility> visibilities, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task AddPriceListItemAsync(PriceListItem priceListItem, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<Guid>> GetProductImageMediaAssetIdsAsync(Guid tenantId, Guid productId, CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<Guid>>(AddedImages.Where(x => x.MediaAssetId.HasValue).Select(x => x.MediaAssetId!.Value).ToList());
        }

        public Task ClearProductMappingsAsync(Guid tenantId, Guid productId, bool clearImages, CancellationToken cancellationToken)
        {
            if (clearImages)
            {
                AddedImages.Clear();
            }

            return Task.CompletedTask;
        }

        public Task MarkMediaAssetsInactiveAsync(Guid tenantId, IReadOnlyCollection<Guid> mediaAssetIds, Guid? updatedByTenantUserId, DateTimeOffset now, CancellationToken cancellationToken)
        {
            InactivatedMediaAssetIds.AddRange(mediaAssetIds);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task<Guid?> GetDefaultPriceListIdAsync(Guid tenantId, CancellationToken cancellationToken)
        {
            return Task.FromResult<Guid?>(Guid.NewGuid());
        }

        public Task<ProductVariant?> GetDefaultVariantAsync(Guid productId, CancellationToken cancellationToken)
        {
            return Task.FromResult(AddedVariant);
        }

        public Task<PriceListItem?> GetPriceListItemAsync(Guid priceListId, Guid variantId, CancellationToken cancellationToken)
        {
            return Task.FromResult<PriceListItem?>(null);
        }

        public Task<ProductBarcode?> GetBarcodeAsync(Guid variantId, CancellationToken cancellationToken)
        {
            return Task.FromResult<ProductBarcode?>(null);
        }

        public Task<bool> ProductExistsAsync(Guid tenantId, Guid productId, CancellationToken cancellationToken) => Task.FromResult(true);
        public Task<bool> ProductVariantExistsAsync(Guid tenantId, Guid productId, Guid variantId, CancellationToken cancellationToken) => Task.FromResult(true);
    }
}


