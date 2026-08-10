using E_POS.Application.Common.Models;
using E_POS.Application.Common.Security;
using E_POS.Application.Modules.Tenant.Inventory.Dtos.Dashboard;

namespace E_POS.Application.Modules.Tenant.Inventory.Contracts.Dashboard;

/// <summary>
/// Provides dashboard analytics and metrics for Tenant Admin inventory.
/// </summary>
public interface IDashboardService
{
    /// <summary>
    /// Retrieves key inventory metrics (e.g. low stock, out of stock) for the dashboard.
    /// </summary>
    Task<ApplicationResult<DashboardMetricsResponse>> GetDashboardMetricsAsync(
        TenantRequestContext context,
        Guid? outletId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves paginated alerts for inventory items requiring attention (e.g. near expiry, out of stock).
    /// </summary>
    Task<ApplicationResult<DashboardAlertsResponse>> GetDashboardAlertsAsync(
        TenantRequestContext context,
        Guid? outletId,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves paginated recent inventory activities (e.g. stock ins, adjustments).
    /// </summary>
    Task<ApplicationResult<DashboardActivitiesResponse>> GetDashboardActivitiesAsync(
        TenantRequestContext context,
        Guid? outletId,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
}
