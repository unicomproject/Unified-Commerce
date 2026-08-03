using E_POS.Application.Modules.Tenant.OutletTillDevice.Services;
using Xunit;

namespace E_POS.UnitTests.OutletTillDevice;

public sealed class HardwareConnectionStatusResolverTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Resolve_MaintenanceLifecycle_ReturnsMaintenance()
    {
        var result = HardwareConnectionStatusResolver.Resolve(
            "MAINTENANCE", null, null, null, Now, Now, 300);

        Assert.Equal(HardwareConnectionStatusResolver.Maintenance, result.ConnectionStatus);
        Assert.Equal("DEVICE_MAINTENANCE", result.WarningCode);
    }

    [Fact]
    public void Resolve_FreshHeartbeat_ReturnsConnected()
    {
        var result = HardwareConnectionStatusResolver.Resolve(
            "ACTIVE", "PASSED", null, Now, Now.AddSeconds(-30), Now, 300);

        Assert.Equal(HardwareConnectionStatusResolver.Connected, result.ConnectionStatus);
        Assert.Equal(HardwareConnectionStatusResolver.HealthHealthy, result.HealthStatus);
        Assert.Null(result.WarningCode);
    }

    [Fact]
    public void Resolve_ExpiredHeartbeat_ReturnsDisconnected()
    {
        var result = HardwareConnectionStatusResolver.Resolve(
            "ACTIVE", "PASSED", null, Now.AddMinutes(-20), Now.AddMinutes(-20), Now, 300);

        Assert.Equal(HardwareConnectionStatusResolver.Disconnected, result.ConnectionStatus);
        Assert.Equal("HARDWARE_HEARTBEAT_EXPIRED", result.WarningCode);
    }

    [Fact]
    public void Resolve_NeverSeen_ReturnsUnknown()
    {
        var result = HardwareConnectionStatusResolver.Resolve(
            "ACTIVE", null, null, null, null, Now, 300);

        Assert.Equal(HardwareConnectionStatusResolver.Unknown, result.ConnectionStatus);
        Assert.Equal("HARDWARE_HEARTBEAT_MISSING", result.WarningCode);
    }

    [Fact]
    public void Resolve_FailedTest_ReturnsNeedsAttention()
    {
        var result = HardwareConnectionStatusResolver.Resolve(
            "ACTIVE", "FAILED", "Printer paper low", Now, Now, Now, 300);

        Assert.Equal(HardwareConnectionStatusResolver.NeedsAttention, result.ConnectionStatus);
        Assert.Equal(HardwareConnectionStatusResolver.HealthFailed, result.HealthStatus);
        Assert.Equal("HARDWARE_TEST_FAILED", result.WarningCode);
        Assert.Equal("Printer paper low", result.WarningMessage);
    }

    [Fact]
    public void Resolve_WarningTest_ReturnsNeedsAttention()
    {
        var result = HardwareConnectionStatusResolver.Resolve(
            "ACTIVE", "WARNING", "Paper low", Now, Now, Now, 300);

        Assert.Equal(HardwareConnectionStatusResolver.NeedsAttention, result.ConnectionStatus);
        Assert.Equal(HardwareConnectionStatusResolver.HealthWarning, result.HealthStatus);
    }
}

public sealed class HardwareAttentionReasonBuilderTests
{
    [Fact]
    public void CalculateAlertCount_IgnoresInfoAndDeduplicates()
    {
        var now = DateTimeOffset.UtcNow;
        var reasons = new[]
        {
            new E_POS.Application.Modules.Tenant.OutletTillDevice.Dtos.TenantAdmin.TenantAdminTillAttentionReasonResponse(
                "HARDWARE_NOT_ASSIGNED", "INFO", "none", null, null, now),
            new E_POS.Application.Modules.Tenant.OutletTillDevice.Dtos.TenantAdmin.TenantAdminTillAttentionReasonResponse(
                "HARDWARE_TEST_FAILED", "ERROR", "failed", Guid.NewGuid(), "RECEIPT_PRINTER", now),
            new E_POS.Application.Modules.Tenant.OutletTillDevice.Dtos.TenantAdmin.TenantAdminTillAttentionReasonResponse(
                "HARDWARE_TEST_FAILED", "ERROR", "failed again", Guid.Parse("11111111-1111-1111-1111-111111111111"), "RECEIPT_PRINTER", now),
            new E_POS.Application.Modules.Tenant.OutletTillDevice.Dtos.TenantAdmin.TenantAdminTillAttentionReasonResponse(
                "HARDWARE_TEST_FAILED", "ERROR", "dup", Guid.Parse("11111111-1111-1111-1111-111111111111"), "RECEIPT_PRINTER", now),
        };

        var count = HardwareAttentionReasonBuilder.CalculateAlertCount(reasons);
        Assert.Equal(2, count);
    }
}
