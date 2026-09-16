using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.ExternalLookup;
using E_POS.Application.Modules.Tenant.CatalogProduct.Services;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;
using Xunit;

namespace E_POS.UnitTests.CatalogProduct;

public sealed class ExternalProductSuggestionNormalizerTests
{
    [Fact]
    public void TryNormalizeFound_MapsSupportedFields_AsSuggestionTextOnly()
    {
        var request = new ExternalProductLookupRequest("4006381333931", "GTIN13", "EAN13");
        var providerResult = new ExternalProductLookupProviderResult(
            ExternalProductLookupStatuses.Found,
            new ExternalProductSuggestion(
                "  Cola  ",
                "Short",
                "Coca-Cola",
                "Beverages",
                "330ml",
                "US",
                "Short desc",
                "Long desc",
                "https://cdn.example/cola.png",
                "4006381333931",
                "GTIN13"),
            "src-9",
            null);

        Assert.True(ExternalProductSuggestionNormalizer.TryNormalizeFound(
            request, providerResult, out var suggestion, out var sourceReference));

        Assert.Equal("Cola", suggestion.ProductName);
        Assert.Equal("Coca-Cola", suggestion.BrandText);
        Assert.Equal("Beverages", suggestion.CategoryText);
        Assert.Equal("330ml", suggestion.UnitText);
        Assert.Equal("https://cdn.example/cola.png", suggestion.ImageCandidate);
        Assert.Equal("src-9", sourceReference);
        // No brandId/categoryId/uomId on model — master-data safety by design.
        Assert.DoesNotContain("BrandId", suggestion.GetType().GetProperties().Select(p => p.Name));
    }

    [Fact]
    public void TryNormalizeFound_NullSuggestion_ReturnsFalse()
    {
        var request = new ExternalProductLookupRequest("4006381333931", "GTIN13", null);
        var providerResult = new ExternalProductLookupProviderResult(
            ExternalProductLookupStatuses.Found, null, null, null);

        Assert.False(ExternalProductSuggestionNormalizer.TryNormalizeFound(
            request, providerResult, out _, out _));
    }

    [Fact]
    public void TryNormalizeFound_UsesRequestIdentifierWhenProviderOmitsGtin()
    {
        var request = new ExternalProductLookupRequest("04006381333931", "GTIN14", "UNKNOWN");
        var providerResult = new ExternalProductLookupProviderResult(
            ExternalProductLookupStatuses.Found,
            new ExternalProductSuggestion(
                "Juice", null, null, null, null, null, null, null, null, null, null),
            null,
            null);

        Assert.True(ExternalProductSuggestionNormalizer.TryNormalizeFound(
            request, providerResult, out var suggestion, out _));
        Assert.Equal("04006381333931", suggestion.PrimaryGtin);
        Assert.True(suggestion.ProductName!.Length <= ProductConstants.ProductNameMaxLength);
    }
}
