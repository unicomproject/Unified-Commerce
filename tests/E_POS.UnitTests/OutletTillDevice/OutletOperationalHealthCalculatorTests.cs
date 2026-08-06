using E_POS.Application.Modules.Tenant.OutletTillDevice.Services;
using Xunit;

namespace E_POS.UnitTests.OutletTillDevice;

public sealed class OutletOperationalHealthCalculatorTests
{
    [Theory]
    [InlineData(0, 0, OutletOperationalHealthCalculator.UnknownStatus)]
    [InlineData(2, 2, OutletOperationalHealthCalculator.HealthyStatus)]
    [InlineData(2, 1, OutletOperationalHealthCalculator.NeedsAttentionStatus)]
    [InlineData(2, 0, OutletOperationalHealthCalculator.CriticalStatus)]
    public void Classify_ListPreviewCountsMatchOverviewClassification(int activeTillCount, int onlineTillCount, string expectedStatus)
    {
        var tills = Enumerable.Range(0, activeTillCount)
            .Select(index => new OutletOperationalHealthCalculator.TillHealthInput(
                Guid.NewGuid(),
                $"T{index}",
                $"Till {index}",
                "ACTIVE",
                index < onlineTillCount ? "Online" : "Offline",
                DateTimeOffset.UtcNow))
            .ToList();

        var overviewStatus = OutletOperationalHealthCalculator.Calculate("ACTIVE", tills).Status;

        Assert.Equal(expectedStatus, OutletOperationalHealthCalculator.Classify(activeTillCount, onlineTillCount));
        Assert.Equal(overviewStatus, OutletOperationalHealthCalculator.Classify(activeTillCount, onlineTillCount));
    }

    [Fact]
    public void Calculate_NoActiveTills_ReturnsUnknownStatus()
    {
        var tills = new List<OutletOperationalHealthCalculator.TillHealthInput>
        {
            new(Guid.NewGuid(), "T1", "Till 1", "INACTIVE", "Offline", null)
        };

        var result = OutletOperationalHealthCalculator.Calculate("ACTIVE", tills);

        Assert.Equal("UNKNOWN", result.Status);
        Assert.Empty(result.Alerts);
        Assert.Equal(0, result.TotalActiveAlertCount);
    }

    [Fact]
    public void Calculate_AllActiveTillsOnline_ReturnsHealthyStatus()
    {
        var now = DateTimeOffset.UtcNow;
        var tills = new List<OutletOperationalHealthCalculator.TillHealthInput>
        {
            new(Guid.NewGuid(), "T1", "Till 1", "ACTIVE", "Online", now),
            new(Guid.NewGuid(), "T2", "Till 2", "ACTIVE", "Online", now.AddMinutes(-1)),
            new(Guid.NewGuid(), "T3", "Till 3", "INACTIVE", "Offline", null) // Intentionally inactive till
        };

        var result = OutletOperationalHealthCalculator.Calculate("ACTIVE", tills);

        Assert.Equal("HEALTHY", result.Status);
        Assert.Equal(now, result.LastActivityAt);
        Assert.Empty(result.Alerts);
        Assert.Equal(0, result.TotalActiveAlertCount);
    }

    [Fact]
    public void Calculate_SomeTillsOffline_ReturnsNeedsAttentionStatusAndAlerts()
    {
        var now = DateTimeOffset.UtcNow;
        var tills = new List<OutletOperationalHealthCalculator.TillHealthInput>
        {
            new(Guid.NewGuid(), "T1", "Till 1", "ACTIVE", "Online", now),
            new(Guid.NewGuid(), "T2", "Till 2", "ACTIVE", "Offline", now.AddMinutes(-10))
        };

        var result = OutletOperationalHealthCalculator.Calculate("ACTIVE", tills);

        Assert.Equal("NEEDS_ATTENTION", result.Status);
        Assert.Single(result.Alerts);
        Assert.Equal(1, result.TotalActiveAlertCount);
        Assert.Equal("Till Offline: Till 2", result.Alerts[0].Title);
    }

    [Fact]
    public void Calculate_AllActiveTillsOffline_ReturnsCriticalStatus()
    {
        var now = DateTimeOffset.UtcNow;
        var tills = new List<OutletOperationalHealthCalculator.TillHealthInput>
        {
            new(Guid.NewGuid(), "T1", "Till 1", "ACTIVE", "Offline", now.AddMinutes(-5)),
            new(Guid.NewGuid(), "T2", "Till 2", "ACTIVE", "Offline", now.AddMinutes(-10))
        };

        var result = OutletOperationalHealthCalculator.Calculate("ACTIVE", tills);

        Assert.Equal("CRITICAL", result.Status);
        Assert.Equal(2, result.Alerts.Count);
        Assert.Equal(2, result.TotalActiveAlertCount);
    }

    [Fact]
    public void Calculate_InactiveOutletWithoutTills_ReturnsUnknownNotCritical()
    {
        var result = OutletOperationalHealthCalculator.Calculate("INACTIVE", Array.Empty<OutletOperationalHealthCalculator.TillHealthInput>());

        Assert.Equal("UNKNOWN", result.Status);
        Assert.Null(result.LastActivityAt);
        Assert.Empty(result.Alerts);
        Assert.Equal(0, result.TotalActiveAlertCount);
    }
}
