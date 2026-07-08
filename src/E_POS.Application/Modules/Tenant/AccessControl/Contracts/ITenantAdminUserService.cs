using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.AccessControl.Dtos.TenantAdmin;

namespace E_POS.Application.Modules.Tenant.AccessControl.Contracts;

public interface ITenantAdminUserService
{
    Task<ApplicationResult<TenantAdminUserListResponse>> ListAsync(
        TenantRequestContext context,
        string? search,
        string? status,
        Guid? roleId,
        Guid? outletId,
        int page,
        int pageSize,
        string? sortBy,
        string? sortDirection,
        CancellationToken cancellationToken);

    Task<ApplicationResult<TenantAdminUserCreateOptionsResponse>> GetCreateOptionsAsync(
        TenantRequestContext context,
        CancellationToken cancellationToken);

    Task<ApplicationResult<TenantAdminUserDetailResponse>> CreateAsync(
        TenantRequestContext context,
        TenantAdminUserCreateRequest request,
        CancellationToken cancellationToken);

    Task<ApplicationResult<TenantAdminUserDetailResponse>> GetByIdAsync(
        TenantRequestContext context,
        Guid userId,
        CancellationToken cancellationToken);

    Task<ApplicationResult<TenantAdminUserDetailResponse>> UpdateAsync(
        TenantRequestContext context,
        Guid userId,
        TenantAdminUserUpdateRequest request,
        CancellationToken cancellationToken);

    Task<ApplicationResult> DeleteAsync(
        TenantRequestContext context,
        Guid userId,
        CancellationToken cancellationToken);
}
