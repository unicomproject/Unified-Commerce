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
    DateTimeOffset? LastActiveAt,
    string? RoleDescription = null,
    IReadOnlyList<OutletOptionResponse>? Outlets = null,
    int OutletCount = 0);

public sealed record TenantAdminUserListResponse(
    IReadOnlyList<TenantAdminUserListItemResponse> Items,
    int Page,
    int PageSize,
    int TotalCount);

public sealed record RoleOptionResponse(
    Guid RoleId,
    string RoleName,
    string RoleCode,
    string? RoleDescription = null);

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
    IReadOnlyList<PermissionGroupResponse> PermissionGroups,
    IReadOnlyList<string> SupportedStatuses);

public sealed record TenantAdminUserCreateRequest(
    string FullName,
    string Email,
    string? PhoneNumber,
    Guid RoleId,
    IReadOnlyList<Guid>? OutletIds,
    bool PermissionOverrideEnabled,
    IReadOnlyList<Guid>? OverriddenPermissionIds,
    bool SendInviteEmail,
    string? ProfileImageFile = null,
    string? EmployeeId = null,
    string? CreateStatus = null,
    Guid? ProfileMediaAssetId = null,
    string? AccountStatus = null);

public sealed record TenantAdminUserUpdateRequest(
    string FullName,
    string Email,
    string? PhoneNumber,
    Guid RoleId,
    IReadOnlyList<Guid>? OutletIds,
    bool PermissionOverrideEnabled,
    IReadOnlyList<Guid>? OverriddenPermissionIds,
    string Status,
    string? ProfileImageFile = null,
    Guid? ProfileMediaAssetId = null,
    string? ProfileMediaAction = null);

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
    string? ProfileImageUrl,
    string? RoleDescription = null,
    int OutletCount = 0,
    TenantAdminUserAccessSummaryResponse? AccessSummary = null,
    string? EmployeeId = null,
    string? StaffCode = null);

public sealed record TenantAdminUserAccessSummaryResponse(
    int OutletCount,
    int ModuleCount,
    int PermissionCount);
