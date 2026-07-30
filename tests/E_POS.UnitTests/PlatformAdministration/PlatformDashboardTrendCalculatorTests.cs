using E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;
using E_POS.Application.Modules.Platform.PlatformAdmin.Services;
using Xunit;

namespace E_POS.UnitTests.PlatformAdministration;

public sealed class PlatformDashboardTrendCalculatorTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Not/A/Real/Timezone")]
    public void TryGetTimeZone_Invalid_ReturnsTimezoneUnavailable(string? timezoneId)
    {
        var ok = PlatformDashboardTrendCalculator.TryGetTimeZone(timezoneId, out var tz, out var error);

        Assert.False(ok);
        Assert.Null(tz);
        Assert.Equal(PlatformDashboardErrorCodes.TimezoneUnavailable, error);
    }

    [Fact]
    public void TryGetTimeZone_Valid_Resolves()
    {
        var ok = PlatformDashboardTrendCalculator.TryGetTimeZone("Asia/Colombo", out var tz, out var error);

        Assert.True(ok);
        Assert.NotNull(tz);
        Assert.Null(error);
    }
}
