using E_POS.Application.Common.Models;

namespace E_POS.Application.Modules.Tenant.POSOperations.Contracts;

public interface IPosHomeDashboardRepository
{
    Task<PosHomeContextResolutionResult> ResolveContextAsync(
        TenantRequestContext context,
        Guid? outletId,
        Guid? tillId,
        Guid? deviceId,
        string? deviceFingerprint,
        CancellationToken cancellationToken);
}

public sealed record PosHomeContextResolutionResult(
    bool IsResolved,
    string? ReasonCode,
    string? Message,
    string? RequiredAction,
    PosHomeDashboardDbSnapshot? Snapshot);

public sealed record PosHomeDashboardDbSnapshot(
    Guid CashierTenantUserId,
    string CashierDisplayName,
    Guid DeviceId,
    string DeviceCode,
    string DeviceName,
    bool DeviceTrusted,
    string DeviceStatus,
    Guid TillId,
    string TillCode,
    string TillName,
    string TillAreaName,
    int TillNumber,
    string? TillSessionStatus,
    Guid TillSessionId,
    DateOnly BusinessDate,
    string CurrencyCode,
    Guid OutletId,
    string OutletName,
    string OutletTimezone,
    int UnreadNotificationCount,
    int ReturnsRefundsCount,
    int CustomersCount,
    int ParkedSalesCount,
    double CashDrawerBalance);
