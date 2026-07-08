using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.POSOperations.Contracts;
using E_POS.Application.Modules.Tenant.POSOperations.Dtos;
using E_POS.Domain.Modules.Tenant.AccessControl.Constants;
using E_POS.Domain.Modules.Tenant.HardwareCash.Constants;
using E_POS.Domain.Modules.Tenant.Orders.Constants;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Constants;
using E_POS.Domain.Modules.Tenant.POSOperations.Constants;

namespace E_POS.Application.Modules.Tenant.POSOperations.Services;

public sealed class PosHomeDashboardService : IPosHomeDashboardService
{
    private readonly IPosHomeDashboardRepository _repository;

    public PosHomeDashboardService(IPosHomeDashboardRepository repository)
    {
        _repository = repository;
    }

    public async Task<ApplicationResult<PosHomeDashboardResponseDto>> GetPosHomeAsync(
        TenantRequestContext context,
        Guid? outletId,
        Guid? tillId,
        Guid? deviceId,
        CancellationToken cancellationToken)
    {
        var hasHomeView =
            context.HasPermission(PosPermissions.Home.View) ||
            context.HasPermission(PosPermissions.Home.ViewDashboard);

        if (!hasHomeView)
        {
            return ApplicationResult<PosHomeDashboardResponseDto>.Failure(
                new ApplicationError(
                    "pos_home_dashboard.permission_denied",
                    "You do not have permission to view POS Home."));
        }

        var resolution = await _repository.ResolveContextAsync(
            context,
            outletId,
            tillId,
            deviceId,
            cancellationToken);

        if (!resolution.IsResolved || resolution.Snapshot is null)
        {
            return ApplicationResult<PosHomeDashboardResponseDto>.Success(
                new PosHomeDashboardResponseDto(
                    ContextResolved: false,
                    ReasonCode: resolution.ReasonCode,
                    Message: resolution.Message,
                    RequiredAction: resolution.RequiredAction,
                    User: null,
                    Context: null,
                    Permissions: null,
                    UnreadNotificationCount: 0,
                    Cards: null,
                    Cashier: null,
                    Device: null,
                    Till: null,
                    Time: null,
                    Notifications: null,
                    Metrics: null,
                    QuickActions: null));
        }

        var snapshot = resolution.Snapshot;
        var tillOpen = true;
        var sessionStatusLabel = TillConstants.FormatSessionStatusLabel(
            snapshot.TillSessionStatus,
            tillOpen);
        var tillDisplayLabel = TillConstants.BuildDisplayLabel(
            snapshot.TillAreaName,
            snapshot.TillNumber,
            sessionStatusLabel);
        var expandedPermissions = TenantPermissionAliases.Expand(context.Permissions.ToList());

        var canStartSalePermission =
            context.HasPermission(PosPermissions.NewSale.View) ||
            context.HasPermission(SalesPermissions.Sale.Create);

        var canViewReturns =
            context.HasPermission(ReturnsPermissions.ViewReturns) ||
            context.HasPermission(ReturnsPermissions.ViewRefunds) ||
            context.HasPermission(ReturnsPermissions.CreateRefund);

        var canViewCustomers =
            context.HasPermission(CustomerPermissions.View) ||
            context.HasPermission(CustomerPermissions.Create);

        var canParkOrViewParkedSales =
            context.HasPermission(SalesPermissions.Park.Create) ||
            context.HasPermission(SalesPermissions.Park.View) ||
            context.HasPermission(SalesPermissions.Park.Recall);

        var canViewCashDrawer =
            context.HasPermission(CashDrawerPermissions.View) ||
            context.HasPermission(CashDrawerPermissions.Manage);

        var canViewNotifications =
            context.HasPermission(PosPermissions.Notifications.View);

        var startSaleEnabled = canStartSalePermission && snapshot.DeviceTrusted && tillOpen;
        var serverNowUtc = DateTimeOffset.UtcNow;

        return ApplicationResult<PosHomeDashboardResponseDto>.Success(
            new PosHomeDashboardResponseDto(
                ContextResolved: true,
                ReasonCode: null,
                Message: null,
                RequiredAction: null,
                User: new PosHomeUserDto(snapshot.CashierDisplayName),
                Context: new PosHomeContextDto(
                    snapshot.OutletName,
                    snapshot.TillName,
                    snapshot.TillSessionId.ToString()),
                Permissions: expandedPermissions,
                UnreadNotificationCount: snapshot.UnreadNotificationCount,
                Cards: new PosHomeCardsDto(
                    StartSale: new PosHomeCardDto(startSaleEnabled),
                    OnlineOrders: null,
                    ReturnsRefunds: new PosHomeCardDto(
                        canViewReturns,
                        snapshot.ReturnsRefundsCount),
                    Customers: new PosHomeCardDto(
                        canViewCustomers,
                        snapshot.CustomersCount),
                    ParkedSales: new PosHomeCardDto(
                        canParkOrViewParkedSales,
                        snapshot.ParkedSalesCount),
                    CashDrawer: new PosHomeCardDto(
                        canViewCashDrawer,
                        Balance: snapshot.CashDrawerBalance)),
                Cashier: new PosHomeCashierDto(snapshot.CashierTenantUserId, snapshot.CashierDisplayName),
                Device: new PosHomeDeviceDto(
                    snapshot.DeviceId,
                    snapshot.DeviceCode,
                    snapshot.DeviceName,
                    snapshot.DeviceTrusted,
                    snapshot.DeviceStatus),
                Till: new PosHomeTillDto(
                    snapshot.TillId,
                    snapshot.TillCode,
                    snapshot.TillName,
                    snapshot.TillAreaName,
                    snapshot.TillNumber,
                    sessionStatusLabel,
                    snapshot.TillSessionId,
                    snapshot.BusinessDate,
                    snapshot.CurrencyCode,
                    tillDisplayLabel),
                Time: new PosHomeTimeDto(
                    serverNowUtc,
                    snapshot.OutletTimezone,
                    snapshot.BusinessDate),
                Notifications: new PosHomeNotificationsDto(snapshot.UnreadNotificationCount),
                Metrics: new PosHomeMetricsDto(snapshot.ParkedSalesCount),
                QuickActions: new PosHomeQuickActionsDto(
                    CanStartSale: startSaleEnabled,
                    CanAddCustomer: canViewCustomers,
                    CanViewReturns: canViewReturns,
                    CanViewParkedSales: canParkOrViewParkedSales,
                    CanViewCashDrawer: canViewCashDrawer,
                    CanViewNotifications: canViewNotifications)));
    }
}
