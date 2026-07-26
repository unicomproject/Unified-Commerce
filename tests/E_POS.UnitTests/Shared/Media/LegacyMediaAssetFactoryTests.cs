using E_POS.Application.Modules.Shared.Media;
using Xunit;

namespace E_POS.UnitTests.Shared.Media;

public sealed class LegacyMediaAssetFactoryTests
{
    private static readonly Guid TenantId = Guid.Parse("aaaaaaaa-0000-4000-8000-000000000001");
    private static readonly Guid OwnerId = Guid.Parse("bbbbbbbb-0000-4000-8000-000000000001");
    private static readonly Guid UserId = Guid.Parse("cccccccc-0000-4000-8000-000000000001");
    private static readonly Guid MediaAssetId = Guid.Parse("dddddddd-0000-4000-8000-000000000001");
    private static readonly DateTimeOffset Now = new(2026, 7, 24, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void CreateImageFromUrl_WithBlankUrl_ReturnsNull()
    {
        var result = LegacyMediaAssetFactory.CreateImageFromUrl(
            TenantId,
            OwnerId,
            "products",
            "PRODUCT",
            " ",
            UserId,
            Now,
            MediaAssetId);

        Assert.Null(result);
    }

    [Theory]
    [InlineData("https://images.example.test/product.jpeg?width=800", ".jpg", "image/jpeg", "product.jpeg")]
    [InlineData("https://images.example.test/category.png", ".png", "image/png", "category.png")]
    [InlineData("https://images.example.test/brand.webp", ".webp", "image/webp", "brand.webp")]
    [InlineData("https://images.example.test/no-extension", ".jpg", "image/jpeg", "no-extension")]
    public void CreateImageFromUrl_InfersSupportedMetadata(
        string url,
        string expectedExtension,
        string expectedMimeType,
        string expectedOriginalFileName)
    {
        var result = LegacyMediaAssetFactory.CreateImageFromUrl(
            TenantId,
            OwnerId,
            "/products//",
            "product image",
            url,
            UserId,
            Now,
            MediaAssetId);

        Assert.NotNull(result);
        Assert.Equal(MediaAssetId, result!.Id);
        Assert.Equal(TenantId, result.TenantId);
        Assert.Equal("legacy-media", result.ContainerName);
        Assert.StartsWith($"legacy/products/{OwnerId:D}/{MediaAssetId:D}", result.StorageKey, StringComparison.Ordinal);
        Assert.EndsWith(expectedExtension, result.StorageKey, StringComparison.Ordinal);
        Assert.Equal(url, result.PublicUrl);
        Assert.Equal(expectedOriginalFileName, result.OriginalFileName);
        Assert.Equal(expectedMimeType, result.MimeType);
        Assert.Equal(expectedExtension, result.FileExtension);
        Assert.Equal(1, result.FileSizeBytes);
        Assert.Null(result.WidthPx);
        Assert.Null(result.HeightPx);
        Assert.Equal("IMAGE", result.AssetType);
        Assert.Equal("PRODUCT_IMAGE", result.AssetPurpose);
        Assert.Equal("ACTIVE", result.Status);
        Assert.Equal(UserId, result.CreatedByTenantUserId);
    }

    [Fact]
    public void CreateImageFromUrl_TrimsPublicUrlAndNormalizesPurpose()
    {
        var result = LegacyMediaAssetFactory.CreateImageFromUrl(
            TenantId,
            OwnerId,
            "categories",
            "category-hero image",
            "  https://images.example.test/category.jpg  ",
            UserId,
            Now,
            MediaAssetId);

        Assert.NotNull(result);
        Assert.Equal("https://images.example.test/category.jpg", result!.PublicUrl);
        Assert.Equal("CATEGORY_HERO_IMAGE", result.AssetPurpose);
    }
}
