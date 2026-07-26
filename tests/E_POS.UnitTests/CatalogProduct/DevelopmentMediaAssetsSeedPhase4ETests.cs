using E_POS.Infrastructure.Persistence.Seed;
using Xunit;

namespace E_POS.UnitTests.CatalogProduct;

public sealed class DevelopmentMediaAssetsSeedPhase4ETests
{
    [Fact]
    public void UpSql_UsesDeterministicMediaAssetIdsForSeedOwners()
    {
        var sql = DevelopmentMediaAssetsSeedData.UpSql;

        Assert.Contains("md5('media_asset:product_images:'", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("md5('media_asset:categories:'", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("md5('media_asset:brands:'", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("md5('media_asset:product_option_values:'", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("md5('media_asset:storefront_banners:'", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("md5('media_asset:tenant_profiles:'", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void UpSql_KeepsConversionScopedToDevelopmentSeedRows()
    {
        var sql = DevelopmentMediaAssetsSeedData.UpSql;

        Assert.Contains(DevelopmentMediaAssetsSeedData.DevelopmentTenantId, sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("cccc0008-%", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("cccc0026-0001-4000-8000-000000000001", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("dddd0002-%", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("cccc0002-%", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("dddd0001-%", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("tenant_users", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("product_option_template_values", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void UpSql_IsIdempotentOnTenantContainerAndStorageKey()
    {
        var sql = DevelopmentMediaAssetsSeedData.UpSql;

        Assert.Contains("'legacy-media'", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ON CONFLICT (tenant_id, container_name, storage_key) DO UPDATE", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("updated_at = now()", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DownSql_ClearsForeignKeysBeforeDeletingDeterministicSeedMediaAssets()
    {
        var sql = DevelopmentMediaAssetsSeedData.DownSql;
        var clearProductImageIndex = sql.IndexOf("UPDATE product_images", StringComparison.OrdinalIgnoreCase);
        var deleteMediaIndex = sql.IndexOf("DELETE FROM media_assets", StringComparison.OrdinalIgnoreCase);

        Assert.True(clearProductImageIndex >= 0);
        Assert.True(deleteMediaIndex > clearProductImageIndex);
        Assert.Contains("'legacy-media'", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(DevelopmentMediaAssetsSeedData.DevelopmentTenantId, sql, StringComparison.OrdinalIgnoreCase);
    }
}
