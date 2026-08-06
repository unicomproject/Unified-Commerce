using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Contracts;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Dtos.TenantAdmin;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Constants;

namespace E_POS.Application.Modules.Tenant.OutletTillDevice.Services;

public sealed class TenantAdminOutletService : ITenantAdminOutletService
{
    private static readonly ApplicationError PermissionDenied =
        new("outlet.permission_denied", "Permission denied for outlet management.");

    private static readonly ApplicationError NotFound =
        new("outlet.not_found", "Outlet was not found.");

    private readonly ITenantAdminOutletRepository _repository;
    private readonly IOutletAuditLogger? _auditLogger;

    public TenantAdminOutletService(
        ITenantAdminOutletRepository repository,
        IOutletAuditLogger? auditLogger = null)
    {
        _repository = repository;
        _auditLogger = auditLogger;
    }

    public async Task<ApplicationResult<TenantAdminOutletListResponse>> ListAsync(
        TenantRequestContext context,
        TenantAdminOutletListQuery query,
        CancellationToken cancellationToken)
    {
        var accessError = ValidateDetailAccess(context);
        if (accessError is not null)
        {
            return ApplicationResult<TenantAdminOutletListResponse>.Failure(accessError);
        }

        var canViewTillsAndHealth = HasAnyPermission(
            context,
            TenantAdminOutletPermissions.TillsView,
            TenantAdminOutletPermissions.TenantTillsView,
            TenantAdminOutletPermissions.Manage);
        if (!canViewTillsAndHealth && !string.IsNullOrWhiteSpace(query.OperationalHealth))
        {
            return ApplicationResult<TenantAdminOutletListResponse>.Failure(PermissionDenied);
        }

        var normalizedQuery = query with
        {
            PageNumber = Math.Max(1, query.PageNumber),
            PageSize = Math.Clamp(query.PageSize, 1, 100),
        };
        var response = await _repository.ListAsync(context.TenantId, normalizedQuery, cancellationToken);
        if (!canViewTillsAndHealth)
        {
            response = response with
            {
                Items = response.Items.Select(item => item with
                {
                    Tills = null,
                    OperationalHealth = null,
                    Access = new TenantAdminOutletListSectionAccessResponse(false),
                }).ToList(),
            };
        }
        return ApplicationResult<TenantAdminOutletListResponse>.Success(response);
    }

    public async Task<ApplicationResult<TenantAdminOutletDetailResponse>> GetDetailAsync(
        TenantRequestContext context,
        Guid outletId,
        CancellationToken cancellationToken)
    {
        var accessError = ValidateDetailAccess(context);
        if (accessError is not null)
        {
            return ApplicationResult<TenantAdminOutletDetailResponse>.Failure(accessError);
        }

        return await LoadDetailAsync(context.TenantId, outletId, cancellationToken);
    }

    public async Task<ApplicationResult<TenantAdminOutletRevenueSummaryResponse>> GetRevenueSummaryAsync(
        TenantRequestContext context,
        Guid outletId,
        CancellationToken cancellationToken)
    {
        var accessError = ValidateRevenueAccess(context);
        if (accessError is not null)
        {
            return ApplicationResult<TenantAdminOutletRevenueSummaryResponse>.Failure(accessError);
        }

        if (!await _repository.OutletExistsAsync(context.TenantId, outletId, cancellationToken))
        {
            return ApplicationResult<TenantAdminOutletRevenueSummaryResponse>.Failure(NotFound);
        }

        var response = await _repository.GetRevenueSummaryAsync(
            context.TenantId,
            outletId,
            cancellationToken);

        return ApplicationResult<TenantAdminOutletRevenueSummaryResponse>.Success(response);
    }

    public async Task<ApplicationResult<TenantAdminOutletUsersResponse>> GetUsersAsync(
        TenantRequestContext context,
        Guid outletId,
        CancellationToken cancellationToken)
    {
        var accessError = ValidateUsersAccess(context);
        if (accessError is not null)
        {
            return ApplicationResult<TenantAdminOutletUsersResponse>.Failure(accessError);
        }

        if (!await _repository.OutletExistsAsync(context.TenantId, outletId, cancellationToken))
        {
            return ApplicationResult<TenantAdminOutletUsersResponse>.Failure(NotFound);
        }

        var response = await _repository.GetUsersAsync(context.TenantId, outletId, cancellationToken);
        return ApplicationResult<TenantAdminOutletUsersResponse>.Success(response);
    }

    public async Task<ApplicationResult<TenantAdminOutletTillsResponse>> GetTillsAsync(
        TenantRequestContext context,
        Guid outletId,
        CancellationToken cancellationToken)
    {
        var accessError = ValidateTillsAccess(context);
        if (accessError is not null)
        {
            return ApplicationResult<TenantAdminOutletTillsResponse>.Failure(accessError);
        }

        if (!await _repository.OutletExistsAsync(context.TenantId, outletId, cancellationToken))
        {
            return ApplicationResult<TenantAdminOutletTillsResponse>.Failure(NotFound);
        }

        var response = await _repository.GetTillsAsync(context.TenantId, outletId, cancellationToken);
        return ApplicationResult<TenantAdminOutletTillsResponse>.Success(response);
    }

    public async Task<ApplicationResult<TenantAdminOutletOverviewResponse>> GetOverviewAsync(
        TenantRequestContext context,
        Guid outletId,
        CancellationToken cancellationToken)
    {
        var accessError = ValidateDetailAccess(context);
        if (accessError is not null)
        {
            return ApplicationResult<TenantAdminOutletOverviewResponse>.Failure(accessError);
        }

        if (!await _repository.OutletExistsAsync(context.TenantId, outletId, cancellationToken))
        {
            return ApplicationResult<TenantAdminOutletOverviewResponse>.Failure(NotFound);
        }

        var outletInfo = await _repository.GetOverviewInfoAsync(context.TenantId, outletId, cancellationToken);
        if (outletInfo is null)
        {
            return ApplicationResult<TenantAdminOutletOverviewResponse>.Failure(NotFound);
        }

        var manager = await _repository.GetOverviewManagerAsync(context.TenantId, outletId, cancellationToken);

        bool canViewTills = HasAnyPermission(context, TenantAdminOutletPermissions.TillsView, TenantAdminOutletPermissions.TenantTillsView, TenantAdminOutletPermissions.Manage);
        bool canViewSales = HasAnyPermission(context, TenantAdminOutletPermissions.RevenueView, TenantAdminOutletPermissions.ReportsSalesView, TenantAdminOutletPermissions.Manage);
        bool canViewInventory = HasAnyPermission(context, "tenant.stock.value.view", "tenant.stock.view", TenantAdminOutletPermissions.Manage);
        bool canViewOrders = HasAnyPermission(context, "tenant.orders.view", TenantAdminOutletPermissions.Manage);
        bool canViewAlerts = canViewTills;

        OutletOverviewTillSummaryResponse? tillSummary = null;
        if (canViewTills)
        {
            var tillsRes = await _repository.GetTillsAsync(context.TenantId, outletId, cancellationToken);
            tillSummary = new OutletOverviewTillSummaryResponse(
                TotalCount: tillsRes.Summary.TotalTills,
                ActiveCount: tillsRes.Summary.ActiveTills,
                OnlineCount: tillsRes.Items.Count(t => string.Equals(t.DeviceStatus, "Online", StringComparison.OrdinalIgnoreCase)),
                AttentionCount: tillsRes.Summary.TillsNeedingAttention);
        }

        OutletOverviewSalesSummaryResponse? salesSummary = null;
        if (canViewSales)
        {
            salesSummary = await _repository.GetOverviewSalesAsync(context.TenantId, outletId, cancellationToken);
        }

        OutletOverviewInventorySummaryResponse? inventorySummary = null;
        if (canViewInventory)
        {
            var stockValue = await _repository.GetOverviewStockValueAsync(context.TenantId, outletId, cancellationToken);
            var currency = await _repository.GetTenantCurrencyCodeAsync(context.TenantId, cancellationToken);
            inventorySummary = new OutletOverviewInventorySummaryResponse(stockValue, currency);
        }

        OutletOverviewOrderSummaryResponse? orderSummary = null;
        if (canViewOrders)
        {
            var openCount = await _repository.GetOverviewOpenOrdersCountAsync(context.TenantId, outletId, cancellationToken);
            orderSummary = new OutletOverviewOrderSummaryResponse(openCount);
        }

        var tillHealthInputs = await _repository.GetOverviewTillHealthInputsAsync(context.TenantId, outletId, cancellationToken);
        var healthResult = OutletOperationalHealthCalculator.Calculate(outletInfo.Status, tillHealthInputs);

        IReadOnlyList<OutletOverviewAlertResponse>? alerts = canViewAlerts ? healthResult.Alerts : null;

        var response = new TenantAdminOutletOverviewResponse(
            Outlet: outletInfo,
            Manager: manager,
            Tills: tillSummary,
            Sales: salesSummary,
            Inventory: inventorySummary,
            Orders: orderSummary,
            Health: new OutletOverviewHealthResponse(
                Status: healthResult.Status,
                LastActivityAt: healthResult.LastActivityAt,
                LastSyncAt: null),
            Alerts: alerts,
            TotalActiveAlertCount: canViewAlerts ? healthResult.TotalActiveAlertCount : 0,
            Access: new OutletOverviewSectionAccessResponse(
                CanViewTills: canViewTills,
                CanViewSales: canViewSales,
                CanViewInventory: canViewInventory,
                CanViewOrders: canViewOrders,
                CanViewAlerts: canViewAlerts));

        return ApplicationResult<TenantAdminOutletOverviewResponse>.Success(response);
    }

    public async Task<ApplicationResult> SetManagerAsync(
        TenantRequestContext context,
        Guid outletId,
        TenantAdminOutletManagerUpdateRequest request,
        CancellationToken cancellationToken)
    {
        var accessError = ValidateManageAccess(context);
        if (accessError is not null) return ApplicationResult.Failure(accessError);

        if (!await _repository.OutletExistsAsync(context.TenantId, outletId, cancellationToken))
        {
            return ApplicationResult.Failure(NotFound);
        }

        if (!await _repository.TenantUserExistsAndActiveAsync(context.TenantId, request.TenantUserId, cancellationToken))
        {
            return ApplicationResult.Failure(new ApplicationError("tenant_user.not_found", "Active tenant user was not found."));
        }

        var now = DateTimeOffset.UtcNow;
        var success = await _repository.SetPrimaryManagerAsync(
            context.TenantId,
            outletId,
            request.TenantUserId,
            context.UserId,
            now,
            cancellationToken);

        if (!success)
        {
            return ApplicationResult.Failure(new ApplicationError("outlet.manager_assignment_failed", "Failed to assign primary manager."));
        }

        _auditLogger?.LogManagerAssigned(context.TenantId, context.UserId, outletId, request.TenantUserId);
        return ApplicationResult.Success();
    }

    public async Task<ApplicationResult> RemoveManagerAsync(
        TenantRequestContext context,
        Guid outletId,
        CancellationToken cancellationToken)
    {
        var accessError = ValidateManageAccess(context);
        if (accessError is not null) return ApplicationResult.Failure(accessError);

        if (!await _repository.OutletExistsAsync(context.TenantId, outletId, cancellationToken))
        {
            return ApplicationResult.Failure(NotFound);
        }

        var now = DateTimeOffset.UtcNow;
        await _repository.RemovePrimaryManagerAsync(
            context.TenantId,
            outletId,
            context.UserId,
            now,
            cancellationToken);

        _auditLogger?.LogManagerRemoved(context.TenantId, context.UserId, outletId);
        return ApplicationResult.Success();
    }

    public async Task<ApplicationResult> SetImageAsync(
        TenantRequestContext context,
        Guid outletId,
        TenantAdminOutletImageUpdateRequest request,
        CancellationToken cancellationToken)
    {
        var accessError = ValidateManageAccess(context);
        if (accessError is not null) return ApplicationResult.Failure(accessError);

        if (!await _repository.OutletExistsAsync(context.TenantId, outletId, cancellationToken))
        {
            return ApplicationResult.Failure(NotFound);
        }

        if (!await _repository.MediaAssetExistsAndActiveAsync(context.TenantId, request.MediaAssetId, cancellationToken))
        {
            return ApplicationResult.Failure(new ApplicationError("media_asset.not_found", "Active media asset was not found."));
        }

        var now = DateTimeOffset.UtcNow;
        var success = await _repository.SetOutletImageAsync(
            context.TenantId,
            outletId,
            request.MediaAssetId,
            context.UserId,
            now,
            cancellationToken);

        if (!success)
        {
            return ApplicationResult.Failure(new ApplicationError("outlet.image_assignment_failed", "Failed to assign outlet image."));
        }

        _auditLogger?.LogImageAssociated(context.TenantId, context.UserId, outletId, request.MediaAssetId);
        return ApplicationResult.Success();
    }

    public async Task<ApplicationResult> RemoveImageAsync(
        TenantRequestContext context,
        Guid outletId,
        CancellationToken cancellationToken)
    {
        var accessError = ValidateManageAccess(context);
        if (accessError is not null) return ApplicationResult.Failure(accessError);

        if (!await _repository.OutletExistsAsync(context.TenantId, outletId, cancellationToken))
        {
            return ApplicationResult.Failure(NotFound);
        }

        var now = DateTimeOffset.UtcNow;
        await _repository.RemoveOutletImageAsync(
            context.TenantId,
            outletId,
            context.UserId,
            now,
            cancellationToken);

        _auditLogger?.LogImageRemoved(context.TenantId, context.UserId, outletId);
        return ApplicationResult.Success();
    }

    public async Task<ApplicationResult> UpdateStatusAsync(
        TenantRequestContext context,
        Guid outletId,
        TenantAdminOutletStatusUpdateRequest request,
        CancellationToken cancellationToken)
    {
        var accessError = ValidateManageAccess(context);
        if (accessError is not null) return ApplicationResult.Failure(accessError);

        if (string.IsNullOrWhiteSpace(request.Status) || !OutletConstants.IsValidWriteStatus(request.Status))
        {
            return ApplicationResult.Failure(new ApplicationError(
                "outlet.invalid_status",
                "Outlet status must be ACTIVE or INACTIVE."));
        }

        var lifecycleState = await _repository.GetLifecycleStateAsync(context.TenantId, outletId, cancellationToken);
        if (lifecycleState is null) return ApplicationResult.Failure(NotFound);

        var status = OutletConstants.NormalizeStatus(request.Status);
        if (status == OutletConstants.InactiveStatus)
        {
            if (lifecycleState.IsDefaultOutlet)
            {
                return ApplicationResult.Failure(new ApplicationError("outlet.default_cannot_disable", "The default outlet cannot be disabled."));
            }

            if (lifecycleState.HasOpenTillSessions || lifecycleState.HasActiveTills || lifecycleState.HasOpenOrders || lifecycleState.HasAllocatedInventory)
            {
                return ApplicationResult.Failure(new ApplicationError(
                    "outlet.disable_conflict",
                    "The outlet cannot be disabled while active tills, sessions, orders, or allocated inventory remain."));
            }
        }

        var updated = await _repository.UpdateStatusAsync(
            context.TenantId,
            outletId,
            status,
            context.UserId,
            DateTimeOffset.UtcNow,
            cancellationToken);
        if (!updated) return ApplicationResult.Failure(NotFound);

        _auditLogger?.LogStatusChanged(context.TenantId, context.UserId, outletId, status);
        return ApplicationResult.Success();
    }

    private async Task<ApplicationResult<TenantAdminOutletDetailResponse>> LoadDetailAsync(
        Guid tenantId,
        Guid outletId,
        CancellationToken cancellationToken)
    {
        var response = await _repository.GetDetailAsync(tenantId, outletId, cancellationToken);
        return response is null
            ? ApplicationResult<TenantAdminOutletDetailResponse>.Failure(NotFound)
            : ApplicationResult<TenantAdminOutletDetailResponse>.Success(response);
    }

    private static ApplicationError? ValidateDetailAccess(TenantRequestContext context)
    {
        return HasAnyPermission(
            context,
            TenantAdminOutletPermissions.View,
            TenantAdminOutletPermissions.DetailsView,
            TenantAdminOutletPermissions.Manage)
            ? null
            : PermissionDenied;
    }

    private static ApplicationError? ValidateManageAccess(TenantRequestContext context)
    {
        return HasAnyPermission(
            context,
            TenantAdminOutletPermissions.Manage,
            TenantAdminOutletPermissions.Update)
            ? null
            : PermissionDenied;
    }

    private static ApplicationError? ValidateRevenueAccess(TenantRequestContext context)
    {
        return HasAnyPermission(
            context,
            TenantAdminOutletPermissions.RevenueView,
            TenantAdminOutletPermissions.ReportsSalesView,
            TenantAdminOutletPermissions.Manage)
            ? null
            : PermissionDenied;
    }

    private static ApplicationError? ValidateUsersAccess(TenantRequestContext context)
    {
        return HasAnyPermission(
            context,
            TenantAdminOutletPermissions.UsersView,
            TenantAdminOutletPermissions.TenantUsersView,
            TenantAdminOutletPermissions.Manage)
            ? null
            : PermissionDenied;
    }

    private static ApplicationError? ValidateTillsAccess(TenantRequestContext context)
    {
        return HasAnyPermission(
            context,
            TenantAdminOutletPermissions.TillsView,
            TenantAdminOutletPermissions.TenantTillsView,
            TenantAdminOutletPermissions.Manage)
            ? null
            : PermissionDenied;
    }

    private static bool HasAnyPermission(TenantRequestContext context, params string[] permissions)
    {
        foreach (var permission in permissions)
        {
            if (context.HasPermission(permission))
            {
                return true;
            }
        }

        return false;
    }
}
