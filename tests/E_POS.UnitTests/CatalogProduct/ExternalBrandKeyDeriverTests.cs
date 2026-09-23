using E_POS.Application.Modules.Tenant.CatalogProduct.Services;
using Xunit;

namespace E_POS.UnitTests.CatalogProduct;

public sealed class ExternalBrandKeyDeriverTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ExtractPrimaryBrandSegment_NullOrBlank_ReturnsNull(string? input)
    {
        Assert.Null(ExternalBrandKeyDeriver.ExtractPrimaryBrandSegment(input));
    }

    [Fact]
    public void ExtractPrimaryBrandSegment_SingleBrand_ReturnsTrimmedValue()
    {
        Assert.Equal("Coca-Cola", ExternalBrandKeyDeriver.ExtractPrimaryBrandSegment("  Coca-Cola  "));
    }

    [Fact]
    public void ExtractPrimaryBrandSegment_MultiValue_ReturnsFirstMeaningfulSegment()
    {
        var result = ExternalBrandKeyDeriver.ExtractPrimaryBrandSegment("Coca-Cola, The Coca-Cola Company");

        Assert.Equal("Coca-Cola", result);
    }

    [Fact]
    public void ExtractPrimaryBrandSegment_LeadingEmptySegments_SkipsToFirstMeaningfulSegment()
    {
        var result = ExternalBrandKeyDeriver.ExtractPrimaryBrandSegment(" , , Nestle, Nestle Lanka");

        Assert.Equal("Nestle", result);
    }

    [Fact]
    public void ExtractPrimaryBrandSegment_AllSegmentsEmpty_ReturnsNull()
    {
        Assert.Null(ExternalBrandKeyDeriver.ExtractPrimaryBrandSegment(" , , , "));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void DeriveExternalBrandKey_NullOrBlank_ReturnsNull(string? input)
    {
        Assert.Null(ExternalBrandKeyDeriver.DeriveExternalBrandKey(input));
    }

    [Fact]
    public void DeriveExternalBrandKey_HyphenatedName_NormalizesToSpaceSeparatedLowercase()
    {
        Assert.Equal("coca cola", ExternalBrandKeyDeriver.DeriveExternalBrandKey("Coca-Cola"));
    }

    [Fact]
    public void DeriveExternalBrandKey_RepeatedWhitespace_Collapses()
    {
        Assert.Equal("coca cola", ExternalBrandKeyDeriver.DeriveExternalBrandKey("Coca   Cola"));
    }

    [Fact]
    public void DeriveExternalBrandKey_MixedCasingAndPunctuation_Normalizes()
    {
        Assert.Equal("the coca cola company", ExternalBrandKeyDeriver.DeriveExternalBrandKey("The Coca-Cola Company!"));
    }

    [Fact]
    public void DeriveExternalBrandKey_OnlyPunctuation_ReturnsNull()
    {
        Assert.Null(ExternalBrandKeyDeriver.DeriveExternalBrandKey("---,,,---"));
    }

    [Fact]
    public void DeriveExternalBrandKey_AlphanumericPreserved()
    {
        Assert.Equal("7 up", ExternalBrandKeyDeriver.DeriveExternalBrandKey("7-Up"));
    }
}
