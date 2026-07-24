using E_POS.Application.Modules.Shared.Media;
using Xunit;

namespace E_POS.UnitTests.Shared.Media;

public sealed class MediaUrlResolverTests
{
    [Fact]
    public void PreferMediaAsset_WhenMediaPublicUrlExists_ReturnsTrimmedMediaUrl()
    {
        var result = MediaUrlResolver.PreferMediaAsset(
            "  https://cdn.example.test/media/product.jpg  ",
            "https://legacy.example.test/product.jpg",
            "legacy/products/product.jpg");

        Assert.Equal("https://cdn.example.test/media/product.jpg", result);
    }

    [Fact]
    public void PreferMediaAsset_WhenMediaPublicUrlMissing_ReturnsTrimmedLegacyUrl()
    {
        var result = MediaUrlResolver.PreferMediaAsset(
            " ",
            "  https://legacy.example.test/product.jpg  ",
            "legacy/products/product.jpg");

        Assert.Equal("https://legacy.example.test/product.jpg", result);
    }

    [Fact]
    public void PreferMediaAsset_WhenMediaAndLegacyUrlMissing_ReturnsTrimmedStorageKey()
    {
        var result = MediaUrlResolver.PreferMediaAsset(
            null,
            " ",
            "  legacy/products/product.jpg  ");

        Assert.Equal("legacy/products/product.jpg", result);
    }

    [Fact]
    public void PreferMediaAsset_WhenAllSourcesMissing_ReturnsNull()
    {
        var result = MediaUrlResolver.PreferMediaAsset(null, " ", "\t");

        Assert.Null(result);
    }

    [Fact]
    public void PreferMediaAssetOrEmpty_WhenAllSourcesMissing_ReturnsEmptyString()
    {
        var result = MediaUrlResolver.PreferMediaAssetOrEmpty(null, null, null);

        Assert.Equal(string.Empty, result);
    }
}
