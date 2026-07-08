namespace E_POS.Application.Modules.Tenant.AccessControl.Dtos.TenantAdmin;

public sealed record TenantAdminUserListItemResponse(
    Guid UserId,
    string FullName,
    string Email,
    string? PhoneNumber,
    Guid? RoleId,
    string RoleName,
    string OutletName,
    string Status,
    DateTimeOffset? LastActiveAt);

public sealed record TenantAdminUserListResponse(
    IReadOnlyList<TenantAdminUserListItemResponse> Items,
    int Page,
    int PageSize,
    int TotalCount);

public sealed record RoleOptionResponse(
    Guid RoleId,
    string RoleName,
    string RoleCode);

public sealed record OutletOptionResponse(
    Guid OutletId,
    string OutletName,
    string OutletCode,
    string Status);

public sealed record PermissionItemResponse(
    Guid PermissionId,
    string PermissionCode,
    string ActionType,
    string? Description);

public sealed record PermissionGroupResponse(
    string GroupName,
    IReadOnlyList<PermissionItemResponse> Permissions);

public sealed record TenantAdminUserCreateOptionsResponse(
    IReadOnlyList<RoleOptionResponse> Roles,
    IReadOnlyList<OutletOptionResponse> Outlets,
    IReadOnlyList<PermissionGroupResponse> PermissionGroups);

public sealed record TenantAdminUserCreateRequest(
    string FullName,
    string Email,
    string? PhoneNumber,
    Guid RoleId,
    IReadOnlyList<Guid>? OutletIds,
    bool PermissionOverrideEnabled,
    IReadOnlyList<Guid>? OverriddenPermissionIds,
    bool SendInviteEmail,
    string? ProfileImageFile = null);

public sealed record TenantAdminUserUpdateRequest(
    string FullName,
    string Email,
    string? PhoneNumber,
    Guid RoleId,
    IReadOnlyList<Guid>? OutletIds,
    bool PermissionOverrideEnabled,
    IReadOnlyList<Guid>? OverriddenPermissionIds,
    string Status,
    string? ProfileImageFile = null);

public sealed record TenantAdminUserDetailResponse(
    Guid UserId,
    string FullName,
    string Email,
    string? PhoneNumber,
    Guid? RoleId,
    string RoleName,
    IReadOnlyList<OutletOptionResponse> Outlets,
    string Status,
    bool PermissionOverrideEnabled,
    IReadOnlyList<Guid> OverriddenPermissionIds,
    DateTimeOffset? LastActiveAt,
    DateTimeOffset CreatedAt,
    string? ProfileImageUrl);
