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

    public TenantAdminOutletService(ITenantAdminOutletRepository repository)
    {
        _repository = repository;
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
