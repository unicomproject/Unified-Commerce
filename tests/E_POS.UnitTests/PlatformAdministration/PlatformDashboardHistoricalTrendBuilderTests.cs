using E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;
using E_POS.Application.Modules.Platform.PlatformAdmin.Services;
using E_POS.Domain.Modules.Platform.Subscription.Constants;
using Xunit;

namespace E_POS.UnitTests.PlatformAdministration;

public sealed class PlatformDashboardHistoricalTrendBuilderTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 15, 12, 0, 0, TimeSpan.Zero);
    private static readonly TimeZoneInfo Colombo = TimeZoneInfo.FindSystemTimeZoneById(
        OperatingSystem.IsWindows() ? "Sri Lanka Standard Time" : "Asia/Colombo");

    [Fact]
    public void BuildMrrSeries_UsesDailyActiveHistoryWithoutFabrication()
    {
        var subId = Guid.NewGuid();
        var states = new List<PlatformDashboardHistoricalTrendBuilder.SubscriptionHistoryState>
        {
            new(
                subId,
                "LKR",
                1200m,
                "monthly",
                "MONTHLY",
                null,
                null,
                "ACTIVE",
                Now.AddDays(-40),
                Now.AddDays(-40),
                [],
                [
                    new(subId, TenantSubscriptionHistoryChangeTypeConstants.StatusChanged, Now.AddDays(-40), null, "ACTIVE", null)
                ])
        };

        var currencies = new Dictionary<string, PlatformDashboardMrrCalculator.CurrencyMetadata>
        {
            ["LKR"] = new("LKR", 2)
        };

        var result = PlatformDashboardHistoricalTrendBuilder.BuildMrrSeries(states, currencies, Now, Colombo);
        Assert.True(result.Success);
        Assert.Single(result.Series);
        Assert.True(result.Series[0].Points.Count > 1);
        Assert.All(result.Series[0].Points, p => Assert.Equal(1200m, p.Value));
    }

    [Fact]
    public void BuildMrrSeries_UnpricedPlanChange_IsIncomplete()
    {
        var subId = Guid.NewGuid();
        var states = new List<PlatformDashboardHistoricalTrendBuilder.SubscriptionHistoryState>
        {
            new(
                subId,
                "LKR",
                900m,
                "monthly",
                "MONTHLY",
                null,
                null,
                "ACTIVE",
                Now.AddDays(-10),
                Now.AddDays(-10),
                [],
                [
                    new(subId, TenantSubscriptionHistoryChangeTypeConstants.PlanChanged, Now.AddDays(-5), null, null, null)
                ])
        };

        var result = PlatformDashboardHistoricalTrendBuilder.BuildMrrSeries(
            states,
            new Dictionary<string, PlatformDashboardMrrCalculator.CurrencyMetadata> { ["LKR"] = new("LKR", 2) },
            Now,
            Colombo);

        Assert.False(result.Success);
        Assert.Equal(PlatformDashboardErrorCodes.MrrHistoryIncomplete, result.ErrorCode);
    }
}
