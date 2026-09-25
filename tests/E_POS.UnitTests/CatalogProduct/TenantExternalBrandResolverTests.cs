using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.CatalogProduct.Contracts;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.TenantAdmin;
using E_POS.Application.Modules.Tenant.CatalogProduct.Services;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace E_POS.UnitTests.CatalogProduct;

public sealed class TenantExternalBrandResolverTests
{
    private static readonly Guid TenantA = Guid.NewGuid();
    private static readonly Guid TenantB = Guid.NewGuid();

    [Fact]
    public async Task ResolveAsync_SavedMappingActiveBrand_ReturnsMappedBrand()
    {
        var brandId = Guid.NewGuid();
        var mappingRepo = new FakeMappingRepository();
        mappingRepo.Add(ExternalBrandMapping.Create(
            Guid.NewGuid(),
            TenantA,
            "openfoodfacts",
            "coca cola",
            "Coca-Cola",
            brandId,
            "PRODUCT_CONFIRMED",
            null,
            DateTimeOffset.UtcNow));

        var productRepo = new FakeProductRepository();
        productRepo.AddSelectableBrand(TenantA, new TenantAdminProductBrandOptionResponse(
            brandId, "Coca Cola", "COCA_COLA"));

        var resolver = new TenantExternalBrandResolver(
            mappingRepo,
            productRepo,
            NullLogger<TenantExternalBrandResolver>.Instance);

        var request = new TenantBrandResolutionRequest(
            TenantA,
            "openfoodfacts",
            "coca cola",
            "Coca-Cola");

        var result = await resolver.ResolveAsync(request, CancellationToken.None);

        Assert.NotNull(result.MappedBrand);
        Assert.Equal(brandId, result.MappedBrand!.Id);
        Assert.Equal("Coca Cola", result.MappedBrand.Name);
        Assert.Equal("COCA_COLA", result.MappedBrand.Code);
        Assert.Empty(result.Suggestions);
    }

    [Fact]
    public async Task ResolveAsync_SavedMappingInactiveBrand_IgnoresMappingAndEvaluatesSuggestions()
    {
        var inactiveBrandId = Guid.NewGuid();
        var activeBrandId = Guid.NewGuid();

        var mappingRepo = new FakeMappingRepository();
        mappingRepo.Add(ExternalBrandMapping.Create(
            Guid.NewGuid(),
            TenantA,
            "openfoodfacts",
            "coca cola",
            "Coca-Cola",
            inactiveBrandId,
            "PRODUCT_CONFIRMED",
            null,
            DateTimeOffset.UtcNow));

        var productRepo = new FakeProductRepository();
        // Inactive/deleted mapped brand -> not selectable
        productRepo.SetBrandSelectable(TenantA, inactiveBrandId, false);

        // Active brand with exact matching name
        productRepo.AddSelectableBrand(TenantA, new TenantAdminProductBrandOptionResponse(
            activeBrandId, "Coca Cola", "COCA_COLA"));

        var resolver = new TenantExternalBrandResolver(
            mappingRepo,
            productRepo,
            NullLogger<TenantExternalBrandResolver>.Instance);

        var request = new TenantBrandResolutionRequest(
            TenantA,
            "openfoodfacts",
            "coca cola",
            "Coca Cola");

        var result = await resolver.ResolveAsync(request, CancellationToken.None);

        Assert.Null(result.MappedBrand);
        Assert.Single(result.Suggestions);
        Assert.Equal(activeBrandId, result.Suggestions[0].Id);
        Assert.Equal("EXACT", result.Suggestions[0].MatchType);
    }

    [Fact]
    public async Task ResolveAsync_SavedMappingMissingBrand_IgnoresMappingAndEvaluatesSuggestions()
    {
        var missingBrandId = Guid.NewGuid();
        var mappingRepo = new FakeMappingRepository();
        mappingRepo.Add(ExternalBrandMapping.Create(
            Guid.NewGuid(),
            TenantA,
            "openfoodfacts",
            "coca cola",
            "Coca-Cola",
            missingBrandId,
            "PRODUCT_CONFIRMED",
            null,
            DateTimeOffset.UtcNow));

        // No selectability entry at all for missingBrandId -> resolver treats as not selectable.
        var productRepo = new FakeProductRepository();

        var resolver = new TenantExternalBrandResolver(
            mappingRepo,
            productRepo,
            NullLogger<TenantExternalBrandResolver>.Instance);

        var request = new TenantBrandResolutionRequest(
            TenantA,
            "openfoodfacts",
            "coca cola",
            "Coca Cola");

        var result = await resolver.ResolveAsync(request, CancellationToken.None);

        Assert.Null(result.MappedBrand);
        Assert.Empty(result.Suggestions);
    }

    [Fact]
    public async Task ResolveAsync_NoMapping_ExactBrandMatch_ReturnsExactSuggestion()
    {
        var brandId = Guid.NewGuid();
        var mappingRepo = new FakeMappingRepository();
        var productRepo = new FakeProductRepository();

        productRepo.AddSelectableBrand(TenantA, new TenantAdminProductBrandOptionResponse(
            brandId, "Nestle", "NESTLE"));

        var resolver = new TenantExternalBrandResolver(
            mappingRepo,
            productRepo,
            NullLogger<TenantExternalBrandResolver>.Instance);

        var request = new TenantBrandResolutionRequest(
            TenantA,
            "openfoodfacts",
            "nestle",
            "Nestle");

        var result = await resolver.ResolveAsync(request, CancellationToken.None);

        Assert.Null(result.MappedBrand);
        Assert.Single(result.Suggestions);
        Assert.Equal(brandId, result.Suggestions[0].Id);
        Assert.Equal("EXACT", result.Suggestions[0].MatchType);
    }

    [Fact]
    public async Task ResolveAsync_NoMapping_NormalizedBrandMatch_ReturnsNormalizedSuggestion()
    {
        var brandId = Guid.NewGuid();
        var mappingRepo = new FakeMappingRepository();
        var productRepo = new FakeProductRepository();

        productRepo.AddSelectableBrand(TenantA, new TenantAdminProductBrandOptionResponse(
            brandId, "Coca Cola", "COCA_COLA"));

        var resolver = new TenantExternalBrandResolver(
            mappingRepo,
            productRepo,
            NullLogger<TenantExternalBrandResolver>.Instance);

        // External name has punctuation
        var request = new TenantBrandResolutionRequest(
            TenantA,
            "openfoodfacts",
            "coca cola",
            "Coca-Cola");

        var result = await resolver.ResolveAsync(request, CancellationToken.None);

        Assert.Null(result.MappedBrand);
        Assert.Single(result.Suggestions);
        Assert.Equal(brandId, result.Suggestions[0].Id);
        Assert.Equal("NORMALIZED", result.Suggestions[0].MatchType);
    }

    [Fact]
    public async Task ResolveAsync_NoMapping_SimilarityTokenMatch_ReturnsSimilaritySuggestion()
    {
        var brandId = Guid.NewGuid();
        var mappingRepo = new FakeMappingRepository();
        var productRepo = new FakeProductRepository();

        productRepo.AddSelectableBrand(TenantA, new TenantAdminProductBrandOptionResponse(
            brandId, "Nestle Lanka", "NESTLE_LANKA"));

        var resolver = new TenantExternalBrandResolver(
            mappingRepo,
            productRepo,
            NullLogger<TenantExternalBrandResolver>.Instance);

        var request = new TenantBrandResolutionRequest(
            TenantA,
            "openfoodfacts",
            "nestle",
            "Nestle");

        var result = await resolver.ResolveAsync(request, CancellationToken.None);

        Assert.Null(result.MappedBrand);
        Assert.Single(result.Suggestions);
        Assert.Equal(brandId, result.Suggestions[0].Id);
        Assert.Equal("SIMILARITY", result.Suggestions[0].MatchType);
    }

    [Fact]
    public async Task ResolveAsync_AccentedExternalBrandVsUnaccentedTenantBrand_ReturnsNormalizedSuggestion()
    {
        // Regression test for real barcode 7613032655495 (OpenFoodFacts "Nestlé, Ricore, Ricoré"):
        // ExternalBrandKeyDeriver takes the primary segment "Nestlé" as-is (accented). Before the
        // diacritic-folding fix, BrandConstants.NormalizeNameForComparison only lowercased the text,
        // so "nestlé" never matched a tenant brand stored as "nestle" at any tier (EXACT, NORMALIZED,
        // or SIMILARITY) even though they are the same brand — Quick Add Brand was shown despite a
        // real match existing. EXACT intentionally stays byte-for-byte strict; the accent-insensitive
        // equivalence now surfaces as a NORMALIZED suggestion instead.
        var brandId = Guid.NewGuid();
        var mappingRepo = new FakeMappingRepository();
        var productRepo = new FakeProductRepository();

        productRepo.AddSelectableBrand(TenantA, new TenantAdminProductBrandOptionResponse(
            brandId, "nestle", "NESTLE- BEVERAGE"));

        var resolver = new TenantExternalBrandResolver(
            mappingRepo,
            productRepo,
            NullLogger<TenantExternalBrandResolver>.Instance);

        var request = new TenantBrandResolutionRequest(
            TenantA,
            "openfoodfacts",
            "nestle",
            "Nestlé");

        var result = await resolver.ResolveAsync(request, CancellationToken.None);

        Assert.Null(result.MappedBrand);
        Assert.Single(result.Suggestions);
        Assert.Equal(brandId, result.Suggestions[0].Id);
        Assert.Equal("NORMALIZED", result.Suggestions[0].MatchType);
    }

    [Fact]
    public async Task ResolveAsync_UnrelatedBrands_ReturnsZeroSuggestions()
    {
        var mappingRepo = new FakeMappingRepository();
        var productRepo = new FakeProductRepository();

        productRepo.AddSelectableBrand(TenantA, new TenantAdminProductBrandOptionResponse(
            Guid.NewGuid(), "Sony", "SONY"));
        productRepo.AddSelectableBrand(TenantA, new TenantAdminProductBrandOptionResponse(
            Guid.NewGuid(), "Samsung", "SAMSUNG"));

        var resolver = new TenantExternalBrandResolver(
            mappingRepo,
            productRepo,
            NullLogger<TenantExternalBrandResolver>.Instance);

        var request = new TenantBrandResolutionRequest(
            TenantA,
            "openfoodfacts",
            "coca cola",
            "Coca Cola");

        var result = await resolver.ResolveAsync(request, CancellationToken.None);

        Assert.Null(result.MappedBrand);
        Assert.Empty(result.Suggestions);
    }

    [Fact]
    public async Task ResolveAsync_TenantIsolation_TenantAMappingNotReturnedForTenantB()
    {
        var brandId = Guid.NewGuid();
        var mappingRepo = new FakeMappingRepository();

        mappingRepo.Add(ExternalBrandMapping.Create(
            Guid.NewGuid(),
            TenantA,
            "openfoodfacts",
            "coca cola",
            "Coca-Cola",
            brandId,
            "PRODUCT_CONFIRMED",
            null,
            DateTimeOffset.UtcNow));

        var productRepo = new FakeProductRepository();
        productRepo.AddSelectableBrand(TenantA, new TenantAdminProductBrandOptionResponse(
            brandId, "Coca Cola", "COCA_COLA"));

        var resolver = new TenantExternalBrandResolver(
            mappingRepo,
            productRepo,
            NullLogger<TenantExternalBrandResolver>.Instance);

        var request = new TenantBrandResolutionRequest(
            TenantB,
            "openfoodfacts",
            "coca cola",
            "Coca Cola");

        var result = await resolver.ResolveAsync(request, CancellationToken.None);

        Assert.Null(result.MappedBrand);
        Assert.Empty(result.Suggestions);
    }

    [Fact]
    public async Task ResolveAsync_ProviderIsolation_OpenFoodFactsMappingNotReturnedForUpcItemDb()
    {
        var brandId = Guid.NewGuid();
        var mappingRepo = new FakeMappingRepository();

        mappingRepo.Add(ExternalBrandMapping.Create(
            Guid.NewGuid(),
            TenantA,
            "openfoodfacts",
            "coca cola",
            "Coca-Cola",
            brandId,
            "PRODUCT_CONFIRMED",
            null,
            DateTimeOffset.UtcNow));

        var productRepo = new FakeProductRepository();
        productRepo.AddSelectableBrand(TenantA, new TenantAdminProductBrandOptionResponse(
            brandId, "Coca Cola", "COCA_COLA"));

        var resolver = new TenantExternalBrandResolver(
            mappingRepo,
            productRepo,
            NullLogger<TenantExternalBrandResolver>.Instance);

        var request = new TenantBrandResolutionRequest(
            TenantA,
            "upcitemdb",
            "coca cola",
            "Coca Cola");

        var result = await resolver.ResolveAsync(request, CancellationToken.None);

        // No upcitemdb mapping exists yet -> falls through to suggestions (EXACT, since the
        // brand name itself matches) rather than incorrectly reusing the openfoodfacts mapping.
        Assert.Null(result.MappedBrand);
        Assert.Single(result.Suggestions);
        Assert.Equal("EXACT", result.Suggestions[0].MatchType);
    }

    [Fact]
    public async Task ResolveAsync_EmptyExternalBrandKey_ReturnsEmptyResult()
    {
        var mappingRepo = new FakeMappingRepository();
        var productRepo = new FakeProductRepository();

        var resolver = new TenantExternalBrandResolver(
            mappingRepo,
            productRepo,
            NullLogger<TenantExternalBrandResolver>.Instance);

        var request = new TenantBrandResolutionRequest(
            TenantA,
            "openfoodfacts",
            null,
            null);

        var result = await resolver.ResolveAsync(request, CancellationToken.None);

        Assert.Null(result.MappedBrand);
        Assert.Empty(result.Suggestions);
    }

    private sealed class FakeMappingRepository : IExternalBrandMappingRepository
    {
        private readonly List<ExternalBrandMapping> _mappings = new();

        public void Add(ExternalBrandMapping mapping) => _mappings.Add(mapping);

        public Task<ExternalBrandMapping?> GetAsync(
            Guid tenantId,
            string provider,
            string externalBrandKey,
            CancellationToken cancellationToken)
        {
            var p = provider.Trim().ToLowerInvariant();
            var k = externalBrandKey.Trim().ToLowerInvariant();
            return Task.FromResult(_mappings.FirstOrDefault(
                x => x.TenantId == tenantId && x.Provider == p && x.ExternalBrandKey == k));
        }

        public Task<ExternalBrandMapping> UpsertAsync(
            Guid tenantId,
            string provider,
            string externalBrandKey,
            string externalBrandName,
            Guid tenantBrandId,
            string mappingSource,
            Guid? userId,
            CancellationToken cancellationToken,
            bool saveChanges = true)
        {
            var p = provider.Trim().ToLowerInvariant();
            var k = externalBrandKey.Trim().ToLowerInvariant();
            var existing = _mappings.FirstOrDefault(x => x.TenantId == tenantId && x.Provider == p && x.ExternalBrandKey == k);
            if (existing is not null)
            {
                existing.Update(externalBrandName, tenantBrandId, mappingSource, userId, DateTimeOffset.UtcNow);
                return Task.FromResult(existing);
            }

            var created = ExternalBrandMapping.Create(
                Guid.NewGuid(), tenantId, p, k, externalBrandName, tenantBrandId, mappingSource, userId, DateTimeOffset.UtcNow);
            _mappings.Add(created);
            return Task.FromResult(created);
        }
    }

    private sealed class FakeProductRepository : ITenantAdminProductRepository
    {
        private readonly Dictionary<Guid, List<TenantAdminProductBrandOptionResponse>> _brands = new();
        private readonly Dictionary<(Guid, Guid), bool> _selectability = new();

        public void AddSelectableBrand(Guid tenantId, TenantAdminProductBrandOptionResponse brand)
        {
            if (!_brands.TryGetValue(tenantId, out var list))
            {
                list = new List<TenantAdminProductBrandOptionResponse>();
                _brands[tenantId] = list;
            }
            list.Add(brand);
            _selectability[(tenantId, brand.BrandId)] = true;
        }

        public void SetBrandSelectable(Guid tenantId, Guid brandId, bool selectable)
        {
            _selectability[(tenantId, brandId)] = selectable;
        }

        public Task<bool> BrandBelongsToTenantAsync(Guid tenantId, Guid brandId, CancellationToken cancellationToken)
        {
            if (_selectability.TryGetValue((tenantId, brandId), out var s))
            {
                return Task.FromResult(s);
            }
            return Task.FromResult(false);
        }

        public Task<TenantAdminProductCreateOptionsResponse> GetCreateOptionsAsync(Guid tenantId, CancellationToken cancellationToken)
        {
            var brands = _brands.TryGetValue(tenantId, out var list)
                ? list.Where(b => _selectability.TryGetValue((tenantId, b.BrandId), out var sel) && sel).ToList()
                : new List<TenantAdminProductBrandOptionResponse>();

            return Task.FromResult(new TenantAdminProductCreateOptionsResponse(
                Categories: Array.Empty<TenantAdminProductCategoryOptionResponse>(),
                Brands: brands,
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
        public Task<bool> IsCategoryEffectivelySelectableAsync(Guid tenantId, Guid categoryId, CancellationToken cancellationToken) => Task.FromResult(true);
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
