using E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;
using E_POS.Application.Modules.Platform.PlatformAdmin.Services;
using Xunit;

namespace E_POS.UnitTests.PlatformAdministration;

public sealed class PlatformDashboardMrrCalculatorTests
{
    [Fact]
    public void Calculate_ActiveMonthly_ReturnsRoundedAmount()
    {
        var result = PlatformDashboardMrrCalculator.Calculate(
            [
                new(
                    Guid.NewGuid(),
                    "ACTIVE",
                    "LKR",
                    1000.125m,
                    "monthly",
                    "MONTHLY",
                    null,
                    null,
                    [])
            ],
            new Dictionary<string, PlatformDashboardMrrCalculator.CurrencyMetadata>(StringComparer.OrdinalIgnoreCase)
            {
                ["LKR"] = new("LKR", 2)
            });

        Assert.True(result.Success);
        Assert.Equal(1000.12m, result.Groups[0].Amount);
        Assert.Equal(2, result.Groups[0].DecimalPlaces);
    }

    [Fact]
    public void Calculate_Yearly_NormalizesByTwelve()
    {
        var result = PlatformDashboardMrrCalculator.Calculate(
            [
                new(Guid.NewGuid(), "ACTIVE", "USD", 1200m, "yearly", "YEARLY", null, null, [])
            ],
            new Dictionary<string, PlatformDashboardMrrCalculator.CurrencyMetadata>
            {
                ["USD"] = new("USD", 2)
            });

        Assert.Equal(100m, result.Groups[0].Amount);
    }

    [Fact]
    public void Calculate_PastDueAndTrial_Excluded()
    {
        var result = PlatformDashboardMrrCalculator.Calculate(
            [
                new(Guid.NewGuid(), "PAST_DUE", "LKR", 500m, "monthly", "MONTHLY", null, null, []),
                new(Guid.NewGuid(), "TRIAL", "LKR", 500m, "monthly", "MONTHLY", null, null, [])
            ],
            new Dictionary<string, PlatformDashboardMrrCalculator.CurrencyMetadata>
            {
                ["LKR"] = new("LKR", 2)
            });

        Assert.True(result.Success);
        Assert.Empty(result.Groups);
    }

    [Fact]
    public void Calculate_MissingMetadata_FailsWholeRevenue()
    {
        var result = PlatformDashboardMrrCalculator.Calculate(
            [
                new(Guid.NewGuid(), "ACTIVE", "GBP", 10m, "monthly", "MONTHLY", null, null, [])
            ],
            new Dictionary<string, PlatformDashboardMrrCalculator.CurrencyMetadata>());

        Assert.False(result.Success);
        Assert.Equal(PlatformDashboardErrorCodes.CurrencyMetadataUnavailable, result.ErrorCode);
        Assert.Equal("GBP", result.FailedCurrencyCode);
    }

    [Theory]
    [InlineData(1.225, 2, 1.22)]
    [InlineData(3.5, 0, 4)]
    public void RoundToCurrency_UsesMidpointToEven(decimal input, int places, decimal expected)
    {
        Assert.Equal(expected, PlatformDashboardMrrCalculator.RoundToCurrency(input, places));
    }

    [Fact]
    public void Calculate_PercentDiscount_AppliesToPlanAndAddons()
    {
        var result = PlatformDashboardMrrCalculator.Calculate(
            [
                new(
                    Guid.NewGuid(),
                    "ACTIVE",
                    "LKR",
                    100m,
                    "monthly",
                    "MONTHLY",
                    "percent",
                    10m,
                    [
                        new("ACTIVE", 50m, 1, "LKR", true)
                    ])
            ],
            new Dictionary<string, PlatformDashboardMrrCalculator.CurrencyMetadata>
            {
                ["LKR"] = new("LKR", 2)
            });

        // (100 + 50) - 10% = 135
        Assert.Equal(135m, result.Groups[0].Amount);
    }
}
