using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Dtos.TenantAdmin;

namespace E_POS.Application.Modules.Tenant.OutletTillDevice.Contracts;

public interface ITenantAdminOutletService
{
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
}
