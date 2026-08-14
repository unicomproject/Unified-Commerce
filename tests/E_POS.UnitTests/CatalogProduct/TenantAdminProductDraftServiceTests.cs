using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Platform.Subscription.Contracts;
using E_POS.Application.Modules.Tenant.CatalogProduct.Contracts;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.TenantAdmin;
using E_POS.Application.Modules.Tenant.CatalogProduct.Services;
using E_POS.Application.Modules.Tenant.CatalogProduct.Validators;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using E_POS.Domain.Modules.Tenant.PricingTax.Entities;
using Xunit;

namespace E_POS.UnitTests.CatalogProduct;

public class TenantAdminProductDraftServiceTests
{
    private static readonly Guid TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid UserId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly DateTimeOffset FixedNow = new(2026, 8, 9, 12, 0, 0, TimeSpan.Zero);

    private class FakeEntitlementEvaluator : ITenantFeatureEntitlementEvaluator
    {
        public Task<TenantFeatureEntitlementEvaluation> EvaluateAsync(Guid tenantId, string featureCode, DateTimeOffset evaluationTime, CancellationToken cancellationToken = default) =>
            Task.FromResult(TenantFeatureEntitlementEvaluation.Allowed(featureCode, featureCode, false, true, false));

        public Task<bool> IsEnabledAsync(Guid tenantId, string featureCode, DateTimeOffset evaluationTime, CancellationToken cancellationToken = default) =>
            Task.FromResult(true);
    }

    private static TenantAdminProductService CreateService(FakeTenantAdminProductRepository repository)
    {
        var clock = new FakeDateTimeProvider { UtcNow = FixedNow };
        var accessPolicy = new ProductWizardAccessPolicy(new FakeEntitlementEvaluator(), repository, clock);
        return new TenantAdminProductService(
            new FakeProductRepository(),
            repository,
            new TenantAdminProductRequestValidator(),
            clock,
            new FakeTenantAdminProductAuditLogger(),
            accessPolicy);
    }

    private static TenantRequestContext CreateContext(IReadOnlyCollection<string> permissions) =>
        new(TenantId, UserId, permissions);

    public class FakeTenantAdminProductRepository : ITenantAdminProductRepository
    {
        public string TenantStatus { get; set; } = "ACTIVE";
        public bool IsInitialDraft { get; set; }
        public bool ActiveCategoryExists { get; init; } = true;
        public bool BrandBelongs { get; init; } = true;
        public bool ProductCodeExists { get; init; }
        public SaveProductDraftResult? DraftResultOverride { get; init; }
        public SaveProductDraftCommand? LastCommand { get; private set; }
        public ProductSetupWizardDto? SetupDto { get; init; }

        public TenantAdminProductCreateOptionsResponse CreateOptions { get; init; } =
            new TenantAdminProductCreateOptionsResponse([], [], [], [], [], [], [], []);

        public Task<string?> GetTenantStatusAsync(Guid tenantId, CancellationToken cancellationToken) =>
            Task.FromResult<string?>(TenantStatus);

        public Task<bool> IsInitialCreationDraftAsync(Guid tenantId, Guid productId, CancellationToken cancellationToken) =>
            Task.FromResult(IsInitialDraft);

        public Task<bool> HasOperationalHistoryAsync(Guid tenantId, Guid productId, CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public Task<SaveProductDraftResult> SaveProductDraftAsync(
            Guid tenantId,
            Guid userId,
            SaveProductDraftCommand command,
            DateTimeOffset now,
            CancellationToken cancellationToken)
        {
            LastCommand = command;
            if (DraftResultOverride is not null)
            {
                return Task.FromResult(DraftResultOverride);
            }

            var response = new ProductDraftResponse(
                command.ProductId ?? Guid.NewGuid(),
                command.ProductName,
                string.IsNullOrWhiteSpace(command.ProductCode) ? null : command.ProductCode,
                ProductConstants.DraftStatus,
                command.DesiredPublishStatus,
                command.TargetSetupStep,
                now,
                command.ExpectedRowVersion.HasValue ? command.ExpectedRowVersion.Value + 1 : 2,
                command.CategoryId,
                command.BrandId,
                command.ShortDescription,
                command.LongDescription,
                command.PosSellable,
                command.TrackInventory,
                command.BatchTracking,
                command.ExpiryTracking,
                command.SerialTracking,
                command.ProductStructure,
                command.AllowOnlineSale,
                []);

            return Task.FromResult(SaveProductDraftResult.Success(response));
        }

        public Task<ProductSetupWizardDto?> GetSetupAsync(
            Guid tenantId, Guid productId, CancellationToken cancellationToken) =>
            Task.FromResult(SetupDto);

        public Task<IReadOnlyList<BundleValidationProductProjection>> GetProductsForBundleValidationAsync(
            Guid tenantId,
            IReadOnlyCollection<Guid> productIds,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<BundleValidationProductProjection>>([]);

        public Task<IReadOnlyList<BundleValidationVariantProjection>> GetVariantsForBundleValidationAsync(
            Guid tenantId,
            IReadOnlyCollection<Guid> variantIds,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<BundleValidationVariantProjection>>([]);

        public Task<IReadOnlyList<BundleValidationUomProjection>> GetComponentUomValidationDataAsync(
            Guid tenantId,
            IReadOnlyCollection<Guid> componentProductIds,
            IReadOnlyCollection<Guid> componentVariantIds,
            IReadOnlyCollection<Guid> componentUomIds,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<BundleValidationUomProjection>>([]);

        public Task<TenantAdminProductSummaryResponse> GetSummaryAsync(Guid tenantId, CancellationToken cancellationToken) =>
            Task.FromResult(new TenantAdminProductSummaryResponse(0, 0, 0, 0));

        public Task<TenantAdminProductCreateOptionsResponse> GetCreateOptionsAsync(Guid tenantId, CancellationToken cancellationToken) =>
            Task.FromResult(CreateOptions);

        public Task<TenantAdminProductListResponse> GetPagedListAsync(
            Guid tenantId, string? search, Guid? categoryId, Guid? brandId, string? productStatus, string? stockStatus,
            int pageNumber, int pageSize, string? sortBy, string? sortDirection, bool canViewStock, CancellationToken cancellationToken) =>
            Task.FromResult(new TenantAdminProductListResponse([], 1, 10, 0, 0, false, false, 0));

        public Task<TenantAdminProductFilterOptionsResponse> GetFilterOptionsAsync(Guid tenantId, CancellationToken cancellationToken) =>
            Task.FromResult(new TenantAdminProductFilterOptionsResponse([], [], [], []));

        public Task<IReadOnlyDictionary<Guid, string>> GetPrimaryCategoryNamesAsync(
            Guid tenantId, IReadOnlyCollection<Guid> productIds, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyDictionary<Guid, string>>(new Dictionary<Guid, string>());

        public Task<IReadOnlyDictionary<Guid, string>> GetPrimaryImageUrlsAsync(
            Guid tenantId, IReadOnlyCollection<Guid> productIds, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyDictionary<Guid, string>>(new Dictionary<Guid, string>());

        public Task<Guid?> ResolveUnitIdAsync(Guid tenantId, string unitType, CancellationToken cancellationToken) =>
            Task.FromResult<Guid?>(Guid.NewGuid());

        public Task<bool> CategoryBelongsToTenantAsync(
            Guid tenantId, Guid categoryId, Guid? parentCategoryId, CancellationToken cancellationToken) =>
            Task.FromResult(ActiveCategoryExists);

        public Task<bool> BrandBelongsToTenantAsync(Guid tenantId, Guid brandId, CancellationToken cancellationToken) =>
            Task.FromResult(BrandBelongs);

        public Task<bool> TaxClassBelongsToTenantAsync(Guid tenantId, Guid taxClassId, CancellationToken cancellationToken) =>
            Task.FromResult(true);

        public Task<bool> OutletsBelongToTenantAsync(
            Guid tenantId, IReadOnlyCollection<Guid> outletIds, CancellationToken cancellationToken) =>
            Task.FromResult(true);

        public Task<TenantAdminProductCreateResponse> CreateProductAsync(
            Guid tenantId, Guid userId, TenantAdminProductCreateRequest request, Guid unitId, DateTimeOffset now,
            CancellationToken cancellationToken) =>
            Task.FromResult(new TenantAdminProductCreateResponse(Guid.NewGuid(), "x", "SKU", "ACTIVE"));

        public Task<TenantAdminProductDetailResponse?> GetDetailAsync(
            Guid tenantId, Guid productId, CancellationToken cancellationToken) =>
            Task.FromResult<TenantAdminProductDetailResponse?>(null);

        public Task<bool> SkuExistsOnOtherProductAsync(
            Guid tenantId, string sku, Guid productId, CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public Task<bool> BarcodeExistsOnOtherProductAsync(
            Guid tenantId, string barcode, Guid productId, CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public Task<TenantAdminProductDetailResponse?> UpdateProductAsync(
            Guid tenantId, Guid userId, Guid productId, TenantAdminProductCreateRequest request, Guid unitId,
            DateTimeOffset now, CancellationToken cancellationToken) =>
            Task.FromResult<TenantAdminProductDetailResponse?>(null);

        public Task<TenantAdminProductActivationSnapshot?> GetActivationSnapshotAsync(
            Guid tenantId, Guid productId, CancellationToken cancellationToken) =>
            Task.FromResult<TenantAdminProductActivationSnapshot?>(null);

        public Task<TenantAdminProductStatusUpdateResponse?> UpdateProductStatusAsync(
            Guid tenantId, Guid userId, Guid productId, string status, DateTimeOffset now,
            CancellationToken cancellationToken) =>
            Task.FromResult<TenantAdminProductStatusUpdateResponse?>(null);

        public Task<TenantAdminProductDeleteOperationResult> DeleteProductAsync(
            Guid tenantId, Guid userId, Guid productId, DateTimeOffset now, CancellationToken cancellationToken) =>
            Task.FromResult(new TenantAdminProductDeleteOperationResult(null, "product.not_found"));

        public Task<TenantAdminProductDeleteHistoryFlags?> GetDeleteHistoryFlagsAsync(
            Guid tenantId, Guid productId, CancellationToken cancellationToken) =>
            Task.FromResult<TenantAdminProductDeleteHistoryFlags?>(null);

        public Task<TenantAdminProductDashboardRawData> GetDashboardAsync(
            Guid tenantId, TenantAdminProductDashboardQuery query, CancellationToken cancellationToken) =>
            Task.FromResult(new TenantAdminProductDashboardRawData(
                "USD", new(0, 0), new(0, 0), new(0, 0), new(0, 0), new(0, 0), new(0, 0), 0, 0, [], []));

        public Task<bool> ActiveCategoryExistsAsync(Guid tenantId, Guid categoryId, CancellationToken cancellationToken) =>
            Task.FromResult(ActiveCategoryExists);

        public Task<bool> ProductCodeExistsAsync(
            Guid tenantId, string productCode, Guid? excludeProductId, CancellationToken cancellationToken) =>
            Task.FromResult(ProductCodeExists);

        public Task<Guid?> GetDefaultInventoryUomIdAsync(Guid tenantId, CancellationToken cancellationToken) =>
            Task.FromResult<Guid?>(Guid.NewGuid());
    }

    private sealed class FakeDateTimeProvider : IDateTimeProvider
    {
        public DateTimeOffset UtcNow { get; init; } = FixedNow;
    }

    private sealed class FakeProductRepository : IProductRepository
    {
        public IReadOnlyCollection<string> ExistingSkus { get; init; } = [];
        public IReadOnlyCollection<string> ExistingBarcodes { get; init; } = [];
        public IReadOnlyCollection<Guid> ExistingProductIds { get; init; } = [];
        public ProductListResponse? ListResponse { get; init; }

        public Task<bool> ProductCodeExistsAsync(Guid tenantId, string productCode, Guid? excludeProductId, CancellationToken cancellationToken) => Task.FromResult(false);
        public Task<bool> SkuExistsAsync(Guid tenantId, string sku, Guid? excludeProductId, CancellationToken cancellationToken) => Task.FromResult(ExistingSkus.Contains(sku, StringComparer.OrdinalIgnoreCase));
        public Task<bool> BarcodeExistsAsync(Guid tenantId, string barcodeValue, Guid? excludeProductId, CancellationToken cancellationToken) => Task.FromResult(ExistingBarcodes.Contains(barcodeValue, StringComparer.OrdinalIgnoreCase));
        public Task<ProductListResponse> ListAsync(Guid tenantId, int pageNumber, int pageSize, string? search, CancellationToken cancellationToken) => Task.FromResult(ListResponse ?? new ProductListResponse([], pageNumber, pageSize, 0));
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
        public Task<bool> ProductExistsAsync(Guid tenantId, Guid productId, CancellationToken cancellationToken) => Task.FromResult(ExistingProductIds.Contains(productId));
        public Task<bool> ProductVariantExistsAsync(Guid tenantId, Guid productId, Guid variantId, CancellationToken cancellationToken) => Task.FromResult(false);
        public Task<bool> ProductIsPriceableAsync(Guid tenantId, Guid productId, CancellationToken cancellationToken) => Task.FromResult(ExistingProductIds.Contains(productId));
        public Task<bool> ProductVariantIsPriceableAsync(Guid tenantId, Guid productId, Guid variantId, CancellationToken cancellationToken) => Task.FromResult(false);
    }

    private sealed class FakeTenantAdminProductAuditLogger : ITenantAdminProductAuditLogger
    {
        public bool ProductDeletedLogged { get; private set; }
        public Guid LastProductId { get; private set; }
        public string LastOutcome { get; private set; } = string.Empty;
        public string LastStatus { get; private set; } = string.Empty;

        public void LogProductDeleted(Guid tenantId, Guid userId, Guid productId, string outcome, string status)
        {
            ProductDeletedLogged = true;
            LastProductId = productId;
            LastOutcome = outcome;
            LastStatus = status;
        }

        public void LogStep2DraftUpdated(Guid tenantId, Guid userId, Guid productId, string oldStructure, string newStructure, bool oldTrackInventory, bool newTrackInventory, bool oldBatchTracking, bool newBatchTracking, bool oldExpiryTracking, bool newExpiryTracking, bool oldSerialTracking, bool newSerialTracking, long rowVersion) { }
    }
}
