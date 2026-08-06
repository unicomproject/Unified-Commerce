using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Dtos.TenantAdmin;

namespace E_POS.Application.Modules.Tenant.OutletTillDevice.Contracts;

public interface ITenantAdminOutletService
{
    Task<ApplicationResult<TenantAdminOutletListResponse>> ListAsync(
        TenantRequestContext context,
        TenantAdminOutletListQuery query,
        CancellationToken cancellationToken);

    Task<ApplicationResult<TenantAdminOutletDetailResponse>> GetDetailAsync(
        TenantRequestContext context,
        Guid outletId,
        CancellationToken cancellationToken);

    Task<ApplicationResult<TenantAdminOutletRevenueSummaryResponse>> GetRevenueSummaryAsync(
        TenantRequestContext context,
        Guid outletId,
        CancellationToken cancellationToken);

    Task<ApplicationResult<TenantAdminOutletUsersResponse>> GetUsersAsync(
        TenantRequestContext context,
        Guid outletId,
        CancellationToken cancellationToken);

    Task<ApplicationResult<TenantAdminOutletTillsResponse>> GetTillsAsync(
        TenantRequestContext context,
        Guid outletId,
        CancellationToken cancellationToken);

    Task<ApplicationResult<TenantAdminOutletOverviewResponse>> GetOverviewAsync(
        TenantRequestContext context,
        Guid outletId,
        CancellationToken cancellationToken);

    Task<ApplicationResult> SetManagerAsync(
        TenantRequestContext context,
        Guid outletId,
        TenantAdminOutletManagerUpdateRequest request,
        CancellationToken cancellationToken);

    Task<ApplicationResult> RemoveManagerAsync(
        TenantRequestContext context,
        Guid outletId,
        CancellationToken cancellationToken);

    Task<ApplicationResult> SetImageAsync(
        TenantRequestContext context,
        Guid outletId,
        TenantAdminOutletImageUpdateRequest request,
        CancellationToken cancellationToken);

    Task<ApplicationResult> RemoveImageAsync(
        TenantRequestContext context,
        Guid outletId,
        CancellationToken cancellationToken);

    Task<ApplicationResult> UpdateStatusAsync(
        TenantRequestContext context,
        Guid outletId,
        TenantAdminOutletStatusUpdateRequest request,
        CancellationToken cancellationToken);
}
