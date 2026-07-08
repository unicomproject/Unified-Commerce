using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Dtos.TenantAdmin;

namespace E_POS.Application.Modules.Tenant.OutletTillDevice.Contracts;

public interface ITenantAdminTillService
{
    Task<ApplicationResult<TenantAdminTillListResponse>> ListAsync(
        TenantRequestContext context,
        string? search,
        string? status,
        Guid? outletId,
        int page,
        int pageSize,
        string? sortBy,
        string? sortDirection,
        CancellationToken cancellationToken);

    Task<ApplicationResult<TenantAdminTillSummaryResponse>> GetSummaryAsync(
        TenantRequestContext context,
        CancellationToken cancellationToken);

    Task<ApplicationResult<TenantAdminTillDetailResponse>> CreateAsync(
        TenantRequestContext context,
        TenantAdminTillCreateRequest request,
        CancellationToken cancellationToken);

    Task<ApplicationResult<TenantAdminTillDetailResponse>> GetByIdAsync(
        TenantRequestContext context,
        Guid tillId,
        CancellationToken cancellationToken);

    Task<ApplicationResult<TenantAdminTillDetailResponse>> UpdateAsync(
        TenantRequestContext context,
        Guid tillId,
        TenantAdminTillUpdateRequest request,
        CancellationToken cancellationToken);

    Task<ApplicationResult> DeleteAsync(
        TenantRequestContext context,
        Guid tillId,
        CancellationToken cancellationToken);

    Task<ApplicationResult<IReadOnlyList<TenantAdminOutletOptionResponse>>> GetOutletOptionsAsync(
        TenantRequestContext context,
        CancellationToken cancellationToken);
}
