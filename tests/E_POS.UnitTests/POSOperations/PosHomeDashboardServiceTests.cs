using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.POSOperations.Contracts;
using E_POS.Application.Modules.Tenant.POSOperations.Services;
using E_POS.Domain.Modules.Tenant.AccessControl.Constants;
using E_POS.Domain.Modules.Tenant.HardwareCash.Constants;
using E_POS.Domain.Modules.Tenant.POSOperations.Constants;
using Xunit;

namespace E_POS.UnitTests.POSOperations;

public sealed class PosHomeDashboardServiceTests
{
    [Fact]
    public async Task GetPosHomeAsync_WithPosHomeView_ReturnsSuccess()
    {
        var service = CreateService();
        var context = new TenantRequestContext(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [PosPermissions.Home.View]);

        var result = await service.GetPosHomeAsync(
            context,
            null,
            null,
            null,
            null,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.True(result.Value!.ContextResolved);
    }

    [Fact]
    public async Task GetPosHomeAsync_WithDashboardAlias_ReturnsSuccess()
    {
        var service = CreateService();
        var context = new TenantRequestContext(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [PosPermissions.Home.ViewDashboard]);

        var result = await service.GetPosHomeAsync(
            context,
            null,
            null,
            null,
            null,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.True(result.Value!.ContextResolved);
    }

    [Fact]
    public async Task GetPosHomeAsync_WithoutHomePermission_ReturnsForbiddenError()
    {
        var service = CreateService();
        var context = new TenantRequestContext(
            Guid.NewGuid(),
            Guid.NewGuid(),
            ["pos.home.view_dashboard"]);

        var result = await service.GetPosHomeAsync(
            context,
            null,
            null,
            null,
            null,
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("pos_home_dashboard.permission_denied", result.Error.Code);
    }

    [Fact]
    public async Task GetPosHomeAsync_AddCustomerRequiresCustomersCreate()
    {
        var service = CreateService();
        var context = new TenantRequestContext(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [
                PosPermissions.Home.View,
                CustomerPermissions.View,
            ]);

        var result = await service.GetPosHomeAsync(
            context,
            null,
            null,
            null,
            null,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.False(result.Value!.Cards!.Customers.Enabled);
        Assert.False(result.Value.QuickActions!.CanAddCustomer);
    }

    [Fact]
    public async Task GetPosHomeAsync_CashDrawerViewDoesNotUseManageAsFallback()
    {
        var service = CreateService();
        var context = new TenantRequestContext(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [
                PosPermissions.Home.View,
                CashDrawerPermissions.Manage,
            ]);

        var result = await service.GetPosHomeAsync(
            context,
            null,
            null,
            null,
            null,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.False(result.Value!.Cards!.CashDrawer.Enabled);
        Assert.False(result.Value.QuickActions!.CanViewCashDrawer);
    }

    private static PosHomeDashboardService CreateService() =>
        new(new FakePosHomeDashboardRepository());

    private sealed class FakePosHomeDashboardRepository : IPosHomeDashboardRepository
    {
        public Task<PosHomeContextResolutionResult> ResolveContextAsync(
            TenantRequestContext context,
            Guid? outletId,
            Guid? tillId,
            Guid? deviceId,
            string? deviceFingerprint,
            CancellationToken cancellationToken)
        {
            var snapshot = new PosHomeDashboardDbSnapshot(
                CashierTenantUserId: context.UserId,
                CashierDisplayName: "Cashier 001",
                CashierRoleLabel: "Cashier",
                TenantDisplayName: "OneVerz",
                TenantTradingName: null,
                TenantLogoUrl: null,
                DeviceId: Guid.NewGuid(),
                DeviceCode: "POS-01",
                DeviceName: "Front POS",
                DeviceTrusted: true,
                DeviceStatus: "ACTIVE",
                TillId: Guid.NewGuid(),
                TillCode: "TILL-001",
                TillName: "Front Till",
                TillAreaName: "Front",
                TillNumber: 1,
                TillSessionStatus: "OPEN",
                TillSessionId: Guid.NewGuid(),
                BusinessDate: new DateOnly(2026, 7, 11),
                CurrencyCode: "LKR",
                OutletId: Guid.NewGuid(),
                OutletName: "Main Outlet",
                OutletTimezone: "Asia/Colombo",
                UnreadNotificationCount: 0,
                ReturnsRefundsCount: 0,
                CustomersCount: 0,
                ParkedSalesCount: 0,
                CashDrawerBalance: 0,
                GrossSalesAmount: 0,
                TransactionCount: 0,
                RefundAmount: 0,
                RefundCount: 0,
                DiscountAmount: 0,
                NetSalesAmount: 0);

            return Task.FromResult(
                new PosHomeContextResolutionResult(
                    IsResolved: true,
                    ReasonCode: null,
                    Message: null,
                    RequiredAction: null,
                    Snapshot: snapshot));
        }
    }
}
