using E_POS.Application.Common.Models;
using E_POS.Application.Common.Security;
using E_POS.Application.Modules.Tenant.Inventory.Dashboard.Contracts.Repositories;
using E_POS.Application.Modules.Tenant.Inventory.Dashboard.Contracts.Services;
using E_POS.Application.Modules.Tenant.Inventory.Dashboard.Dtos;
using E_POS.Domain.Modules.Tenant.Inventory.Constants;

namespace E_POS.Application.Modules.Tenant.Inventory.Dashboard.Services;

public sealed class DashboardService : IDashboardService
{
    private readonly IDashboardRepository _repository;

    public DashboardService(IDashboardRepository repository)
    {
        _repository = repository;
    }

    public async Task<ApplicationResult<DashboardMetricsResponse>> GetDashboardMetricsAsync(
        TenantRequestContext context,
        Guid? outletId,
        CancellationToken cancellationToken)
    {
        // Enforce View permission for accessing dashboard metrics
        if (!context.HasPermission(StockPermissions.DashboardView) && !context.HasPermission(StockPermissions.LegacyInventoryView))
            return ApplicationResult<DashboardMetricsResponse>.Failure(new ApplicationError("inventory.permission_denied", "Permission denied."));

        if (outletId.HasValue)
        {
            // Verify outlet access to prevent cross-tenant/cross-outlet data leakage
            var hasAccess = await _repository.UserHasOutletAccessAsync(context.TenantId, context.UserId, outletId.Value, cancellationToken);
            if (!hasAccess)
                return ApplicationResult<DashboardMetricsResponse>.Failure(new ApplicationError("inventory.permission_denied", "You do not have access to this outlet."));
        }

        var result = await _repository.GetDashboardMetricsAsync(context.TenantId, outletId, cancellationToken);
        return ApplicationResult<DashboardMetricsResponse>.Success(result);
    }

    public async Task<ApplicationResult<DashboardAlertsResponse>> GetDashboardAlertsAsync(
        TenantRequestContext context,
        Guid? outletId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        if (!context.HasPermission(StockPermissions.DashboardView) && !context.HasPermission(StockPermissions.LegacyInventoryView))
            return ApplicationResult<DashboardAlertsResponse>.Failure(new ApplicationError("inventory.permission_denied", "Permission denied."));

        if (outletId.HasValue)
        {
            var hasAccess = await _repository.UserHasOutletAccessAsync(context.TenantId, context.UserId, outletId.Value, cancellationToken);
            if (!hasAccess)
                return ApplicationResult<DashboardAlertsResponse>.Failure(new ApplicationError("inventory.permission_denied", "You do not have access to this outlet."));
        }

        var result = await _repository.GetDashboardAlertsAsync(context.TenantId, outletId, page, pageSize, cancellationToken);
        return ApplicationResult<DashboardAlertsResponse>.Success(result);
    }

    public async Task<ApplicationResult<DashboardActivitiesResponse>> GetDashboardActivitiesAsync(
        TenantRequestContext context,
        Guid? outletId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        if (!context.HasPermission(StockPermissions.DashboardView) && !context.HasPermission(StockPermissions.LegacyInventoryView))
            return ApplicationResult<DashboardActivitiesResponse>.Failure(new ApplicationError("inventory.permission_denied", "Permission denied."));

        if (outletId.HasValue)
        {
            var hasAccess = await _repository.UserHasOutletAccessAsync(context.TenantId, context.UserId, outletId.Value, cancellationToken);
            if (!hasAccess)
                return ApplicationResult<DashboardActivitiesResponse>.Failure(new ApplicationError("inventory.permission_denied", "You do not have access to this outlet."));
        }

        var result = await _repository.GetDashboardActivitiesAsync(context.TenantId, outletId, page, pageSize, cancellationToken);
        return ApplicationResult<DashboardActivitiesResponse>.Success(result);
    }
}
