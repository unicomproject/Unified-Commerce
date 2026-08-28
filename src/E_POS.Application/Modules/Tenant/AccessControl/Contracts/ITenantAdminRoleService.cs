using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.AccessControl.Dtos.TenantAdmin;

namespace E_POS.Application.Modules.Tenant.AccessControl.Contracts;

public interface ITenantAdminRoleService
{
    Task<ApplicationResult<TenantAdminRoleListResponse>> ListAsync(
        TenantRequestContext context,
        string? search,
        string? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<ApplicationResult<TenantAdminRoleDetailResponse>> GetByIdAsync(
        TenantRequestContext context,
        Guid roleId,
        CancellationToken cancellationToken);

    Task<ApplicationResult<TenantRoleSetupOptionsResponse>> GetSetupOptionsAsync(
        TenantRequestContext context,
        CancellationToken cancellationToken);

    Task<ApplicationResult<TenantAdminRoleDetailResponse>> CreateAsync(
        TenantRequestContext context,
        TenantAdminRoleCreateRequest request,
        CancellationToken cancellationToken,
        string? idempotencyKey = null);

    Task<ApplicationResult<TenantAdminRoleDetailResponse>> UpdateAsync(
        TenantRequestContext context,
        Guid roleId,
        TenantAdminRoleUpdateRequest request,
        CancellationToken cancellationToken);

    Task<ApplicationResult<TenantAdminRoleDetailResponse>> UpdateStatusAsync(
        TenantRequestContext context,
        Guid roleId,
        TenantAdminRoleStatusRequest request,
        CancellationToken cancellationToken);

    Task<ApplicationResult> DeleteAsync(
        TenantRequestContext context,
        Guid roleId,
        DateTimeOffset? expectedUpdatedAt,
        CancellationToken cancellationToken);

    Task<ApplicationResult<TenantPermissionCatalogResponse>> GetPermissionCatalogAsync(
        TenantRequestContext context,
        CancellationToken cancellationToken);

    Task<ApplicationResult<TenantRolePermissionsResponse>> GetPermissionsAsync(
        TenantRequestContext context,
        Guid roleId,
        CancellationToken cancellationToken);

    Task<ApplicationResult<TenantRolePermissionsResponse>> ReplacePermissionsAsync(
        TenantRequestContext context,
        Guid roleId,
        TenantRolePermissionsUpdateRequest request,
        CancellationToken cancellationToken);

    Task<ApplicationResult<TenantRoleAssignmentsResponse>> GetAssignmentsAsync(
        TenantRequestContext context,
        Guid roleId,
        CancellationToken cancellationToken);

    Task<ApplicationResult<TenantRoleAssignmentsResponse>> ReplaceAssignmentsAsync(
        TenantRequestContext context,
        Guid roleId,
        TenantRoleAssignmentsUpdateRequest request,
        CancellationToken cancellationToken);

    Task<ApplicationResult<TenantAdminRoleDetailResponse>> SaveSetupAsync(
        TenantRequestContext context,
        Guid roleId,
        TenantRoleSetupSaveRequest request,
        CancellationToken cancellationToken);
}
