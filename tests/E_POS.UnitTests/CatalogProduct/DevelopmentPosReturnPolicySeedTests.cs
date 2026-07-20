using E_POS.Infrastructure.Persistence.Seed;
using Xunit;

namespace E_POS.UnitTests.AccessControl;

public sealed class DevelopmentPosReturnPolicySeedTests
{
    [Fact]
    public void UpSql_CreatesDev14DayPolicyAndAssignsMerProductsOnlyWhenMissing()
    {
        var sql = DevelopmentPosReturnPolicySeedData.UpSql;

        Assert.Contains(DevelopmentPosReturnPolicySeedData.PolicyCode, sql, StringComparison.Ordinal);
        Assert.Contains(DevelopmentPosReturnPolicySeedData.PolicyId, sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("is_default_policy = TRUE", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("product_code LIKE 'MER-%'", sql, StringComparison.Ordinal);
        Assert.Contains("p.return_policy_id IS NULL", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("ON CONFLICT (permission_code)", sql, StringComparison.Ordinal);
        Assert.Contains("WHERE NOT EXISTS", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void DownSql_RemovesOnlyMigrationOwnedPolicyAndMerAssignments()
    {
        var sql = DevelopmentPosReturnPolicySeedData.DownSql;

        Assert.Contains(DevelopmentPosReturnPolicySeedData.PolicyId, sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("return_policy_code = 'DEV-14DAYS'", sql, StringComparison.Ordinal);
        Assert.Contains("product_code LIKE 'MER-%'", sql, StringComparison.Ordinal);
        Assert.Contains("return_policy_id = NULL", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("DELETE FROM products", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DELETE FROM sales_orders", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PolicyConstants_MatchFourteenDayTemplateWindow()
    {
        Assert.Equal(14, DevelopmentPosReturnPolicySeedData.ReturnWindowDays);
        Assert.Equal(14, DevelopmentPosReturnPolicySeedData.ExchangeWindowDays);
        Assert.Equal("DEV-14DAYS", DevelopmentPosReturnPolicySeedData.PolicyCode);
    }
}
