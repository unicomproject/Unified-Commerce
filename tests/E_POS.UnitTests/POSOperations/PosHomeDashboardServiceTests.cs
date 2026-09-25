using System.Text.Json;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.POSOperations.Contracts;
using E_POS.Application.Modules.Tenant.POSOperations.Dtos;
using E_POS.Application.Modules.Tenant.POSOperations.Services;
using E_POS.Domain.Modules.Tenant.AccessControl.Constants;
using E_POS.Domain.Modules.Tenant.AccessControl.Catalog.CashierPos;
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
        Assert.Equal("OneVerz POS", result.Value.Branding!.DisplayName);
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
        Assert.Equal(
            "https://cdn.example.test/cashier-001.jpg",
            result.Value!.Cashier!.ProfileImageUrl);
        Assert.False(result.Value.Cards!.CashDrawer.Enabled);
        Assert.False(result.Value.QuickActions!.CanViewCashDrawer);
    }

    [Fact]
    public async Task GetPosHomeAsync_WithoutSummarySectionPermission_OmitsSummary()
    {
        var result = await CreateService().GetPosHomeAsync(
            new TenantRequestContext(Guid.NewGuid(), Guid.NewGuid(), [PosPermissions.Home.View]),
            null, null, null, null, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value!.Summary);
    }

    [Fact]
    public async Task GetPosHomeAsync_GranularSummaryPermissionsFilterProtectedFields()
    {
        var result = await CreateService().GetPosHomeAsync(
            new TenantRequestContext(Guid.NewGuid(), Guid.NewGuid(),
            [
                PosPermissions.Home.View,
                CashierPosFineGrainedPermissions.PosHomeSessionSummaryView,
                CashierPosFineGrainedPermissions.PosHomeSessionSummaryTotalSales,
                CashierPosFineGrainedPermissions.PosHomeSessionSummaryTransactionCount,
            ]),
            null, null, null, null, CancellationToken.None);

        var summary = Assert.IsType<PosHomeSummaryDto>(result.Value!.Summary);
        Assert.Equal(125m, summary.GrossSalesAmount);
        Assert.Equal(3, summary.TransactionCount);
        Assert.Null(summary.RefundAmount);
        Assert.Null(summary.RefundCount);
        Assert.False(summary.ReturnsApplicable);
        Assert.Null(summary.DiscountAmount);
        Assert.False(summary.DiscountsApplicable);
        Assert.Null(summary.NetSalesAmount);
    }

    [Fact]
    public async Task GetPosHomeAsync_AllSummaryPermissions_SerializesTypedApplicabilityContract()
    {
        var result = await CreateService().GetPosHomeAsync(
            new TenantRequestContext(Guid.NewGuid(), Guid.NewGuid(),
            [
                PosPermissions.Home.View,
                CashierPosFineGrainedPermissions.PosHomeSessionSummaryView,
                CashierPosFineGrainedPermissions.PosHomeSessionSummaryTotalSales,
                CashierPosFineGrainedPermissions.PosHomeSessionSummaryTransactionCount,
                CashierPosFineGrainedPermissions.PosHomeSessionSummaryReturns,
                CashierPosFineGrainedPermissions.PosHomeSessionSummaryDiscounts,
                CashierPosFineGrainedPermissions.PosHomeSessionSummaryNetSales,
            ]),
            null, null, null, null, CancellationToken.None);

        var summary = Assert.IsType<PosHomeSummaryDto>(result.Value!.Summary);
        Assert.True(summary.ReturnsApplicable);
        Assert.True(summary.DiscountsApplicable);
        var json = JsonSerializer.Serialize(summary, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.Contains("\"scope\":\"CURRENT_TILL_SESSION\"", json);
        Assert.Contains("\"grossSalesAmount\":125", json);
        Assert.Contains("\"returnsApplicable\":true", json);
        Assert.Contains("\"discountsApplicable\":true", json);
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
                CashierProfileImageUrl: "https://cdn.example.test/cashier-001.jpg",
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
                BusinessDisplayName: "OneVerz POS",
                BusinessLogoUrl: null,
                UnreadNotificationCount: 0,
                ReturnsRefundsCount: 0,
                CustomersCount: 0,
                ParkedSalesCount: 0,
                CashDrawerBalance: 0,
                GrossSalesAmount: 125,
                TransactionCount: 3,
                RefundAmount: 5,
                RefundCount: 1,
                DiscountAmount: 10,
                NetSalesAmount: 110);

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
