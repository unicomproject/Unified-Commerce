using E_POS.Infrastructure.Persistence.Seed;
using Xunit;

namespace E_POS.UnitTests.POSOperations;

public sealed class PlatformSalesChannelSeedTests
{
    [Fact]
    public void CanonicalPosDefinition_UsesReservedDeterministicIdentity()
    {
        Assert.Equal(Guid.Parse("d0000000-0000-4000-8000-000000000003"),
            PlatformSalesChannelSeedConstants.PosChannelId);
        Assert.Equal("POS", PlatformSalesChannelSeedConstants.PosChannelCode);
        Assert.Equal("Point of Sale", PlatformSalesChannelSeedConstants.PosChannelName);
        Assert.Equal("POS", PlatformSalesChannelSeedConstants.PosChannelType);
    }

    [Fact]
    public void SeedSql_IsIdempotentAndPreservesExistingPlatformChannels()
    {
        var sql = PlatformSalesChannelSeedData.UpSql;

        Assert.Contains("'PHYSICAL', 'Physical Store', 'PHYSICAL'", sql);
        Assert.Contains("'ONLINE', 'E-Commerce', 'ONLINE'", sql);
        Assert.Contains("ON CONFLICT (id) DO UPDATE", sql);
        Assert.DoesNotContain("'POS', 'Point of Sale', 'POS'", sql);
    }

    [Fact]
    public void RemediationSql_RejectsConflictsAndUpsertsCanonicalRow()
    {
        var sql = PlatformSalesChannelSeedData.CanonicalPosUpSql;

        Assert.Contains("channel_type = 'POS' OR channel_code = 'POS'", sql);
        Assert.Contains("RAISE EXCEPTION", sql);
        Assert.Contains(PlatformSalesChannelSeedConstants.PosChannelId.ToString(),
            sql, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, Count(sql, "INSERT INTO platform_sales_channels"));
        Assert.Contains("ON CONFLICT (id) DO UPDATE", sql);
        Assert.DoesNotContain("DELETE FROM", sql, StringComparison.OrdinalIgnoreCase);
    }

    private static int Count(string value, string fragment)
    {
        var count = 0;
        var start = 0;
        while ((start = value.IndexOf(fragment, start, StringComparison.Ordinal)) >= 0)
        {
            count++;
            start += fragment.Length;
        }

        return count;
    }
}
