namespace E_POS.Application.Modules.Tenant.AccessControl.Dtos.TenantAdmin;

public sealed record TenantAdminUserListItemResponse(
    Guid UserId,
    string FullName,
    string Email,
    string? PhoneNumber,
    string? StaffCode,
    Guid? RoleId,
    string RoleName,
    string OutletName,
    string Status,
    DateTimeOffset? LastActiveAt,
    string? RoleDescription = null,
    IReadOnlyList<OutletOptionResponse>? Outlets = null,
    int OutletCount = 0,
    string? ProfileImageUrl = null);

public sealed record TenantAdminUserListResponse(
    IReadOnlyList<TenantAdminUserListItemResponse> Items,
    int Page,
    int PageSize,
    int TotalCount);

public sealed record RoleOptionResponse(
    Guid RoleId,
    string RoleName,
    string RoleCode,
    string? RoleDescription = null,
    bool IsActive = true,
    int ModuleCount = 0,
    int PermissionCount = 0,
    IReadOnlyList<string>? ModulePreview = null,
    IReadOnlyList<string>? PermissionPreview = null);

public sealed record OutletOptionResponse(
    Guid OutletId,
    string OutletName,
    string OutletCode,
    string Status);

public sealed record PermissionItemResponse(
    Guid PermissionId,
    string PermissionCode,
    string ActionType,
    string? Description,
    string? PermissionName = null,
    Guid? ModuleId = null,
    string? ModuleCode = null,
    string? ModuleName = null,
    int SortOrder = 0,
    bool IsAssignable = true,
    bool IsLocked = false);

public sealed record PermissionGroupResponse(
    string GroupName,
    IReadOnlyList<PermissionItemResponse> Permissions,
    Guid? ModuleId = null,
    string? ModuleCode = null,
    string? Description = null,
    int SortOrder = 0);

public sealed record TillOptionResponse(
    Guid TillId,
    Guid OutletId,
    string TillName,
    string TillCode,
    string Status);

public sealed record TenantAdminUserCreateCapabilitiesResponse(
    bool SupportsInvitedUserCreation,
    bool SupportsDirectActiveCreation,
    bool SupportsUserPermissionOverrides,
    bool SupportsPermissionDenies,
    bool SupportsAllOutletAccess,
    bool SupportsNoOutletAccess,
    bool SupportsExplicitTillAccess,
    bool SupportsDefaultOutlet,
    bool SupportsDefaultTill,
    bool SupportsAccessStartDate,
    bool SupportsTemporaryPassword,
    bool SupportsForcePasswordChange,
    bool SupportsTwoFactorDuringCreation,
    bool SupportsSaveDraft);

public sealed record TenantAdminUserCreateOptionsResponse(
    IReadOnlyList<RoleOptionResponse> Roles,
    IReadOnlyList<OutletOptionResponse> Outlets,
    IReadOnlyList<PermissionGroupResponse> PermissionGroups,
    IReadOnlyList<string> SupportedStatuses,
    IReadOnlyList<TillOptionResponse>? Tills = null,
    IReadOnlyList<string>? SupportedOutletAccessScopes = null,
    IReadOnlyList<string>? SupportedTillAccessScopes = null,
    TenantAdminUserCreateCapabilitiesResponse? Capabilities = null,
    string? PermissionCatalogVersion = null);

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
    string? AccountStatus = null,
    string? OutletAccessScope = null,
    Guid? DefaultOutletId = null,
    string? TillAccessScope = null,
    IReadOnlyList<Guid>? TillIds = null,
    Guid? DefaultTillId = null,
    string? PermissionCatalogVersion = null,
    IReadOnlyList<Guid>? DeniedPermissionIds = null,
    string? Password = null,
    string? ConfirmPassword = null);

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
    string? ProfileMediaAction = null,
    string? OutletAccessScope = null,
    Guid? DefaultOutletId = null,
    string? TillAccessScope = null,
    IReadOnlyList<Guid>? TillIds = null,
    Guid? DefaultTillId = null,
    string? PermissionCatalogVersion = null,
    IReadOnlyList<Guid>? DeniedPermissionIds = null);

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
    string? StaffCode = null,
    string? RoleCode = null,
    string OutletAccessScope = "ALL_OUTLETS",
    Guid? DefaultOutletId = null,
    string TillAccessScope = "ALL_ACCESSIBLE_TILLS",
    IReadOnlyList<TillOptionResponse>? Tills = null,
    Guid? DefaultTillId = null,
    string? InvitationStatus = null,
    IReadOnlyList<string>? EffectivePermissionCodes = null);

public sealed record TenantAdminUserAccessSummaryResponse(
    int OutletCount,
    int ModuleCount,
    int PermissionCount,
    int TillCount = 0,
    int InheritedPermissionCount = 0,
    int DirectPermissionCount = 0);
