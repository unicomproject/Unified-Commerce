using E_POS.Application.Modules.Tenant.AccessControl.Dtos.TenantAdmin;
using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using E_POS.Domain.Modules.Tenant.TenantAuth.Entities;

namespace E_POS.Application.Modules.Tenant.AccessControl.Contracts;

public interface ITenantAdminUserRepository
{
    Task<TenantAdminUserListResponse> ListAsync(
        Guid tenantId,
        string? search,
        string? status,
        Guid? roleId,
        Guid? outletId,
        int page,
        int pageSize,
        string sortBy,
        string sortDirection,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<RoleOptionResponse>> GetRoleOptionsAsync(
        Guid tenantId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<OutletOptionResponse>> GetOutletOptionsAsync(
        Guid tenantId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<PermissionGroupResponse>> GetPermissionGroupsAsync(
        CancellationToken cancellationToken);

    Task<bool> RoleBelongsToTenantAsync(
        Guid tenantId,
        Guid roleId,
        CancellationToken cancellationToken);

    Task<bool> OutletsBelongToTenantAsync(
        Guid tenantId,
        IReadOnlyCollection<Guid> outletIds,
        CancellationToken cancellationToken);

    Task<bool> EmailExistsForTenantAsync(
        Guid tenantId,
        string normalizedEmail,
        Guid? excludeUserId,
        CancellationToken cancellationToken);

    Task<bool> PermissionIdsExistAsync(
        IReadOnlyCollection<Guid> permissionIds,
        CancellationToken cancellationToken);

    Task<Guid> CreateAsync(
        TenantUser user,
        Guid roleId,
        IReadOnlyCollection<Guid> outletIds,
        IReadOnlyCollection<Guid> overriddenPermissionIds,
        UserInvite? invite,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task<TenantAdminUserDetailResponse?> GetDetailAsync(
        Guid tenantId,
        Guid userId,
        CancellationToken cancellationToken);

    Task<TenantUser?> GetEditableAsync(
        Guid tenantId,
        Guid userId,
        CancellationToken cancellationToken);

    Task ReplaceAssignmentsAsync(
        Guid tenantId,
        Guid userId,
        Guid roleId,
        IReadOnlyCollection<Guid> outletIds,
        bool permissionOverrideEnabled,
        IReadOnlyCollection<Guid> overriddenPermissionIds,
        Guid actingUserId,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);

    Task<bool> HasSalesReferencesAsync(
        Guid tenantId,
        Guid userId,
        CancellationToken cancellationToken);

    Task<bool> HasActiveTillSessionAsync(
        Guid tenantId,
        Guid userId,
        CancellationToken cancellationToken);
}
