using E_POS.Infrastructure.Persistence.Seed;
using E_POS.Infrastructure.Persistence.Migrations;
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

    [Fact]
    public void ForwardRepair_ReconcilesOnlyTheCanonicalDevelopmentCashierAsset()
    {
        var sql = ReconcileDevelopmentCashierProfileImageUrl.RepairSql;

        Assert.Contains(DevelopmentTenantSeedConstants.CashierEmail, sql);
        Assert.Contains(
            DevelopmentCashierProfileImageSeedData.ProfileImageAssetId.ToString(),
            sql,
            StringComparison.OrdinalIgnoreCase);
        Assert.Contains(DevelopmentCashierProfileImageSeedData.ProfileImageUrl, sql);
        Assert.Contains("tenant_id = seed_tenant", sql);
        Assert.Contains("public_url = legacy_local_url", sql);
        Assert.Contains("DEVELOPMENT CASHIER PROFILE MEDIA IDENTITY CONFLICT", sql);
    }

    [Fact]
    public void ForwardRepair_IsSchemaNeutralAndDoesNotRestoreTheStaleLocalUrl()
    {
        var sql = ReconcileDevelopmentCashierProfileImageUrl.RepairSql;

        Assert.DoesNotContain("ALTER TABLE", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("CREATE TABLE", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("legacy_local_url", sql, StringComparison.OrdinalIgnoreCase);
    }
}
