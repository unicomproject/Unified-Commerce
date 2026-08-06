using E_POS.Infrastructure.Persistence.Seed;
using Xunit;

namespace E_POS.UnitTests.AccessControl;

public sealed class DevelopmentCashierProfileImageSeedTests
{
    [Fact]
    public void Seed_UsesMediaAssetReferenceForDevelopmentCashier()
    {
        Assert.Contains(DevelopmentTenantSeedConstants.CashierEmail, DevelopmentCashierProfileImageSeedData.UpSql);
        Assert.Contains(DevelopmentCashierProfileImageSeedData.ProfileImageUrl, DevelopmentCashierProfileImageSeedData.UpSql);
        Assert.Contains(
            DevelopmentCashierProfileImageSeedData.ProfileImageAssetId.ToString(),
            DevelopmentCashierProfileImageSeedData.UpSql,
            StringComparison.OrdinalIgnoreCase);
        Assert.Contains("INSERT INTO media_assets", DevelopmentCashierProfileImageSeedData.UpSql);
        Assert.Contains("profile_image_url", DevelopmentCashierProfileImageSeedData.UpSql);
    }
}
