using E_POS.Application.Modules.Tenant.CatalogProduct.Validators;
using Xunit;

namespace E_POS.UnitTests.CatalogProduct;

public sealed class ProductSkuCandidateGeneratorTests
{
    [Theory]
    [InlineData("TSH", 125, "TSH-000125")]
    [InlineData(" bev ", 128, "BEV-000128")]
    [InlineData("LAP-TOP", 1, "LAP-TOP-000001")]
    public void BuildProductBase_UsesCategoryAndSixDigitTenantSequence(
        string categoryCode,
        long sequence,
        string expected)
    {
        Assert.Equal(expected, ProductSkuCandidateGenerator.BuildProductBase(categoryCode, sequence));
    }

    [Theory]
    [InlineData("TSH-000125", new[] { "BLK", "S" }, "TSH-000125-BLK-S")]
    [InlineData("LAP-000127", new[] { "i7", "16gb", "512gb" }, "LAP-000127-I7-16GB-512GB")]
    public void BuildVariantSku_AppendsDynamicOrderedValueCodes(
        string productBase,
        string[] valueCodes,
        string expected)
    {
        Assert.Equal(
            expected,
            ProductSkuCandidateGenerator.BuildVariantSku(productBase, valueCodes));
    }

    [Theory]
    [InlineData("")]
    [InlineData("BLACK SHIRT")]
    [InlineData("BLK_1")]
    [InlineData("-BLK")]
    [InlineData("BLK-")]
    public void BuildVariantSku_InvalidValueCode_FailsClearly(string valueCode)
    {
        Assert.Throws<ArgumentException>(() =>
            ProductSkuCandidateGenerator.BuildVariantSku("TSH-000125", [valueCode]));
    }

    [Fact]
    public void MatchesCategory_UsesPersistedCategoryToken()
    {
        Assert.True(ProductSkuCandidateGenerator.MatchesCategory("TSH-000125", "tsh"));
        Assert.False(ProductSkuCandidateGenerator.MatchesCategory("TSH-000125", "BEV"));
    }
}
