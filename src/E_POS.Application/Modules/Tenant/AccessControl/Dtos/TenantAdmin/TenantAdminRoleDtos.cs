namespace E_POS.Application.Modules.Tenant.AccessControl.Dtos.TenantAdmin;

public sealed record TenantAdminRoleListResponse(
    IReadOnlyList<TenantAdminRoleListItemResponse> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);

public sealed record TenantAdminRoleListItemResponse(
    Guid RoleId,
    string RoleCode,
    string RoleName,
    string? RoleDescription,
    bool IsActive,
    bool IsSystem,
    int PermissionCount,
    int UserCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record TenantAdminRoleDetailResponse(
    Guid RoleId,
    string RoleCode,
    string RoleName,
    string? RoleDescription,
    bool IsActive,
    bool IsSystem,
    int PermissionCount,
    int UserCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record TenantRoleSetupOptionsResponse(
    IReadOnlyList<TenantRoleSetupOptionResponse> Roles);

public sealed record TenantRoleSetupOptionResponse(
    Guid RoleId,
    string RoleCode,
    string RoleName,
    string? RoleDescription,
    bool IsActive,
    bool IsSystem,
    int PermissionCount,
    int UserCount,
    DateTimeOffset UpdatedAt);

public sealed record TenantRoleSetupSaveRequest(
    IReadOnlyList<string>? PermissionCodes,
    IReadOnlyList<TenantAdminRoleAssignmentRequest>? Assignments,
    DateTimeOffset? ExpectedUpdatedAt = null);

public sealed record TenantAdminRoleCreateRequest(
    string RoleName,
    string RoleCode,
    string? RoleDescription,
    IReadOnlyList<string>? PermissionCodes = null,
    IReadOnlyList<TenantAdminRoleAssignmentRequest>? Assignments = null);

public sealed record TenantAdminRoleUpdateRequest(
    string RoleName,
    string RoleCode,
    string? RoleDescription,
    DateTimeOffset? ExpectedUpdatedAt = null);

public sealed record TenantAdminRoleStatusRequest(
    bool IsActive,
    DateTimeOffset? ExpectedUpdatedAt = null);

public sealed record TenantPermissionCatalogResponse(
    IReadOnlyList<TenantPermissionCatalogModuleResponse> Modules);

public sealed record TenantPermissionCatalogModuleResponse(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    string Scope,
    int SortOrder,
    bool IsActive,
    IReadOnlyList<TenantPermissionCatalogFeatureResponse> Features);

public sealed record TenantPermissionCatalogFeatureResponse(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    string? EntitlementKey,
    int SortOrder,
    bool IsActive,
    IReadOnlyList<TenantPermissionCatalogPermissionResponse> Permissions);

public sealed record TenantPermissionCatalogPermissionResponse(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    string Action,
    string Scope,
    int SortOrder,
    bool IsActive,
    string Source,
    bool Assignable = true,
    string? BlockedReason = null);

public sealed record TenantRolePermissionsResponse(
    Guid RoleId,
    string RoleCode,
    string RoleName,
    string RoleScope,
    bool IsSystem,
    IReadOnlyList<string> AssignedPermissionCodes,
    IReadOnlyList<Guid> AssignedPermissionIds,
    DateTimeOffset UpdatedAt);

public sealed record TenantRolePermissionsUpdateRequest(
    IReadOnlyList<string>? PermissionCodes,
    DateTimeOffset? ExpectedUpdatedAt = null);

public sealed record TenantRoleAssignmentsResponse(
    Guid RoleId,
    string RoleCode,
    string RoleName,
    bool IsSystem,
    IReadOnlyList<TenantAdminRoleAssignmentResponse> Assignments,
    DateTimeOffset UpdatedAt);

public sealed record TenantAdminRoleAssignmentResponse(
    Guid UserId,
    string FullName,
    string Email,
    string AccessScope,
    IReadOnlyList<Guid> OutletIds);

public sealed record TenantRoleAssignmentsUpdateRequest(
    IReadOnlyList<TenantAdminRoleAssignmentRequest>? Assignments,
    DateTimeOffset? ExpectedUpdatedAt = null);

public sealed record TenantAdminRoleAssignmentRequest(
    Guid UserId,
    string AccessScope,
    IReadOnlyList<Guid>? OutletIds = null);
