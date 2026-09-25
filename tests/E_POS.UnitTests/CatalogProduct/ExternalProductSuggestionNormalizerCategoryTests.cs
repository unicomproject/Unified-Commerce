using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.ExternalLookup;
using E_POS.Application.Modules.Tenant.CatalogProduct.Services;
using Xunit;

namespace E_POS.UnitTests.CatalogProduct;

public sealed class ExternalProductSuggestionNormalizerCategoryTests
{
    [Fact]
    public void NormalizeExternalCategoryKey_TrimsAndNormalizesCasing()
    {
        var raw = "  EN:Carbonated-Drinks  ";
        var normalized = ExternalProductSuggestionNormalizer.NormalizeExternalCategoryKey(raw);
        Assert.Equal("en:carbonated-drinks", normalized);
    }

    [Fact]
    public void NormalizeExternalCategoryKey_PreservesColonAndSpecialChars()
    {
        var raw = "en:colas_diet-zero";
        var normalized = ExternalProductSuggestionNormalizer.NormalizeExternalCategoryKey(raw);
        Assert.Equal("en:colas_diet-zero", normalized);
    }

    [Fact]
    public void NormalizeExternalCategoryKey_StripsControlCharacters()
    {
        var raw = "en:colas\u0000\u0007\r\n";
        var normalized = ExternalProductSuggestionNormalizer.NormalizeExternalCategoryKey(raw);
        Assert.Equal("en:colas", normalized);
    }

    [Fact]
    public void NormalizeExternalCategoryKey_EnforcesMaxLength()
    {
        var raw = new string('a', 300);
        var normalized = ExternalProductSuggestionNormalizer.NormalizeExternalCategoryKey(raw);
        Assert.NotNull(normalized);
        Assert.Equal(ExternalProductSuggestionNormalizer.ExternalCategoryKeyMaxLength, normalized!.Length);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void NormalizeExternalCategoryKey_EmptyOrNull_ReturnsNull(string? value)
    {
        var normalized = ExternalProductSuggestionNormalizer.NormalizeExternalCategoryKey(value);
        Assert.Null(normalized);
    }

    [Fact]
    public void NormalizeExternalCategoryName_TrimsAndStripsControlChars()
    {
        var raw = "  Carbonated Drinks \u0000 \r\n ";
        var normalized = ExternalProductSuggestionNormalizer.NormalizeExternalCategoryName(raw);
        Assert.Equal("Carbonated Drinks", normalized);
    }

    [Fact]
    public void NormalizeExternalCategoryHierarchy_PreservesOrderAndSanitizes()
    {
        var rawHierarchy = new[]
        {
            "  EN:Beverages  ",
            "en:carbonated-drinks",
            "",
            "   ",
            "en:colas\u0000"
        };

        var normalized = ExternalProductSuggestionNormalizer.NormalizeExternalCategoryHierarchy(rawHierarchy);

        Assert.NotNull(normalized);
        Assert.Equal(3, normalized!.Count);
        Assert.Equal("en:beverages", normalized[0]);
        Assert.Equal("en:carbonated-drinks", normalized[1]);
        Assert.Equal("en:colas", normalized[2]);
    }

    [Fact]
    public void NormalizeExternalCategoryHierarchy_NullOrEmpty_ReturnsNull()
    {
        Assert.Null(ExternalProductSuggestionNormalizer.NormalizeExternalCategoryHierarchy(null));
        Assert.Null(ExternalProductSuggestionNormalizer.NormalizeExternalCategoryHierarchy(Array.Empty<string>()));
        Assert.Null(ExternalProductSuggestionNormalizer.NormalizeExternalCategoryHierarchy(new[] { "  ", "" }));
    }

    [Fact]
    public void TryNormalizeFound_PreservesNormalizedCategoryMetadata()
    {
        var request = new ExternalProductLookupRequest("5449000000996", "EAN13", "EAN_13");
        var suggestion = new ExternalProductSuggestion(
            ProductName: "Coca-Cola",
            ShortName: "Coke",
            BrandText: "Coca-Cola",
            CategoryText: "Beverages, Colas",
            UnitText: "330 ml",
            CountryCode: "UK",
            ShortDescription: "Soft drink",
            LongDescription: "Cola soft drink",
            ImageCandidate: "https://example.com/coke.jpg",
            PrimaryGtin: "5449000000996",
            IdentifierStandard: "EAN13",
            ExternalCategoryKey: "  EN:Colas  ",
            ExternalCategoryName: "  Colas  ",
            ExternalCategoryHierarchy: new[] { "en:beverages", "en:colas" });

        var providerResult = new ExternalProductLookupProviderResult(
            ExternalProductLookupStatuses.Found,
            suggestion,
            "openfoodfacts",
            null);

        var ok = ExternalProductSuggestionNormalizer.TryNormalizeFound(
            request,
            providerResult,
            out var normalized,
            out var sourceRef);

        Assert.True(ok);
        Assert.NotNull(normalized);
        Assert.Equal("en:colas", normalized!.ExternalCategoryKey);
        Assert.Equal("Colas", normalized.ExternalCategoryName);
        Assert.NotNull(normalized.ExternalCategoryHierarchy);
        Assert.Equal(2, normalized.ExternalCategoryHierarchy!.Count);
        Assert.Equal("en:beverages", normalized.ExternalCategoryHierarchy[0]);
        Assert.Equal("en:colas", normalized.ExternalCategoryHierarchy[1]);
    }
}
