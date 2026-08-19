using E_POS.Application.Modules.Tenant.AccessControl.Dtos.TenantAdmin;
using E_POS.Domain.Modules.Tenant.AccessControl.Entities;

namespace E_POS.Application.Modules.Tenant.AccessControl.Contracts;

public interface ITenantAdminRoleRepository
{
    Task<TenantAdminRoleListResponse> ListAsync(
        Guid tenantId,
        string? search,
        string? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<TenantAdminRoleDetailResponse?> GetDetailAsync(
        Guid tenantId,
        Guid roleId,
        CancellationToken cancellationToken);

    Task<TenantRole?> GetEditableAsync(
        Guid tenantId,
        Guid roleId,
        CancellationToken cancellationToken);

    Task<bool> RoleCodeExistsAsync(
        Guid tenantId,
        string roleCode,
        Guid? excludeRoleId,
        CancellationToken cancellationToken);

    Task<bool> RoleNameExistsAsync(
        Guid tenantId,
        string roleName,
        Guid? excludeRoleId,
        CancellationToken cancellationToken);

    Task AddAsync(
        TenantRole role,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<PermissionDefinition>> GetAssignablePermissionsByCodeAsync(
        Guid tenantId,
        IReadOnlyCollection<string> permissionCodes,
        IReadOnlyCollection<string> actorPermissionCodes,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task<TenantPermissionCatalogResponse> GetPermissionCatalogAsync(
        Guid tenantId,
        IReadOnlyCollection<string> actorPermissionCodes,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task<TenantRolePermissionsResponse?> GetPermissionsAsync(
        Guid tenantId,
        Guid roleId,
        CancellationToken cancellationToken);

    Task ReplacePermissionsAsync(
        Guid tenantId,
        Guid roleId,
        IReadOnlyCollection<Guid> permissionIds,
        Guid actorUserId,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task<TenantRoleAssignmentsResponse?> GetAssignmentsAsync(
        Guid tenantId,
        Guid roleId,
        CancellationToken cancellationToken);

    Task<RoleAssignmentValidationResult> ValidateAssignmentsAsync(
        Guid tenantId,
        IReadOnlyCollection<TenantAdminRoleAssignmentRequest> assignments,
        CancellationToken cancellationToken);

    Task ReplaceAssignmentsAsync(
        Guid tenantId,
        Guid roleId,
        IReadOnlyCollection<TenantAdminRoleAssignmentRequest> assignments,
        Guid actorUserId,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task<bool> WouldRemoveLastAdminAsync(
        Guid tenantId,
        Guid roleId,
        IReadOnlyCollection<Guid>? replacementPermissionIds,
        bool? replacementIsActive,
        CancellationToken cancellationToken);

    Task<bool> WouldReplaceAssignmentsRemoveLastAdminAsync(
        Guid tenantId,
        Guid roleId,
        IReadOnlyCollection<TenantAdminRoleAssignmentRequest> replacementAssignments,
        CancellationToken cancellationToken);

    Task AddAuditAsync(
        Guid tenantId,
        Guid actorUserId,
        Guid roleId,
        string action,
        object? payload,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public sealed record RoleAssignmentValidationResult(
    bool IsValid,
    RoleAssignmentValidationFailure Failure = RoleAssignmentValidationFailure.None)
{
    public static RoleAssignmentValidationResult Valid { get; } = new(true);

    public static RoleAssignmentValidationResult Invalid(RoleAssignmentValidationFailure failure) => new(false, failure);
}

public enum RoleAssignmentValidationFailure
{
    None,
    UserNotFound,
    OutletNotFound,
    OutletInactive,
    InvalidAccessScope,
    MissingOutletSelection
}
