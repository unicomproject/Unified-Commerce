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
        Assert.Equal("LEAF_EXACT", result.Suggestions[0].MatchType);
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
        Assert.Equal("LEAF_EXACT", result.Suggestions[0].MatchType);
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
        Assert.Equal("LEAF_NORMALIZED", result.Suggestions[0].MatchType);
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
        Assert.Equal("LEAF_SIMILARITY", result.Suggestions[0].MatchType);
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

    [Fact]
    public async Task ResolveAsync_AccentedExternalCategoryVsUnaccentedTenantCategory_ReturnsNormalizedSuggestion()
    {
        // Regression test mirroring the Brand-side diacritic fix: an accented external category
        // display name should match an unaccented tenant category name at the NORMALIZED tier.
        var categoryId = Guid.NewGuid();
        var mappingRepo = new FakeMappingRepository();
        var productRepo = new FakeProductRepository();

        productRepo.AddSelectableCategory(TenantA, new TenantAdminProductCategoryOptionResponse(
            categoryId, "CAT-CAFE", "Cafe", null, 1, "Cafe", false, 0));

        var resolver = new TenantExternalCategoryResolver(
            mappingRepo,
            productRepo,
            NullLogger<TenantExternalCategoryResolver>.Instance);

        var request = new TenantCategoryResolutionRequest(
            TenantA,
            "openfoodfacts",
            "en:cafe",
            "Café");

        var result = await resolver.ResolveAsync(request, CancellationToken.None);

        Assert.Null(result.MappedCategory);
        Assert.Single(result.Suggestions);
        Assert.Equal(categoryId, result.Suggestions[0].Id);
        Assert.Equal("LEAF_NORMALIZED", result.Suggestions[0].MatchType);
    }

    [Fact]
    public async Task ResolveAsync_RealBarcode7613032655495InstantCoffeeCategory_NoTenantCategoryTextuallyMatches_ReturnsZeroSuggestions()
    {
        // Regression fixture for real barcode 7613032655495 (Nestlé Ricoré instant coffee substitute).
        // OpenFoodFacts' leaf category name/key have no textual/token overlap with a generic grocery
        // tenant catalogue (Beverages, Groceries, Snacks, ...). This is a genuine weak semantic gap —
        // not a resolver bug — and the resolver must not force an auto-map in this situation.
        var mappingRepo = new FakeMappingRepository();
        var productRepo = new FakeProductRepository();

        productRepo.AddSelectableCategory(TenantA, new TenantAdminProductCategoryOptionResponse(
            Guid.NewGuid(), "BEVERAGES", "Beverages", null, 1, "Beverages", false, 0));
        productRepo.AddSelectableCategory(TenantA, new TenantAdminProductCategoryOptionResponse(
            Guid.NewGuid(), "GROCERIES", "Groceries", null, 1, "Groceries", false, 1));
        productRepo.AddSelectableCategory(TenantA, new TenantAdminProductCategoryOptionResponse(
            Guid.NewGuid(), "SNACKS", "Snacks", null, 1, "Snacks", false, 2));

        var resolver = new TenantExternalCategoryResolver(
            mappingRepo,
            productRepo,
            NullLogger<TenantExternalCategoryResolver>.Instance);

        var request = new TenantCategoryResolutionRequest(
            TenantA,
            "openfoodfacts",
            "en:instant-mix-of-chicory-and-coffee-powder",
            "Chicorée et café en poudre soluble");

        var result = await resolver.ResolveAsync(request, CancellationToken.None);

        Assert.Null(result.MappedCategory);
        Assert.Empty(result.Suggestions);
    }

    // --- Category hierarchy-aware resolver enhancement ---------------------------------------
    // Real-shape fixture: OpenFoodFacts Coca-Cola can (barcode 5449000000996).
    // categories_hierarchy = [en:beverages-and-beverages-preparations, en:beverages,
    //   en:non-alcoholic-beverages, en:carbonated-drinks, en:soft-drinks, en:sodas, en:colas].
    // Leaf ExternalCategoryName is "Colas"; tenant only has a generic "Beverages" category, which
    // has no textual overlap with "Colas" and was therefore never suggested before this enhancement.

    private static readonly IReadOnlyList<string> CocaColaHierarchy = new[]
    {
        "en:beverages-and-beverages-preparations",
        "en:beverages",
        "en:non-alcoholic-beverages",
        "en:carbonated-drinks",
        "en:soft-drinks",
        "en:sodas",
        "en:colas",
    };

    [Fact]
    public async Task ResolveAsync_RealCocaColaHierarchy_TenantHasOnlyBeverages_ReturnsHierarchyExactSuggestion()
    {
        // Required real-shape regression (§24 of the hierarchy-aware resolver spec).
        var beveragesId = Guid.NewGuid();
        var mappingRepo = new FakeMappingRepository();
        var productRepo = new FakeProductRepository();

        productRepo.AddSelectableCategory(TenantA, new TenantAdminProductCategoryOptionResponse(
            beveragesId, "BEVERAGES", "Beverages", null, 1, "Beverages", false, 0));

        var resolver = new TenantExternalCategoryResolver(
            mappingRepo, productRepo, NullLogger<TenantExternalCategoryResolver>.Instance);

        var request = new TenantCategoryResolutionRequest(
            TenantA, "openfoodfacts", "en:colas", "Colas", CocaColaHierarchy);

        var result = await resolver.ResolveAsync(request, CancellationToken.None);

        // §6/§10: a hierarchy match is a suggestion only, never auto-selected/persisted.
        Assert.Null(result.MappedCategory);
        Assert.Single(result.Suggestions);
        Assert.Equal(beveragesId, result.Suggestions[0].Id);
        Assert.Equal("Beverages", result.Suggestions[0].Name);
        Assert.Equal("HIERARCHY_EXACT", result.Suggestions[0].MatchType);
    }

    [Fact]
    public async Task ResolveAsync_HierarchyMatch_DoesNotSuggestUnrelatedBroadCategory()
    {
        // §20/§6: textual hierarchy matching only — a tenant category that is broadly related in
        // meaning ("Food") but never actually appears as hierarchy text must NOT be suggested.
        var beveragesId = Guid.NewGuid();
        var mappingRepo = new FakeMappingRepository();
        var productRepo = new FakeProductRepository();

        productRepo.AddSelectableCategory(TenantA, new TenantAdminProductCategoryOptionResponse(
            beveragesId, "BEVERAGES", "Beverages", null, 1, "Beverages", false, 0));
        productRepo.AddSelectableCategory(TenantA, new TenantAdminProductCategoryOptionResponse(
            Guid.NewGuid(), "FOOD", "Food", null, 1, "Food", false, 1));

        var resolver = new TenantExternalCategoryResolver(
            mappingRepo, productRepo, NullLogger<TenantExternalCategoryResolver>.Instance);

        var request = new TenantCategoryResolutionRequest(
            TenantA, "openfoodfacts", "en:colas", "Colas", CocaColaHierarchy);

        var result = await resolver.ResolveAsync(request, CancellationToken.None);

        Assert.Single(result.Suggestions);
        Assert.Equal(beveragesId, result.Suggestions[0].Id);
        Assert.DoesNotContain(result.Suggestions, s => s.Name == "Food");
    }

    [Fact]
    public async Task ResolveAsync_HierarchyNormalizedMatch_PunctuationDifference_ReturnsHierarchyNormalizedSuggestion()
    {
        var softDrinksId = Guid.NewGuid();
        var mappingRepo = new FakeMappingRepository();
        var productRepo = new FakeProductRepository();

        // Tenant category has punctuation the raw hierarchy tag doesn't ("Soft-Drinks" vs "en:soft-drinks"
        // deriving to "soft drinks" — matches only after StripPunctuation, not raw equality).
        productRepo.AddSelectableCategory(TenantA, new TenantAdminProductCategoryOptionResponse(
            softDrinksId, "CAT-SOFT-DRINKS", "Soft-Drinks!", null, 1, "Soft-Drinks!", false, 0));

        var resolver = new TenantExternalCategoryResolver(
            mappingRepo, productRepo, NullLogger<TenantExternalCategoryResolver>.Instance);

        // Leaf is "Colas" (no textual relation to "Soft-Drinks!"), so only the hierarchy tier can match.
        var request = new TenantCategoryResolutionRequest(
            TenantA, "openfoodfacts", "en:colas", "Colas", CocaColaHierarchy);

        var result = await resolver.ResolveAsync(request, CancellationToken.None);

        Assert.Single(result.Suggestions);
        Assert.Equal(softDrinksId, result.Suggestions[0].Id);
        Assert.Equal("HIERARCHY_NORMALIZED", result.Suggestions[0].MatchType);
    }

    [Fact]
    public async Task ResolveAsync_HierarchyDiacriticMatch_ReturnsHierarchyNormalizedSuggestion()
    {
        // Accent-folding parity for hierarchy nodes (§19), mirroring the Brand/leaf-category fix.
        var cafeId = Guid.NewGuid();
        var mappingRepo = new FakeMappingRepository();
        var productRepo = new FakeProductRepository();

        productRepo.AddSelectableCategory(TenantA, new TenantAdminProductCategoryOptionResponse(
            cafeId, "CAT-CAFE", "Cafe", null, 1, "Cafe", false, 0));

        var resolver = new TenantExternalCategoryResolver(
            mappingRepo, productRepo, NullLogger<TenantExternalCategoryResolver>.Instance);

        var hierarchy = new[] { "en:beverages", "en:café" };
        var request = new TenantCategoryResolutionRequest(
            TenantA, "openfoodfacts", "en:instant-coffees", "Instant Coffees", hierarchy);

        var result = await resolver.ResolveAsync(request, CancellationToken.None);

        Assert.Single(result.Suggestions);
        Assert.Equal(cafeId, result.Suggestions[0].Id);
        Assert.Equal("HIERARCHY_NORMALIZED", result.Suggestions[0].MatchType);
    }

    [Fact]
    public async Task ResolveAsync_MultipleHierarchyMatches_ReturnsDeepestFirst_UpToMax3()
    {
        // §7: deepest/most-specific tenant category match must be returned first.
        var beveragesId = Guid.NewGuid();
        var carbonatedId = Guid.NewGuid();
        var sodasId = Guid.NewGuid();
        var mappingRepo = new FakeMappingRepository();
        var productRepo = new FakeProductRepository();

        productRepo.AddSelectableCategory(TenantA, new TenantAdminProductCategoryOptionResponse(
            beveragesId, "BEVERAGES", "Beverages", null, 1, "Beverages", false, 0));
        productRepo.AddSelectableCategory(TenantA, new TenantAdminProductCategoryOptionResponse(
            carbonatedId, "CARBONATED", "Carbonated Drinks", null, 1, "Carbonated Drinks", false, 1));
        productRepo.AddSelectableCategory(TenantA, new TenantAdminProductCategoryOptionResponse(
            sodasId, "SODAS", "Sodas", null, 1, "Sodas", false, 2));

        var resolver = new TenantExternalCategoryResolver(
            mappingRepo, productRepo, NullLogger<TenantExternalCategoryResolver>.Instance);

        // Leaf is "Colas" (no tenant category named Colas), so all 3 matches come from the hierarchy.
        var request = new TenantCategoryResolutionRequest(
            TenantA, "openfoodfacts", "en:colas", "Colas", CocaColaHierarchy);

        var result = await resolver.ResolveAsync(request, CancellationToken.None);

        Assert.Equal(3, result.Suggestions.Count);
        Assert.Equal(sodasId, result.Suggestions[0].Id);
        Assert.Equal(carbonatedId, result.Suggestions[1].Id);
        Assert.Equal(beveragesId, result.Suggestions[2].Id);
        Assert.All(result.Suggestions, s => Assert.Equal("HIERARCHY_EXACT", s.MatchType));
    }

    [Fact]
    public async Task ResolveAsync_HierarchyMatch_ExcludesInactiveCategory()
    {
        // §9: only ACTIVE/selectable tenant categories may be suggested via hierarchy matching.
        var inactiveId = Guid.NewGuid();
        var mappingRepo = new FakeMappingRepository();
        var productRepo = new FakeProductRepository();

        productRepo.AddSelectableCategory(TenantA, new TenantAdminProductCategoryOptionResponse(
            inactiveId, "BEVERAGES", "Beverages", null, 1, "Beverages", false, 0));
        productRepo.SetCategorySelectable(TenantA, inactiveId, false);

        var resolver = new TenantExternalCategoryResolver(
            mappingRepo, productRepo, NullLogger<TenantExternalCategoryResolver>.Instance);

        var request = new TenantCategoryResolutionRequest(
            TenantA, "openfoodfacts", "en:colas", "Colas", CocaColaHierarchy);

        var result = await resolver.ResolveAsync(request, CancellationToken.None);

        Assert.Empty(result.Suggestions);
    }

    [Fact]
    public async Task ResolveAsync_SavedMapping_StillWinsOverHierarchyMatch()
    {
        // §10: a saved mapping to one category must not be displaced just because a hierarchy node
        // also matches a different tenant category.
        var mappedId = Guid.NewGuid();
        var beveragesId = Guid.NewGuid();
        var mappingRepo = new FakeMappingRepository();
        mappingRepo.Add(ExternalCategoryMapping.Create(
            Guid.NewGuid(), TenantA, "openfoodfacts", "en:colas", "Colas",
            mappedId, "PRODUCT_CONFIRMED", null, DateTimeOffset.UtcNow));

        var productRepo = new FakeProductRepository();
        productRepo.AddSelectableCategory(TenantA, new TenantAdminProductCategoryOptionResponse(
            mappedId, "CAT-SOFT-DRINKS", "Soft Drinks", null, 1, "Soft Drinks", false, 0));
        productRepo.AddSelectableCategory(TenantA, new TenantAdminProductCategoryOptionResponse(
            beveragesId, "BEVERAGES", "Beverages", null, 1, "Beverages", false, 1));

        var resolver = new TenantExternalCategoryResolver(
            mappingRepo, productRepo, NullLogger<TenantExternalCategoryResolver>.Instance);

        var request = new TenantCategoryResolutionRequest(
            TenantA, "openfoodfacts", "en:colas", "Colas", CocaColaHierarchy);

        var result = await resolver.ResolveAsync(request, CancellationToken.None);

        Assert.NotNull(result.MappedCategory);
        Assert.Equal(mappedId, result.MappedCategory!.Id);
        Assert.Empty(result.Suggestions);
    }

    [Fact]
    public async Task ResolveAsync_LeafExactMatch_RanksBeforeHierarchyMatch_AndBothAppear()
    {
        // §8: leaf tiers are stronger than hierarchy tiers and must be ordered first.
        var colasId = Guid.NewGuid();
        var beveragesId = Guid.NewGuid();
        var mappingRepo = new FakeMappingRepository();
        var productRepo = new FakeProductRepository();

        productRepo.AddSelectableCategory(TenantA, new TenantAdminProductCategoryOptionResponse(
            colasId, "CAT-COLAS", "Colas", null, 1, "Colas", false, 0));
        productRepo.AddSelectableCategory(TenantA, new TenantAdminProductCategoryOptionResponse(
            beveragesId, "BEVERAGES", "Beverages", null, 1, "Beverages", false, 1));

        var resolver = new TenantExternalCategoryResolver(
            mappingRepo, productRepo, NullLogger<TenantExternalCategoryResolver>.Instance);

        var request = new TenantCategoryResolutionRequest(
            TenantA, "openfoodfacts", "en:colas", "Colas", CocaColaHierarchy);

        var result = await resolver.ResolveAsync(request, CancellationToken.None);

        Assert.Equal(2, result.Suggestions.Count);
        Assert.Equal(colasId, result.Suggestions[0].Id);
        Assert.Equal("LEAF_EXACT", result.Suggestions[0].MatchType);
        Assert.Equal(beveragesId, result.Suggestions[1].Id);
        Assert.Equal("HIERARCHY_EXACT", result.Suggestions[1].MatchType);
    }

    [Fact]
    public async Task ResolveAsync_HierarchyExactMatch_DedupesAgainstWeakerLeafSimilarityMatch()
    {
        // §8: a category matched by both a strong hierarchy tier and a weaker leaf tier must be
        // returned exactly once, tagged with the stronger match type.
        var beveragesId = Guid.NewGuid();
        var mappingRepo = new FakeMappingRepository();
        var productRepo = new FakeProductRepository();

        productRepo.AddSelectableCategory(TenantA, new TenantAdminProductCategoryOptionResponse(
            beveragesId, "BEVERAGES", "Beverages", null, 1, "Beverages", false, 0));

        var resolver = new TenantExternalCategoryResolver(
            mappingRepo, productRepo, NullLogger<TenantExternalCategoryResolver>.Instance);

        // Leaf name "Drinks And Beverages" token-overlaps "Beverages" (LEAF_SIMILARITY), while the
        // hierarchy also contains "en:beverages" (HIERARCHY_EXACT) for the very same tenant category.
        var request = new TenantCategoryResolutionRequest(
            TenantA, "openfoodfacts", "en:colas", "Drinks And Beverages", CocaColaHierarchy);

        var result = await resolver.ResolveAsync(request, CancellationToken.None);

        Assert.Single(result.Suggestions);
        Assert.Equal(beveragesId, result.Suggestions[0].Id);
        Assert.Equal("HIERARCHY_EXACT", result.Suggestions[0].MatchType);
    }

    [Fact]
    public async Task ResolveAsync_HierarchySimilarityMatch_ConservativeTokenOverlap()
    {
        var carbonatedId = Guid.NewGuid();
        var mappingRepo = new FakeMappingRepository();
        var productRepo = new FakeProductRepository();

        // Tenant's category name is a near-variant ("Carbonated Drink", singular) of the hierarchy
        // node "en:carbonated-drinks" -> "carbonated drinks" — token overlap, not an exact/normalized match.
        productRepo.AddSelectableCategory(TenantA, new TenantAdminProductCategoryOptionResponse(
            carbonatedId, "CAT-CARB", "Carbonated Drink", null, 1, "Carbonated Drink", false, 0));

        var resolver = new TenantExternalCategoryResolver(
            mappingRepo, productRepo, NullLogger<TenantExternalCategoryResolver>.Instance);

        var request = new TenantCategoryResolutionRequest(
            TenantA, "openfoodfacts", "en:colas", "Colas", CocaColaHierarchy);

        var result = await resolver.ResolveAsync(request, CancellationToken.None);

        Assert.Single(result.Suggestions);
        Assert.Equal(carbonatedId, result.Suggestions[0].Id);
        Assert.Equal("HIERARCHY_SIMILARITY", result.Suggestions[0].MatchType);
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
