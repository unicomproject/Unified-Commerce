using E_POS.Infrastructure.Persistence.Seed;
using Xunit;

namespace E_POS.UnitTests.CatalogProduct;

public sealed class DevelopmentMediaAssetsSeedTests
{
    [Fact]
    public void UpSql_ConvertsSeedImageOwnersToMediaAssets()
    {
        var sql = DevelopmentMediaAssetsSeedData.UpSql;

        Assert.Contains("INSERT INTO media_assets", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("UPDATE product_images", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("UPDATE categories", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("UPDATE brands", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("UPDATE product_option_values", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("UPDATE storefront_banners", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("UPDATE tenant_profiles", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ON CONFLICT (tenant_id, container_name, storage_key) DO UPDATE", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("product_option_template_values", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DownSql_RemovesOnlyDevelopmentMediaAssetLinksBeforeDeletingAssets()
    {
        var sql = DevelopmentMediaAssetsSeedData.DownSql;

        Assert.Contains(DevelopmentMediaAssetsSeedData.DevelopmentTenantId, sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("SET media_asset_id = NULL", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("SET image_media_asset_id = NULL", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("SET logo_media_asset_id = NULL", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("DELETE FROM media_assets", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("container_name = 'legacy-media'", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("product_option_template_values", sql, StringComparison.OrdinalIgnoreCase);
    }
}