using E_POS.Application.Modules.Tenant.Inventory.Dashboard.Dtos;

namespace E_POS.Application.Modules.Tenant.Inventory.Dashboard.Contracts.Repositories;

/// <summary>
/// Provides data access for dashboard analytics and metrics for Tenant Admin inventory.
/// </summary>
public interface IDashboardRepository
{
    /// <summary>
    /// Retrieves key inventory metrics (e.g. low stock, out of stock) for the dashboard.
    /// </summary>
    Task<DashboardMetricsResponse> GetDashboardMetricsAsync(
        Guid tenantId,
        Guid? outletId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves paginated alerts for inventory items requiring attention (e.g. near expiry, out of stock).
    /// </summary>
    Task<DashboardAlertsResponse> GetDashboardAlertsAsync(
        Guid tenantId,
        Guid? outletId,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves paginated recent inventory activities (e.g. stock ins, adjustments).
    /// </summary>
    Task<DashboardActivitiesResponse> GetDashboardActivitiesAsync(
        Guid tenantId,
        Guid? outletId,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
        
    /// <summary>
    /// Verifies if the user has access to the specified outlet.
    /// </summary>
    Task<bool> UserHasOutletAccessAsync(
        Guid tenantId,
        Guid userId,
        Guid outletId,
        CancellationToken cancellationToken);
}
