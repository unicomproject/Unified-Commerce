using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.CatalogProduct.Contracts;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.TenantAdmin;
using E_POS.Application.Modules.Tenant.CatalogProduct.Services;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace E_POS.UnitTests.CatalogProduct;

public sealed class TenantExternalCategoryResolverTests
{
    private static readonly Guid TenantA = Guid.NewGuid();
    private static readonly Guid TenantB = Guid.NewGuid();

    [Fact]
    public async Task ResolveAsync_SavedMappingActiveCategory_ReturnsMappedCategory()
    {
        var categoryId = Guid.NewGuid();
        var mappingRepo = new FakeMappingRepository();
        mappingRepo.Add(ExternalCategoryMapping.Create(
            Guid.NewGuid(),
            TenantA,
            "openfoodfacts",
            "en:colas",
            "Colas",
            categoryId,
            "PRODUCT_CONFIRMED",
            null,
            DateTimeOffset.UtcNow));

        var productRepo = new FakeProductRepository();
        productRepo.AddSelectableCategory(TenantA, new TenantAdminProductCategoryOptionResponse(
            categoryId,
            "CAT-SOFT-DRINKS",
            "Soft Drinks",
            null,
            1,
            "Soft Drinks",
            false,
            0));

        var resolver = new TenantExternalCategoryResolver(
            mappingRepo,
            productRepo,
            NullLogger<TenantExternalCategoryResolver>.Instance);

        var request = new TenantCategoryResolutionRequest(
            TenantA,
            "openfoodfacts",
            "en:colas",
            "Colas");

        var result = await resolver.ResolveAsync(request, CancellationToken.None);

        Assert.NotNull(result.MappedCategory);
        Assert.Equal(categoryId, result.MappedCategory!.Id);
        Assert.Equal("Soft Drinks", result.MappedCategory.Name);
        Assert.Equal("CAT-SOFT-DRINKS", result.MappedCategory.Code);
        Assert.Empty(result.Suggestions);
    }

    [Fact]
    public async Task ResolveAsync_SavedMappingInactiveCategory_IgnoresMappingAndEvaluatesSuggestions()
    {
        var inactiveCategoryId = Guid.NewGuid();
        var activeCategoryId = Guid.NewGuid();

        var mappingRepo = new FakeMappingRepository();
        mappingRepo.Add(ExternalCategoryMapping.Create(
            Guid.NewGuid(),
            TenantA,
            "openfoodfacts",
            "en:colas",
            "Colas",
            inactiveCategoryId,
            "PRODUCT_CONFIRMED",
            null,
            DateTimeOffset.UtcNow));

        var productRepo = new FakeProductRepository();
        // Mark inactive category as NOT effectively selectable
        productRepo.SetCategorySelectable(TenantA, inactiveCategoryId, false);

        // Add active category that matches exact name
        productRepo.AddSelectableCategory(TenantA, new TenantAdminProductCategoryOptionResponse(
            activeCategoryId,
            "CAT-COLAS",
            "Colas",
            null,
            1,
            "Colas",
            false,
            0));

        var resolver = new TenantExternalCategoryResolver(
            mappingRepo,
            productRepo,
            NullLogger<TenantExternalCategoryResolver>.Instance);

        var request = new TenantCategoryResolutionRequest(
            TenantA,
            "openfoodfacts",
            "en:colas",
            "Colas");

        var result = await resolver.ResolveAsync(request, CancellationToken.None);

        // MappedCategory should be null because it was inactive
        Assert.Null(result.MappedCategory);
        // Active category should be suggested instead
        Assert.Single(result.Suggestions);
        Assert.Equal(activeCategoryId, result.Suggestions[0].Id);
        Assert.Equal("EXACT", result.Suggestions[0].MatchType);
    }

    [Fact]
    public async Task ResolveAsync_NoMapping_ExactCategoryMatch_ReturnsExactSuggestion()
    {
        var categoryId = Guid.NewGuid();
        var mappingRepo = new FakeMappingRepository();
        var productRepo = new FakeProductRepository();

        productRepo.AddSelectableCategory(TenantA, new TenantAdminProductCategoryOptionResponse(
            categoryId,
            "CAT-CARBONATED",
            "Carbonated Drinks",
            null,
            1,
            "Carbonated Drinks",
            false,
            0));

        var resolver = new TenantExternalCategoryResolver(
            mappingRepo,
            productRepo,
            NullLogger<TenantExternalCategoryResolver>.Instance);

        var request = new TenantCategoryResolutionRequest(
            TenantA,
            "openfoodfacts",
            "en:carbonated-drinks",
            "Carbonated drinks");

        var result = await resolver.ResolveAsync(request, CancellationToken.None);

        Assert.Null(result.MappedCategory);
        Assert.Single(result.Suggestions);
        Assert.Equal(categoryId, result.Suggestions[0].Id);
        Assert.Equal("EXACT", result.Suggestions[0].MatchType);
    }

    [Fact]
    public async Task ResolveAsync_NoMapping_NormalizedCategoryMatch_ReturnsNormalizedSuggestion()
    {
        var categoryId = Guid.NewGuid();
        var mappingRepo = new FakeMappingRepository();
        var productRepo = new FakeProductRepository();

        productRepo.AddSelectableCategory(TenantA, new TenantAdminProductCategoryOptionResponse(
            categoryId,
            "CAT-SOFT-DRINKS",
            "Soft Drinks",
            null,
            1,
            "Soft Drinks",
            false,
            0));

        var resolver = new TenantExternalCategoryResolver(
            mappingRepo,
            productRepo,
            NullLogger<TenantExternalCategoryResolver>.Instance);

        // External name has hyphens
        var request = new TenantCategoryResolutionRequest(
            TenantA,
            "openfoodfacts",
            "en:soft-drinks",
            "Soft-Drinks");

        var result = await resolver.ResolveAsync(request, CancellationToken.None);

        Assert.Null(result.MappedCategory);
        Assert.Single(result.Suggestions);
        Assert.Equal(categoryId, result.Suggestions[0].Id);
        Assert.Equal("NORMALIZED", result.Suggestions[0].MatchType);
    }

    [Fact]
    public async Task ResolveAsync_NoMapping_SimilarityTokenMatch_ReturnsSimilaritySuggestion()
    {
        var categoryId = Guid.NewGuid();
        var mappingRepo = new FakeMappingRepository();
        var productRepo = new FakeProductRepository();

        productRepo.AddSelectableCategory(TenantA, new TenantAdminProductCategoryOptionResponse(
            categoryId,
            "CAT-BEVERAGES",
            "Cold Beverages & Juices",
            null,
            1,
            "Cold Beverages & Juices",
            false,
            0));

        var resolver = new TenantExternalCategoryResolver(
            mappingRepo,
            productRepo,
            NullLogger<TenantExternalCategoryResolver>.Instance);

        var request = new TenantCategoryResolutionRequest(
            TenantA,
            "openfoodfacts",
            "en:beverages",
            "Beverages");

        var result = await resolver.ResolveAsync(request, CancellationToken.None);

        Assert.Null(result.MappedCategory);
        Assert.Single(result.Suggestions);
        Assert.Equal(categoryId, result.Suggestions[0].Id);
        Assert.Equal("SIMILARITY", result.Suggestions[0].MatchType);
    }

    [Fact]
    public async Task ResolveAsync_UnrelatedCategories_ReturnsZeroSuggestions()
    {
        var mappingRepo = new FakeMappingRepository();
        var productRepo = new FakeProductRepository();

        productRepo.AddSelectableCategory(TenantA, new TenantAdminProductCategoryOptionResponse(
            Guid.NewGuid(), "CAT-LAPTOPS", "Laptops", null, 1, "Laptops", false, 0));
        productRepo.AddSelectableCategory(TenantA, new TenantAdminProductCategoryOptionResponse(
            Guid.NewGuid(), "CAT-PHONES", "Mobile Phones", null, 1, "Mobile Phones", false, 0));

        var resolver = new TenantExternalCategoryResolver(
            mappingRepo,
            productRepo,
            NullLogger<TenantExternalCategoryResolver>.Instance);

        var request = new TenantCategoryResolutionRequest(
            TenantA,
            "openfoodfacts",
            "en:carbonated-drinks",
            "Carbonated drinks");

        var result = await resolver.ResolveAsync(request, CancellationToken.None);

        Assert.Null(result.MappedCategory);
        Assert.Empty(result.Suggestions);
    }

    [Fact]
    public async Task ResolveAsync_TenantIsolation_TenantAMappingNotReturnedForTenantB()
    {
        var categoryId = Guid.NewGuid();
        var mappingRepo = new FakeMappingRepository();

        // Tenant A has mapping
        mappingRepo.Add(ExternalCategoryMapping.Create(
            Guid.NewGuid(),
            TenantA,
            "openfoodfacts",
            "en:colas",
            "Colas",
            categoryId,
            "PRODUCT_CONFIRMED",
            null,
            DateTimeOffset.UtcNow));

        var productRepo = new FakeProductRepository();
        productRepo.AddSelectableCategory(TenantA, new TenantAdminProductCategoryOptionResponse(
            categoryId, "CAT-SOFT-DRINKS", "Soft Drinks", null, 1, "Soft Drinks", false, 0));

        var resolver = new TenantExternalCategoryResolver(
            mappingRepo,
            productRepo,
            NullLogger<TenantExternalCategoryResolver>.Instance);

        // Query for Tenant B
        var request = new TenantCategoryResolutionRequest(
            TenantB,
            "openfoodfacts",
            "en:colas",
            "Colas");

        var result = await resolver.ResolveAsync(request, CancellationToken.None);

        Assert.Null(result.MappedCategory);
        Assert.Empty(result.Suggestions);
    }

    [Fact]
    public async Task ResolveAsync_EmptyExternalCategoryKey_ReturnsEmptyResult()
    {
        var mappingRepo = new FakeMappingRepository();
        var productRepo = new FakeProductRepository();

        var resolver = new TenantExternalCategoryResolver(
            mappingRepo,
            productRepo,
            NullLogger<TenantExternalCategoryResolver>.Instance);

        var request = new TenantCategoryResolutionRequest(
            TenantA,
            "openfoodfacts",
            null,
            null);

        var result = await resolver.ResolveAsync(request, CancellationToken.None);

        Assert.Null(result.MappedCategory);
        Assert.Empty(result.Suggestions);
    }

    private sealed class FakeMappingRepository : IExternalCategoryMappingRepository
    {
        private readonly List<ExternalCategoryMapping> _mappings = new();

        public void Add(ExternalCategoryMapping mapping) => _mappings.Add(mapping);

        public Task<ExternalCategoryMapping?> GetAsync(
            Guid tenantId,
            string provider,
            string externalCategoryKey,
            CancellationToken cancellationToken)
        {
            var p = provider.Trim().ToLowerInvariant();
            var k = externalCategoryKey.Trim().ToLowerInvariant();
            return Task.FromResult(_mappings.FirstOrDefault(
                x => x.TenantId == tenantId && x.Provider == p && x.ExternalCategoryKey == k));
        }

        public Task<ExternalCategoryMapping> UpsertAsync(
            Guid tenantId,
            string provider,
            string externalCategoryKey,
            string externalCategoryName,
            Guid tenantCategoryId,
            string mappingSource,
            Guid? userId,
            CancellationToken cancellationToken,
            bool saveChanges = true)
        {
            var p = provider.Trim().ToLowerInvariant();
            var k = externalCategoryKey.Trim().ToLowerInvariant();
            var existing = _mappings.FirstOrDefault(x => x.TenantId == tenantId && x.Provider == p && x.ExternalCategoryKey == k);
            if (existing is not null)
            {
                existing.Update(externalCategoryName, tenantCategoryId, mappingSource, userId, DateTimeOffset.UtcNow);
                return Task.FromResult(existing);
            }

            var created = ExternalCategoryMapping.Create(
                Guid.NewGuid(), tenantId, p, k, externalCategoryName, tenantCategoryId, mappingSource, userId, DateTimeOffset.UtcNow);
            _mappings.Add(created);
            return Task.FromResult(created);
        }
    }

    private sealed class FakeProductRepository : ITenantAdminProductRepository
    {
        private readonly Dictionary<Guid, List<TenantAdminProductCategoryOptionResponse>> _categories = new();
        private readonly Dictionary<(Guid, Guid), bool> _selectability = new();

        public void AddSelectableCategory(Guid tenantId, TenantAdminProductCategoryOptionResponse category)
        {
            if (!_categories.TryGetValue(tenantId, out var list))
            {
                list = new List<TenantAdminProductCategoryOptionResponse>();
                _categories[tenantId] = list;
            }
            list.Add(category);
            _selectability[(tenantId, category.Id)] = true;
        }

        public void SetCategorySelectable(Guid tenantId, Guid categoryId, bool selectable)
        {
            _selectability[(tenantId, categoryId)] = selectable;
        }

        public Task<bool> IsCategoryEffectivelySelectableAsync(Guid tenantId, Guid categoryId, CancellationToken cancellationToken)
        {
            if (_selectability.TryGetValue((tenantId, categoryId), out var s))
            {
                return Task.FromResult(s);
            }
            return Task.FromResult(false);
        }

        public Task<TenantAdminProductCreateOptionsResponse> GetCreateOptionsAsync(Guid tenantId, CancellationToken cancellationToken)
        {
            var cats = _categories.TryGetValue(tenantId, out var list)
                ? list.Where(c => _selectability.TryGetValue((tenantId, c.Id), out var sel) && sel).ToList()
                : new List<TenantAdminProductCategoryOptionResponse>();

            return Task.FromResult(new TenantAdminProductCreateOptionsResponse(
                Categories: cats,
                Brands: Array.Empty<TenantAdminProductBrandOptionResponse>(),
                Units: Array.Empty<TenantAdminProductUnitOptionResponse>(),
                Taxes: Array.Empty<TenantAdminProductTaxOptionResponse>(),
                Outlets: Array.Empty<TenantAdminProductOutletOptionResponse>(),
                VariantOptionTemplates: Array.Empty<TenantAdminProductVariantOptionTemplateResponse>(),
                SalesChannels: Array.Empty<TenantAdminProductSalesChannelOptionResponse>(),
                BarcodeTypes: Array.Empty<TenantAdminProductBarcodeTypeOptionResponse>(),
                CurrencyCode: "LKR"));
        }


        public Task<TenantAdminProductSummaryResponse> GetSummaryAsync(Guid tenantId, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<TenantAdminProductSummaryCardsResponse> GetSummaryCardsAsync(Guid tenantId, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<bool> CategoryBelongsToTenantAsync(Guid tenantId, Guid categoryId, Guid? parentCategoryId, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<bool> BrandBelongsToTenantAsync(Guid tenantId, Guid brandId, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<bool> TaxClassBelongsToTenantAsync(Guid tenantId, Guid taxClassId, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<bool> OutletsBelongToTenantAsync(Guid tenantId, IReadOnlyCollection<Guid> outletIds, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<Guid?> ResolveUnitIdAsync(Guid tenantId, string unitCode, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<TenantAdminProductCreateResponse> CreateProductAsync(Guid tenantId, Guid? userId, TenantAdminProductCreateRequest request, Guid unitId, DateTimeOffset now, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<TenantAdminProductDetailResponse?> GetDetailAsync(Guid tenantId, Guid productId, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<TenantAdminProductStockDetailResponse?> GetStockDetailAsync(Guid tenantId, Guid productId, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<IReadOnlyList<TenantAdminProductOutletDetailResponse>> GetOutletDetailsAsync(Guid tenantId, Guid productId, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<TenantAdminProductStatusUpdateResponse?> UpdateStatusAsync(Guid tenantId, Guid productId, string status, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<TenantAdminProductDeleteHistoryFlags?> GetDeleteHistoryFlagsAsync(Guid tenantId, Guid productId, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<TenantAdminProductDeleteResponse?> SoftDeleteAsync(Guid tenantId, Guid productId, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<bool> SetPrimaryCategoryAsync(Guid tenantId, Guid productId, Guid categoryId, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<ProductDraftResponse> SaveDraftAsync(Guid tenantId, Guid? userId, Guid? productId, SaveProductDraftRequest request, DateTimeOffset now, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<ProductSetupWizardDto?> GetSetupAsync(Guid tenantId, Guid productId, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<bool> DeleteDraftAsync(Guid tenantId, Guid productId, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<TenantAdminProductListResponse> GetPagedListAsync(Guid tenantId, string? search, Guid? categoryId, Guid? brandId, string? productStatus, string? stockStatus, int pageNumber, int pageSize, string? sortBy, string? sortDirection, bool canViewStock, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<IReadOnlyDictionary<Guid, string>> GetPrimaryCategoryNamesAsync(Guid tenantId, IReadOnlyCollection<Guid> productIds, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyDictionary<Guid, string>>(new Dictionary<Guid, string>());
        public Task<IReadOnlyDictionary<Guid, string>> GetPrimaryImageUrlsAsync(Guid tenantId, IReadOnlyCollection<Guid> productIds, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyDictionary<Guid, string>>(new Dictionary<Guid, string>());
        public Task<TenantAdminProductFilterOptionsResponse> GetFilterOptionsAsync(Guid tenantId, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<bool> SkuExistsOnOtherProductAsync(Guid tenantId, string sku, Guid productId, CancellationToken cancellationToken) => Task.FromResult(false);
        public Task<bool> BarcodeExistsOnOtherProductAsync(Guid tenantId, string barcode, Guid productId, CancellationToken cancellationToken) => Task.FromResult(false);
        public Task<TenantAdminProductDetailResponse?> UpdateProductAsync(Guid tenantId, Guid userId, Guid productId, TenantAdminProductCreateRequest request, Guid unitId, DateTimeOffset now, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<TenantAdminProductStatusUpdateResponse?> UpdateProductStatusAsync(Guid tenantId, Guid userId, Guid productId, string status, DateTimeOffset now, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<TenantAdminProductActivationSnapshot?> GetActivationSnapshotAsync(Guid tenantId, Guid productId, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<TenantAdminProductDeleteOperationResult> DeleteProductAsync(Guid tenantId, Guid userId, Guid productId, DateTimeOffset now, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<TenantAdminProductDashboardRawData> GetDashboardAsync(Guid tenantId, TenantAdminProductDashboardQuery query, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<bool> ActiveCategoryExistsAsync(Guid tenantId, Guid categoryId, CancellationToken cancellationToken) => Task.FromResult(true);
        public Task<bool> CategoryExistsForExistingMappingAsync(Guid tenantId, Guid categoryId, CancellationToken cancellationToken) => Task.FromResult(true);
        public Task<string?> GetActiveCategoryCodeAsync(Guid tenantId, Guid categoryId, CancellationToken cancellationToken) => Task.FromResult<string?>("CAT");
        public Task<long> AllocateNextProductSkuSequenceAsync(Guid tenantId, DateTimeOffset now, CancellationToken cancellationToken) => Task.FromResult(1L);
        public Task<string?> GetGeneratedSkuBaseAsync(Guid tenantId, Guid productId, CancellationToken cancellationToken) => Task.FromResult<string?>(null);
        public Task<E_POS.Application.Common.Models.ApplicationError?> ReplaceGeneratedSkuBaseAsync(Guid tenantId, Guid userId, Guid productId, long expectedRowVersion, string generatedSkuBase, DateTimeOffset now, CancellationToken cancellationToken) => Task.FromResult<E_POS.Application.Common.Models.ApplicationError?>(null);
        public Task<bool> ProductCodeExistsAsync(Guid tenantId, string productCode, Guid? excludeProductId, CancellationToken cancellationToken) => Task.FromResult(false);
        public Task<Guid?> GetDefaultInventoryUomIdAsync(Guid tenantId, CancellationToken cancellationToken) => Task.FromResult<Guid?>(Guid.NewGuid());
        public Task<bool> SkuExistsAsync(Guid tenantId, string sku, Guid? excludeProductId = null, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<bool> BarcodeExistsAsync(Guid tenantId, string barcode, Guid? excludeProductId = null, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<IReadOnlyDictionary<string, Guid>> FindSkuConflictsAsync(Guid tenantId, IReadOnlyCollection<string> skus, IReadOnlyCollection<Guid> excludeVariantIds, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyDictionary<string, Guid>>(new Dictionary<string, Guid>());
        public Task<IReadOnlyDictionary<string, Guid>> FindBarcodeConflictsAsync(Guid tenantId, IReadOnlyCollection<string> barcodes, IReadOnlyCollection<Guid> excludeVariantIds, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyDictionary<string, Guid>>(new Dictionary<string, Guid>());
        public Task<IReadOnlyList<BarcodeSkuVariantTargetProjection>> GetStep5SellableVariantTargetsAsync(Guid tenantId, Guid productId, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<BarcodeSkuVariantTargetProjection>>(Array.Empty<BarcodeSkuVariantTargetProjection>());
        public Task<bool> ProductSlugExistsAsync(string slug, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<bool> SkuExistsInDraftAsync(Guid tenantId, string sku, Guid draftId, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<string?> GetTenantStatusAsync(Guid tenantId, CancellationToken cancellationToken) => Task.FromResult<string?>("ACTIVE");
        public Task<bool> IsInitialCreationDraftAsync(Guid tenantId, Guid productId, CancellationToken cancellationToken) => Task.FromResult(false);
        public Task<bool> HasScanContextAsync(Guid tenantId, Guid productId, CancellationToken cancellationToken) => Task.FromResult(false);
        public Task<SaveProductDraftResult> SaveProductDraftAsync(Guid tenantId, Guid userId, SaveProductDraftCommand command, DateTimeOffset now, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<bool> HasOperationalHistoryAsync(Guid tenantId, Guid productId, CancellationToken cancellationToken) => Task.FromResult(false);
        public Task UpdateVariantAsync(Guid tenantId, Guid userId, Guid productId, Guid variantId, TenantAdminProductVariantUpdateRequest request, DateTimeOffset now, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task AddBarcodeAsync(Guid tenantId, Guid userId, Guid productId, Guid variantId, TenantAdminProductBarcodeAddRequest request, DateTimeOffset now, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task DeleteBarcodeAsync(Guid tenantId, Guid userId, Guid productId, Guid variantId, Guid barcodeId, DateTimeOffset now, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task RestoreAsync(Guid tenantId, Guid userId, Guid productId, DateTimeOffset now, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<TenantAdminProductCreateResponse> DuplicateAsync(Guid tenantId, Guid userId, Guid productId, DateTimeOffset now, CancellationToken cancellationToken) => Task.FromResult(new TenantAdminProductCreateResponse(Guid.NewGuid(), "Duplicate", "DUP-001", "DRAFT"));
        public Task<IReadOnlyList<BundleValidationProductProjection>> GetProductsForBundleValidationAsync(Guid tenantId, IReadOnlyCollection<Guid> productIds, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<BundleValidationProductProjection>>(Array.Empty<BundleValidationProductProjection>());
        public Task<IReadOnlyList<BundleValidationVariantProjection>> GetVariantsForBundleValidationAsync(Guid tenantId, IReadOnlyCollection<Guid> variantIds, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<BundleValidationVariantProjection>>(Array.Empty<BundleValidationVariantProjection>());
        public Task<IReadOnlyList<BundleValidationUomProjection>> GetComponentUomValidationDataAsync(Guid tenantId, IReadOnlyCollection<Guid> componentProductIds, IReadOnlyCollection<Guid> componentVariantIds, IReadOnlyCollection<Guid> componentUomIds, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<BundleValidationUomProjection>>(Array.Empty<BundleValidationUomProjection>());
        public Task SaveVariantsAsync(Guid tenantId, Guid productId, VariantConfigurationDto variantConfiguration, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<IReadOnlyList<ApplicationFieldError>> ValidateVariantConfigurationCatalogAsync(Guid tenantId, Guid? productId, VariantConfigurationDto configuration, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<ApplicationFieldError>>(Array.Empty<ApplicationFieldError>());
        public Task<SaveProductDraftResult> CreateProductFromWizardAsync(Guid tenantId, Guid userId, TenantAdminWizardProductCreateRequest request, DateTimeOffset now, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<ProductBarcodeResolveMatchProjection?> FindBarcodeResolveMatchAsync(Guid tenantId, string normalizedBarcode, CancellationToken cancellationToken) => Task.FromResult<ProductBarcodeResolveMatchProjection?>(null);
    }
}
