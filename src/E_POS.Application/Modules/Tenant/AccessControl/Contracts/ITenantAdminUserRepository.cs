using E_POS.Application.Modules.Tenant.AccessControl.Dtos.TenantAdmin;
using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using E_POS.Domain.Modules.Tenant.TenantAuth.Entities;
using E_POS.Domain.Modules.Shared.Integration.Entities;
using E_POS.Domain.Modules.Shared.Audit.Entities;

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

    Task<IReadOnlyList<TillOptionResponse>> GetTillOptionsAsync(
        Guid tenantId,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<TillOptionResponse>>([]);

    Task<IReadOnlyList<PermissionGroupResponse>> GetPermissionGroupsAsync(
        Guid tenantId,
        IReadOnlyCollection<string> actorPermissionCodes,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task<bool> RoleBelongsToTenantAsync(
        Guid tenantId,
        Guid roleId,
        CancellationToken cancellationToken);

    Task<TenantAdminUserAccessValidationResult> ValidateRoleAssignmentAsync(
        Guid tenantId,
        Guid roleId,
        IReadOnlyCollection<string> actorPermissionCodes,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task<bool> OutletsBelongToTenantAsync(
        Guid tenantId,
        IReadOnlyCollection<Guid> outletIds,
        CancellationToken cancellationToken);

    Task<TenantAdminUserAccessValidationResult> ValidateOutletSelectionAsync(
        Guid tenantId,
        IReadOnlyCollection<Guid> outletIds,
        CancellationToken cancellationToken);

    Task<TenantAdminUserAccessValidationResult> ValidateTillSelectionAsync(
        Guid tenantId,
        IReadOnlyCollection<Guid> tillIds,
        IReadOnlyCollection<Guid> allowedOutletIds,
        bool allowAllTenantOutlets,
        CancellationToken cancellationToken) =>
        Task.FromResult(TenantAdminUserAccessValidationResult.Valid);

    Task<bool> EmailExistsForTenantAsync(
        Guid tenantId,
        string normalizedEmail,
        Guid? excludeUserId,
        CancellationToken cancellationToken);

    Task<bool> PermissionIdsExistAsync(
        IReadOnlyCollection<Guid> permissionIds,
        CancellationToken cancellationToken);

    Task<TenantAdminUserAccessValidationResult> ValidatePermissionOverridesAsync(
        Guid tenantId,
        IReadOnlyCollection<Guid> permissionIds,
        IReadOnlyCollection<string> actorPermissionCodes,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task<TenantAdminUserProfileMediaValidationResult> ValidateProfileMediaAsync(
        Guid tenantId,
        Guid mediaAssetId,
        Guid? targetUserId,
        CancellationToken cancellationToken);

    Task<Guid> CreateAsync(
        TenantUser user,
        Guid roleId,
        IReadOnlyCollection<Guid> outletIds,
        IReadOnlyCollection<Guid> overriddenPermissionIds,
        UserInvite? invite,
        TenantUserInviteDeliverySecret? deliverySecret,
        IntegrationOutboxMessage? outboxMessage,
        IReadOnlyCollection<AuditLog> auditLogs,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task<Guid> CreateAsync(
        TenantUser user,
        Guid roleId,
        string outletAccessScope,
        IReadOnlyCollection<Guid> outletIds,
        IReadOnlyCollection<Guid> overriddenPermissionIds,
        IReadOnlyCollection<Guid> tillIds,
        UserInvite? invite,
        TenantUserInviteDeliverySecret? deliverySecret,
        IntegrationOutboxMessage? outboxMessage,
        IReadOnlyCollection<AuditLog> auditLogs,
        DateTimeOffset now,
        CancellationToken cancellationToken) =>
        CreateAsync(
            user,
            roleId,
            outletIds,
            overriddenPermissionIds,
            invite,
            deliverySecret,
            outboxMessage,
            auditLogs,
            now,
            cancellationToken);

    Task<TenantAdminUserInviteMutationResult> ResendInviteAsync(
        Guid tenantId,
        Guid actorUserId,
        Guid userId,
        string inviteTokenHash,
        string encryptedToken,
        string keyVersion,
        DateTimeOffset expiresAt,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task<TenantAdminUserInviteMutationResult> RevokeInviteAsync(
        Guid tenantId,
        Guid actorUserId,
        Guid userId,
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

    Task ReplaceAssignmentsAsync(
        Guid tenantId,
        Guid userId,
        Guid roleId,
        string outletAccessScope,
        IReadOnlyCollection<Guid> outletIds,
        bool permissionOverrideEnabled,
        IReadOnlyCollection<Guid> overriddenPermissionIds,
        IReadOnlyCollection<Guid> tillIds,
        Guid actingUserId,
        DateTimeOffset now,
        CancellationToken cancellationToken) =>
        ReplaceAssignmentsAsync(
            tenantId,
            userId,
            roleId,
            outletIds,
            permissionOverrideEnabled,
            overriddenPermissionIds,
            actingUserId,
            now,
            cancellationToken);

    Task ApplyProfileMediaChangeAsync(
        Guid tenantId,
        Guid userId,
        Guid actorUserId,
        Guid? previousMediaAssetId,
        Guid? nextMediaAssetId,
        string auditAction,
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

public sealed record TenantAdminUserAccessValidationResult(
    bool IsValid,
    TenantAdminUserAccessValidationFailure Failure = TenantAdminUserAccessValidationFailure.None)
{
    public static TenantAdminUserAccessValidationResult Valid { get; } = new(true);
    public static TenantAdminUserAccessValidationResult Invalid(TenantAdminUserAccessValidationFailure failure) =>
        new(false, failure);
}

public enum TenantAdminUserAccessValidationFailure
{
    None,
    RoleNotFound,
    RoleWrongTenant,
    RoleInactive,
    RoleNotDelegable,
    OutletNotFound,
    OutletWrongTenant,
    OutletInactive,
    PermissionNotFound,
    PermissionInactive,
    PermissionNotAssignable,
    TenantEntitlementMissing,
    ActorCannotDelegate,
    InvalidScope,
    TillNotFound,
    TillWrongTenant,
    TillInactive,
    TillOutsideOutletScope
}

public sealed record TenantAdminUserProfileMediaValidationResult(
    bool IsValid,
    TenantAdminUserProfileMediaValidationFailure Failure = TenantAdminUserProfileMediaValidationFailure.None,
    string? ResolvedUrl = null)
{
    public static TenantAdminUserProfileMediaValidationResult Valid(string? resolvedUrl) => new(true, ResolvedUrl: resolvedUrl);
    public static TenantAdminUserProfileMediaValidationResult Invalid(TenantAdminUserProfileMediaValidationFailure failure) =>
        new(false, failure);
}

public enum TenantAdminUserProfileMediaValidationFailure
{
    None,
    NotFound,
    WrongTenant,
    NotImage,
    NotAttachable,
    Deleted,
    Expired,
    IncompatibleOwner
}

public sealed record TenantAdminUserInviteMutationResult(
    TenantAdminUserInviteMutationStatus Status,
    TenantAdminUserDetailResponse? Response = null,
    Guid? InviteId = null)
{
    public static TenantAdminUserInviteMutationResult Success(TenantAdminUserDetailResponse response, Guid? inviteId = null) =>
        new(TenantAdminUserInviteMutationStatus.Success, response, inviteId);
}

public enum TenantAdminUserInviteMutationStatus
{
    Success,
    NotFound,
    NotEligible,
    NoUsableInvite
}
