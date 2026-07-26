using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.CatalogProduct.Contracts;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos;
using E_POS.Application.Modules.Tenant.TenantFoundation.Contracts;
using E_POS.Application.Modules.Tenant.PricingTax.Contracts;
using E_POS.Application.Modules.Tenant.PricingTax.Dtos;
using E_POS.Application.Modules.Tenant.PricingTax.Services;
using E_POS.Application.Modules.Tenant.PricingTax.Validators;
using E_POS.Domain.Modules.Tenant.PricingTax.Constants;
using E_POS.Domain.Modules.Tenant.PricingTax.Entities;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using E_POS.Domain.Modules.ECommerce.FulfilmentPickup.Entities;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Dtos;
using Xunit;

namespace E_POS.UnitTests.PricingTax;

public sealed class PriceListItemsServiceTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 7, 5, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task CreateAsync_WithoutPermission_ReturnsPermissionDenied()
    {
        var service = CreateService();

        var result = await service.CreateAsync(
            CreateContext([]),
            CreateValidRequest(),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("pricing.price_list_item.permission_denied", result.Error.Code);
    }

    [Fact]
    public async Task CreateAsync_WithPermission_Succeeds()
    {
        var repository = new FakePriceListItemsRepository();
        var service = CreateService(repository);

        var result = await service.CreateAsync(
            CreateContext([PricingTaxConstants.UpdatePermission]),
            CreateValidRequest(),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(repository.AddedItem);
        Assert.Equal(10.50m, repository.AddedItem.SellingPrice);
        Assert.Equal(15.00m, repository.AddedItem.CompareAtPrice);
        Assert.Equal(1m, repository.AddedItem.MinQuantity);
    }

    [Fact]
    public async Task CreateAsync_WithInvalidPriceList_ReturnsError()
    {
        var priceListRepository = new FakePriceListRepository { PriceListExists = false };
        var service = CreateService(priceListRepository: priceListRepository);

        var result = await service.CreateAsync(
            CreateContext([PricingTaxConstants.UpdatePermission]),
            CreateValidRequest(),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("pricing.price_list_item.invalid_price_list", result.Error.Code);
    }

    [Fact]
    public async Task CreateAsync_WithInvalidProduct_ReturnsError()
    {
        var productRepository = new FakeProductRepository { ProductExists = false };
        var service = CreateService(productRepository: productRepository);

        var result = await service.CreateAsync(
            CreateContext([PricingTaxConstants.UpdatePermission]),
            CreateValidRequest(),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("pricing.price_list_item.invalid_product", result.Error.Code);
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateEntry_ReturnsConflict()
    {
        var repository = new FakePriceListItemsRepository { ItemExists = true };
        var service = CreateService(repository);

        var result = await service.CreateAsync(
            CreateContext([PricingTaxConstants.UpdatePermission]),
            CreateValidRequest(),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("pricing.price_list_item.duplicate_entry", result.Error.Code);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesProperties()
    {
        var repository = new FakePriceListItemsRepository();
        var service = CreateService(repository);

        var itemId = Guid.NewGuid();
        var item = PriceListItem.Create(
            itemId, TenantId, Guid.NewGuid(), Guid.NewGuid(), null, null, 5m, null, 1m, null, null, "ACTIVE", UserId, Now);
        repository.EditableItem = item;

        var updateRequest = new PriceListItemUpdateRequest(25m, 30m, 5m, null, null, "INACTIVE");

        var result = await service.UpdateAsync(
            CreateContext([PricingTaxConstants.UpdatePermission]),
            itemId,
            updateRequest,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(25m, item.SellingPrice);
        Assert.Equal(30m, item.CompareAtPrice);
        Assert.Equal(5m, item.MinQuantity);
        Assert.Equal("INACTIVE", item.Status);
    }

    private static TenantRequestContext CreateContext(IReadOnlyCollection<string> permissions)
    {
        return new TenantRequestContext(TenantId, UserId, permissions);
    }

    private static PriceListItemCreateRequest CreateValidRequest()
    {
        return new PriceListItemCreateRequest(
            PriceListId: Guid.NewGuid(),
            ProductId: Guid.NewGuid(),
            ProductVariantId: null,
            UomId: null,
            SellingPrice: 10.50m,
            CompareAtPrice: 15.00m,
            MinQuantity: 1m,
            ValidFrom: null,
            ValidUntil: null,
            Status: "ACTIVE");
    }

    private static PriceListItemsService CreateService(
        IPriceListItemsRepository? repository = null,
        IPriceListRepository? priceListRepository = null,
        IProductRepository? productRepository = null,
        IUnitOfMeasureRepository? uomRepository = null)
    {
        return new PriceListItemsService(
            repository ?? new FakePriceListItemsRepository(),
            priceListRepository ?? new FakePriceListRepository(),
            productRepository ?? new FakeProductRepository(),
            uomRepository ?? new FakeUnitOfMeasureRepository(),
            new PriceListItemsRequestValidator(),
            new FakeDateTimeProvider());
    }

    private sealed class FakeDateTimeProvider : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => Now;
    }

    private sealed class FakePriceListRepository : IPriceListRepository
    {
        public bool PriceListExists { get; set; } = true;
        public PriceList? GetEditable { get; set; }

        public Task<bool> PriceListCodeExistsAsync(Guid tenantId, string code, Guid? excludePriceListId, CancellationToken cancellationToken) => Task.FromResult(false);
        public Task<PriceList?> GetEditableAsync(Guid tenantId, Guid id, CancellationToken cancellationToken)
        {
            if (!PriceListExists) return Task.FromResult<PriceList?>(null);
            return Task.FromResult<PriceList?>(PriceList.Create(id, tenantId, "PL-1", "Default", "POS", "USD", false, true, 0, null, null, "ACTIVE", null, Now));
        }
        public Task<PriceListResponse?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken)
        {
            if (!PriceListExists) return Task.FromResult<PriceListResponse?>(null);
            return Task.FromResult<PriceListResponse?>(new PriceListResponse(id, "PL-1", "Default", "POS", "USD", false, true, 0, null, null, "ACTIVE", [], [], Now, Now));
        }
        public Task<PriceListListResponse> ListAsync(Guid tenantId, int pageNumber, int pageSize, string? search, CancellationToken cancellationToken) => Task.FromResult(new PriceListListResponse([], pageNumber, pageSize, 0));
        public Task AddAsync(PriceList priceList, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task AddOutletAssignmentsAsync(IEnumerable<PriceListOutlet> assignments, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task AddChannelAssignmentsAsync(IEnumerable<PriceListChannel> assignments, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task ClearAssignmentsAsync(Guid priceListId, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task ClearOtherDefaultsAsync(Guid tenantId, Guid defaultPriceListId, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeProductRepository : IProductRepository
    {
        public bool ProductExists { get; set; } = true;
        public bool VariantExists { get; set; } = true;

        public Task<bool> ProductExistsAsync(Guid tenantId, Guid productId, CancellationToken cancellationToken) => Task.FromResult(ProductExists);
        public Task<bool> ProductVariantExistsAsync(Guid tenantId, Guid productId, Guid variantId, CancellationToken cancellationToken) => Task.FromResult(VariantExists);
        
        public Task<bool> ProductCodeExistsAsync(Guid tenantId, string productCode, Guid? excludeProductId, CancellationToken cancellationToken) => Task.FromResult(false);
        public Task<bool> SkuExistsAsync(Guid tenantId, string sku, Guid? excludeProductId, CancellationToken cancellationToken) => Task.FromResult(false);
        public Task<bool> BarcodeExistsAsync(Guid tenantId, string barcodeValue, Guid? excludeProductId, CancellationToken cancellationToken) => Task.FromResult(false);
        public Task<ProductListResponse> ListAsync(Guid tenantId, int pageNumber, int pageSize, string? search, CancellationToken cancellationToken) => Task.FromResult(new ProductListResponse([], pageNumber, pageSize, 0));
        public Task<ProductResponse?> GetByIdAsync(Guid tenantId, Guid productId, bool includeDeleted, CancellationToken cancellationToken) => Task.FromResult<ProductResponse?>(null);
        public Task<Product?> GetEditableAsync(Guid tenantId, Guid productId, CancellationToken cancellationToken) => Task.FromResult<Product?>(null);
        public Task AddAsync(Product product, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task AddVariantAsync(ProductVariant variant, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task AddBarcodeAsync(ProductBarcode barcode, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task AddCategoryLinksAsync(IEnumerable<ProductCategory> links, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task AddCollectionLinksAsync(IEnumerable<ProductCollection> links, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task AddImagesAsync(IEnumerable<ProductImage> images, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task AddMediaAssetsAsync(IEnumerable<E_POS.Domain.Modules.Shared.Media.Entities.MediaAsset> mediaAssets, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task AddChannelVisibilitiesAsync(IEnumerable<ProductChannelVisibility> visibilities, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task AddPriceListItemAsync(PriceListItem priceListItem, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<IReadOnlyList<Guid>> GetProductImageMediaAssetIdsAsync(Guid tenantId, Guid productId, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<Guid>>(Array.Empty<Guid>());
        public Task ClearProductMappingsAsync(Guid tenantId, Guid productId, bool clearImages, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task MarkMediaAssetsInactiveAsync(Guid tenantId, IReadOnlyCollection<Guid> mediaAssetIds, Guid? updatedByTenantUserId, DateTimeOffset now, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<Guid?> GetDefaultPriceListIdAsync(Guid tenantId, CancellationToken cancellationToken) => Task.FromResult<Guid?>(null);
        public Task<ProductVariant?> GetDefaultVariantAsync(Guid productId, CancellationToken cancellationToken) => Task.FromResult<ProductVariant?>(null);
        public Task<PriceListItem?> GetPriceListItemAsync(Guid priceListId, Guid variantId, CancellationToken cancellationToken) => Task.FromResult<PriceListItem?>(null);
        public Task<ProductBarcode?> GetBarcodeAsync(Guid variantId, CancellationToken cancellationToken) => Task.FromResult<ProductBarcode?>(null);
    }

    private sealed class FakeUnitOfMeasureRepository : IUnitOfMeasureRepository
    {
        public bool UomExists { get; set; } = true;

        public Task<IReadOnlyList<UnitOfMeasureResponse>> ListAsync(Guid tenantId, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<UnitOfMeasureResponse>>([]);
        public Task<bool> UomExistsAsync(Guid? tenantId, Guid uomId, CancellationToken cancellationToken) => Task.FromResult(UomExists);
    }

    private sealed class FakePriceListItemsRepository : IPriceListItemsRepository
    {
        public bool ItemExists { get; set; }
        public PriceListItem? AddedItem { get; private set; }
        public PriceListItem? EditableItem { get; set; }

        public Task<bool> ItemExistsAsync(Guid tenantId, Guid priceListId, Guid productId, Guid? variantId, Guid? uomId, decimal minQuantity, Guid? excludeItemId, CancellationToken cancellationToken)
        {
            return Task.FromResult(ItemExists);
        }

        public Task<PriceListItem?> GetEditableAsync(Guid tenantId, Guid id, CancellationToken cancellationToken)
        {
            return Task.FromResult(EditableItem);
        }

        public Task<PriceListItemResponse?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken)
        {
            if (AddedItem != null)
            {
                return Task.FromResult<PriceListItemResponse?>(new PriceListItemResponse(
                    AddedItem.Id, AddedItem.PriceListId, AddedItem.ProductId, AddedItem.ProductVariantId, AddedItem.UomId,
                    AddedItem.SellingPrice, AddedItem.CompareAtPrice, AddedItem.MinQuantity, AddedItem.ValidFrom, AddedItem.ValidUntil, AddedItem.Status, Now, Now));
            }
            if (EditableItem != null)
            {
                return Task.FromResult<PriceListItemResponse?>(new PriceListItemResponse(
                    EditableItem.Id, EditableItem.PriceListId, EditableItem.ProductId, EditableItem.ProductVariantId, EditableItem.UomId,
                    EditableItem.SellingPrice, EditableItem.CompareAtPrice, EditableItem.MinQuantity, EditableItem.ValidFrom, EditableItem.ValidUntil, EditableItem.Status, Now, Now));
            }
            return Task.FromResult<PriceListItemResponse?>(null);
        }

        public Task<PriceListItemListResponse> ListAsync(Guid tenantId, Guid priceListId, int pageNumber, int pageSize, CancellationToken cancellationToken)
        {
            return Task.FromResult(new PriceListItemListResponse([], pageNumber, pageSize, 0));
        }

        public Task AddAsync(PriceListItem priceListItem, CancellationToken cancellationToken)
        {
            AddedItem = priceListItem;
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}


