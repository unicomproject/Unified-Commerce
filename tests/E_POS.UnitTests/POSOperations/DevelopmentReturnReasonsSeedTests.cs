using E_POS.Infrastructure.Persistence.Seed;
using Xunit;

namespace E_POS.UnitTests.POSOperations;

public sealed class DevelopmentReturnReasonsSeedTests
{
    [Fact]
    public void UpSql_IsTenantScopedIdempotentAndCustomizationSafe()
    {
        var sql = DevelopmentReturnReasonsSeedData.UpSql;
        var tenantId = DevelopmentTenantSeedConstants.DevelopmentTenantId.ToString();

        Assert.Contains(tenantId, sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ON CONFLICT (tenant_id, reason_code) DO NOTHING", sql);
        Assert.DoesNotContain("DO UPDATE", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DELETE FROM return_reasons", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void UpSql_ContainsUniqueOrderedActiveCanonicalReasons()
    {
        var sql = DevelopmentReturnReasonsSeedData.UpSql;
        var codes = new[]
        {
            "DAMAGED",
            "WRONG_ITEM",
            "SIZE_ISSUE",
            "CHANGED_MIND",
            "DEFECTIVE",
            "OTHER",
        };

        foreach (var code in codes)
        {
            Assert.Equal(1, CountOccurrences(sql, $"'{code}'"));
        }

        Assert.Contains("true, 0, now(), now()", sql);
        Assert.Contains("true, 5, now(), now()", sql);
        Assert.Contains("'BOTH'", sql);
    }

    [Fact]
    public void DownSql_DoesNotDeletePotentialTenantCustomizations()
    {
        Assert.DoesNotContain(
            "DELETE",
            DevelopmentReturnReasonsSeedData.DownSql,
            StringComparison.OrdinalIgnoreCase);
    }

    private static int CountOccurrences(string value, string search)
    {
        var count = 0;
        var index = 0;
        while ((index = value.IndexOf(search, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += search.Length;
        }

        return count;
    }
}
