using E_POS.Infrastructure.Persistence.Seed;
using Xunit;

namespace E_POS.UnitTests.AccessControl;

public sealed class DevelopmentPosDashboardIdentitySeedTests
{
    [Fact]
    public void Seed_UsesExistingBrandingAndCashierColumns()
    {
        Assert.Contains("INSERT INTO media_assets", DevelopmentPosDashboardIdentitySeedData.UpSql);
        Assert.Contains("logo_media_asset_id", DevelopmentPosDashboardIdentitySeedData.UpSql);
        Assert.Contains("full_name = 'Kavin'", DevelopmentPosDashboardIdentitySeedData.UpSql);
        Assert.Contains("display_name = 'Kavin'", DevelopmentPosDashboardIdentitySeedData.UpSql);
        Assert.Contains(
            DevelopmentPosDashboardIdentitySeedData.BrandingLogoPublicUrl,
            DevelopmentPosDashboardIdentitySeedData.UpSql);
    }
}
