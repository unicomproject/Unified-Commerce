using E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;
using E_POS.Application.Modules.Platform.PlatformAdmin.Services;
using Xunit;

namespace E_POS.UnitTests.PlatformAdministration;

public sealed class PlatformDashboardHealthAggregatorTests
{
    [Fact]
    public void Aggregate_CriticalDegraded_ReturnsCritical()
    {
        var overall = PlatformDashboardHealthAggregator.Aggregate(
        [
            new("core_api", "HEALTHY", true, null),
            new("payment", "DEGRADED", true, "Payment provider is not configured.")
        ]);

        Assert.Equal("CRITICAL", overall);
    }

    [Fact]
    public void Aggregate_NonCriticalDegraded_ReturnsDegraded()
    {
        var overall = PlatformDashboardHealthAggregator.Aggregate(
        [
            new("core_api", "HEALTHY", true, null),
            new("database", "HEALTHY", true, null),
            new("email", "DEGRADED", false, "Email transport is not configured.")
        ]);

        Assert.Equal("DEGRADED", overall);
    }

    [Fact]
    public void Aggregate_CriticalUnknown_ReturnsDegraded()
    {
        var overall = PlatformDashboardHealthAggregator.Aggregate(
        [
            new("core_api", "HEALTHY", true, null),
            new("payment", "UNKNOWN", true, "Configured but no live probe.")
        ]);

        Assert.Equal("DEGRADED", overall);
    }

    [Fact]
    public void Aggregate_AllHealthy_ReturnsHealthy()
    {
        var overall = PlatformDashboardHealthAggregator.Aggregate(
        [
            new("core_api", "HEALTHY", true, null),
            new("database", "HEALTHY", true, null)
        ]);

        Assert.Equal("HEALTHY", overall);
    }

    [Fact]
    public void Aggregate_AllUnknown_ReturnsUnknown()
    {
        var overall = PlatformDashboardHealthAggregator.Aggregate(
        [
            new("email", "UNKNOWN", false, null),
            new("blob", "UNKNOWN", false, null)
        ]);

        Assert.Equal("UNKNOWN", overall);
    }
}
