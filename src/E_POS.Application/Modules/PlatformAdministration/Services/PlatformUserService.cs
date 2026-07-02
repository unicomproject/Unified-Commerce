using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.PlatformAdministration.Contracts;
using E_POS.Application.Modules.PlatformAdministration.Dtos;
using E_POS.Domain.Modules.PlatformAdministration.Constants;
using E_POS.Domain.Modules.PlatformAdministration.Entities;

namespace E_POS.Application.Modules.PlatformAdministration.Services;

public sealed class PlatformUserService : IPlatformUserService
{
    private static readonly ApplicationError AccessDenied = new(
        "platform_users.access_denied",
        "Platform user access denied.");

    private static readonly ApplicationError NotFound = new(
        "platform_users.not_found",
        "Platform user was not found.");

    private static readonly ApplicationError ValidationFailed = new(
        "platform_users.validation_failed",
        "Platform user validation failed.");

    private static readonly ApplicationError Conflict = new(
        "platform_users.conflict",
        "Platform user conflict.");

    private static readonly ApplicationError ProtectedRoleDenied = new(
        "platform_users.protected_role_denied",
        "Only an active super administrator can assign the protected super administrator role.");

    private static readonly ApplicationError SuperAdminLockout = new(
        "platform_users.super_admin_lockout",
        "Cannot remove the last active super administrator or deactivate the only active super administrator.");

    private static readonly HashSet<string> AllowedStatuses =
    [
        PlatformAuthConstants.ActiveStatus,
        PlatformAuthConstants.InactiveStatus,
        PlatformAuthConstants.LockedStatus,
        PlatformAuthConstants.DeletedStatus
    ];

    private static readonly HashSet<string> DeactivatedStatuses =
    [
        PlatformAuthConstants.InactiveStatus,
        PlatformAuthConstants.LockedStatus,
        PlatformAuthConstants.DeletedStatus
    ];

    private readonly IPlatformUserRepository _userRepository;
    private readonly IPlatformPermissionChecker _permissionChecker;
    private readonly IDateTimeProvider _dateTimeProvider;

    public PlatformUserService(
        IPlatformUserRepository userRepository,
        IPlatformPermissionChecker permissionChecker,
        IDateTimeProvider dateTimeProvider)
    {
        _userRepository = userRepository;
        _permissionChecker = permissionChecker;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<ApplicationResult<PlatformUserListResponse>> GetUsersAsync(
        Guid platformUserId,
        CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(platformUserId, PlatformPermissionCodes.UsersView, cancellationToken))
        {
            return ApplicationResult<PlatformUserListResponse>.Failure(AccessDenied);
        }

        var users = await _userRepository.GetUsersAsync(cancellationToken);
        return ApplicationResult<PlatformUserListResponse>.Success(users);
    }

    public async Task<ApplicationResult<PlatformUserDetailResponse>> GetUserAsync(
        Guid userId,
        Guid platformUserId,
        CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(platformUserId, PlatformPermissionCodes.UsersView, cancellationToken))
        {
            return ApplicationResult<PlatformUserDetailResponse>.Failure(AccessDenied);
        }

        var user = await _userRepository.GetUserByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return ApplicationResult<PlatformUserDetailResponse>.Failure(NotFound);
        }

        return ApplicationResult<PlatformUserDetailResponse>.Success(user);
    }

    public async Task<ApplicationResult<PlatformUserDetailResponse>> CreateUserAsync(
        CreatePlatformUserRequest request,
        Guid platformUserId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!await HasPermissionAsync(platformUserId, PlatformPermissionCodes.UsersCreate, cancellationToken))
        {
            return ApplicationResult<PlatformUserDetailResponse>.Failure(AccessDenied);
        }

        var email = NormalizeEmail(request.Email);
        if (string.IsNullOrWhiteSpace(email))
        {
            return ApplicationResult<PlatformUserDetailResponse>.Failure(
                ValidationFailed with { Message = "Email is required." });
        }

        var normalizedEmail = PlatformUser.NormalizeEmail(email);
        if (await _userRepository.EmailExistsAsync(normalizedEmail, null, cancellationToken))
        {
            return ApplicationResult<PlatformUserDetailResponse>.Failure(
                Conflict with { Message = "A platform user with this email already exists." });
        }

        var roleResolution = await ResolveRequestedRolesAsync(request.RoleIds, request.RoleCodes, cancellationToken);
        if (roleResolution.IsFailure)
        {
            return ApplicationResult<PlatformUserDetailResponse>.Failure(roleResolution.Error);
        }

        var roles = roleResolution.Value!;
        if (roles.Count == 0)
        {
            return ApplicationResult<PlatformUserDetailResponse>.Failure(
                ValidationFailed with { Message = "At least one platform role is required." });
        }

        var protectedRoleCheck = await ValidateProtectedRoleAssignmentAsync(
            platformUserId,
            [],
            roles,
            cancellationToken);

        if (protectedRoleCheck is not null)
        {
            return ApplicationResult<PlatformUserDetailResponse>.Failure(protectedRoleCheck);
        }

        var status = NormalizeCreateStatus(request.Status);
        if (!AllowedStatuses.Contains(status))
        {
            return ApplicationResult<PlatformUserDetailResponse>.Failure(
                ValidationFailed with { Message = "Invalid platform user status." });
        }

        var now = _dateTimeProvider.UtcNow;
        var user = PlatformUser.CreatePendingInvite(Guid.NewGuid(), email, now);
        if (!string.Equals(status, PlatformAuthConstants.InactiveStatus, StringComparison.Ordinal))
        {
            user.SetStatus(status, now);
        }

        await _userRepository.AddUserWithRolesAsync(
            user,
            roles.Select(role => role.Id).ToList(),
            now,
            cancellationToken);

        var createdUser = await _userRepository.GetUserByIdAsync(user.Id, cancellationToken);
        return ApplicationResult<PlatformUserDetailResponse>.Success(createdUser!);
    }

    public async Task<ApplicationResult<PlatformUserDetailResponse>> UpdateUserAsync(
        Guid userId,
        UpdatePlatformUserRequest request,
        Guid platformUserId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!await HasPermissionAsync(platformUserId, PlatformPermissionCodes.UsersUpdate, cancellationToken))
        {
            return ApplicationResult<PlatformUserDetailResponse>.Failure(AccessDenied);
        }

        var user = await _userRepository.GetUserEntityByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return ApplicationResult<PlatformUserDetailResponse>.Failure(NotFound);
        }

        if (string.IsNullOrWhiteSpace(request.Status))
        {
            return ApplicationResult<PlatformUserDetailResponse>.Failure(
                ValidationFailed with { Message = "Status is required." });
        }

        var status = request.Status.Trim().ToUpperInvariant();
        if (!AllowedStatuses.Contains(status))
        {
            return ApplicationResult<PlatformUserDetailResponse>.Failure(
                ValidationFailed with { Message = "Invalid platform user status." });
        }

        if (DeactivatedStatuses.Contains(status))
        {
            var lockoutError = await ValidateSuperAdminLockoutAsync(
                userId,
                platformUserId,
                currentRoleCodes: null,
                nextRoleCodes: null,
                nextStatus: status,
                cancellationToken);

            if (lockoutError is not null)
            {
                return ApplicationResult<PlatformUserDetailResponse>.Failure(lockoutError);
            }
        }

        var now = _dateTimeProvider.UtcNow;
        user.SetStatus(status, now);
        await _userRepository.UpdateUserAsync(user, cancellationToken);

        var updatedUser = await _userRepository.GetUserByIdAsync(userId, cancellationToken);
        return ApplicationResult<PlatformUserDetailResponse>.Success(updatedUser!);
    }

    public async Task<ApplicationResult<PlatformUserDetailResponse>> AssignRolesAsync(
        Guid userId,
        AssignPlatformUserRolesRequest request,
        Guid platformUserId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!await HasPermissionAsync(platformUserId, PlatformPermissionCodes.UsersRolesAssign, cancellationToken))
        {
            return ApplicationResult<PlatformUserDetailResponse>.Failure(AccessDenied);
        }

        var user = await _userRepository.GetUserEntityByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return ApplicationResult<PlatformUserDetailResponse>.Failure(NotFound);
        }

        var roleResolution = await ResolveRequestedRolesAsync(request.RoleIds, request.RoleCodes, cancellationToken);
        if (roleResolution.IsFailure)
        {
            return ApplicationResult<PlatformUserDetailResponse>.Failure(roleResolution.Error);
        }

        var roles = roleResolution.Value!;
        if (roles.Count == 0)
        {
            return ApplicationResult<PlatformUserDetailResponse>.Failure(
                ValidationFailed with { Message = "At least one platform role is required." });
        }

        var currentRoleCodes = await _userRepository.GetUserActiveRoleCodesAsync(userId, cancellationToken);
        var nextRoleCodes = roles.Select(role => role.RoleCode).ToList();

        var protectedRoleCheck = await ValidateProtectedRoleAssignmentAsync(
            platformUserId,
            currentRoleCodes,
            roles,
            cancellationToken);

        if (protectedRoleCheck is not null)
        {
            return ApplicationResult<PlatformUserDetailResponse>.Failure(protectedRoleCheck);
        }

        var lockoutError = await ValidateSuperAdminLockoutAsync(
            userId,
            platformUserId,
            currentRoleCodes,
            nextRoleCodes,
            nextStatus: null,
            cancellationToken);

        if (lockoutError is not null)
        {
            return ApplicationResult<PlatformUserDetailResponse>.Failure(lockoutError);
        }

        var now = _dateTimeProvider.UtcNow;
        await _userRepository.ReplaceUserRolesAsync(
            userId,
            roles.Select(role => role.Id).ToList(),
            now,
            cancellationToken);

        var updatedUser = await _userRepository.GetUserByIdAsync(userId, cancellationToken);
        return ApplicationResult<PlatformUserDetailResponse>.Success(updatedUser!);
    }

    private async Task<ApplicationResult<IReadOnlyList<ResolvedPlatformRole>>> ResolveRequestedRolesAsync(
        IReadOnlyList<Guid>? roleIds,
        IReadOnlyList<string>? roleCodes,
        CancellationToken cancellationToken)
    {
        var hasRoleIds = roleIds?.Any(id => id != Guid.Empty) == true;
        var hasRoleCodes = roleCodes?.Any(code => !string.IsNullOrWhiteSpace(code)) == true;

        if (!hasRoleIds && !hasRoleCodes)
        {
            return ApplicationResult<IReadOnlyList<ResolvedPlatformRole>>.Failure(
                ValidationFailed with { Message = "RoleIds or roleCodes are required." });
        }

        var resolvedRoles = await _userRepository.ResolveActiveRolesAsync(roleIds, roleCodes, cancellationToken);
        var requestedRoleIds = roleIds?.Where(id => id != Guid.Empty).Distinct().ToList() ?? [];
        var requestedRoleCodes = roleCodes?
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Select(code => code.Trim().ToLowerInvariant())
            .Distinct(StringComparer.Ordinal)
            .ToList() ?? [];

        var unknownRoleIds = requestedRoleIds
            .Except(resolvedRoles.Select(role => role.Id))
            .ToList();

        var unknownRoleCodes = requestedRoleCodes
            .Except(resolvedRoles.Select(role => role.RoleCode), StringComparer.Ordinal)
            .ToList();

        if (unknownRoleIds.Count > 0 || unknownRoleCodes.Count > 0)
        {
            var unknownParts = new List<string>();
            if (unknownRoleIds.Count > 0)
            {
                unknownParts.Add($"roleIds: {string.Join(", ", unknownRoleIds)}");
            }

            if (unknownRoleCodes.Count > 0)
            {
                unknownParts.Add($"roleCodes: {string.Join(", ", unknownRoleCodes)}");
            }

            return ApplicationResult<IReadOnlyList<ResolvedPlatformRole>>.Failure(
                ValidationFailed with
                {
                    Message = $"Unknown platform roles ({string.Join("; ", unknownParts)})."
                });
        }

        return ApplicationResult<IReadOnlyList<ResolvedPlatformRole>>.Success(resolvedRoles);
    }

    private async Task<ApplicationError?> ValidateProtectedRoleAssignmentAsync(
        Guid requesterUserId,
        IReadOnlyList<string> currentRoleCodes,
        IReadOnlyList<ResolvedPlatformRole> requestedRoles,
        CancellationToken cancellationToken)
    {
        var requestedProtectedRoles = requestedRoles
            .Where(role => PlatformRoleProtection.IsProtectedRole(role.RoleCode))
            .ToList();

        if (requestedProtectedRoles.Count == 0)
        {
            return null;
        }

        var requesterIsSuperAdmin = await _userRepository.UserHasActiveRoleCodeAsync(
            requesterUserId,
            PlatformRoleCodes.SuperAdministrator,
            cancellationToken);

        if (!requesterIsSuperAdmin)
        {
            return ProtectedRoleDenied;
        }

        return null;
    }

    private async Task<ApplicationError?> ValidateSuperAdminLockoutAsync(
        Guid targetUserId,
        Guid requesterUserId,
        IReadOnlyList<string>? currentRoleCodes,
        IReadOnlyList<string>? nextRoleCodes,
        string? nextStatus,
        CancellationToken cancellationToken)
    {
        currentRoleCodes ??= await _userRepository.GetUserActiveRoleCodesAsync(targetUserId, cancellationToken);

        var currentlySuperAdmin = currentRoleCodes.Contains(
            PlatformRoleCodes.SuperAdministrator,
            StringComparer.Ordinal);

        if (!currentlySuperAdmin)
        {
            return null;
        }

        var removingSuperAdminRole = nextRoleCodes is not null &&
            !nextRoleCodes.Contains(PlatformRoleCodes.SuperAdministrator, StringComparer.Ordinal);

        var deactivatingUser = nextStatus is not null && DeactivatedStatuses.Contains(nextStatus);

        if (!removingSuperAdminRole && !deactivatingUser)
        {
            return null;
        }

        var otherActiveSuperAdmins = await _userRepository.CountActiveSuperAdministratorsAsync(
            targetUserId,
            cancellationToken);

        if (otherActiveSuperAdmins > 0)
        {
            return null;
        }

        if (targetUserId == requesterUserId)
        {
            return SuperAdminLockout with
            {
                Message = "You cannot deactivate yourself or remove your super administrator role when you are the only active super administrator."
            };
        }

        return SuperAdminLockout;
    }

    private async Task<bool> HasPermissionAsync(
        Guid platformUserId,
        string permissionCode,
        CancellationToken cancellationToken)
    {
        return await _permissionChecker.HasPermissionAsync(platformUserId, permissionCode, cancellationToken);
    }

    private static string NormalizeEmail(string? email)
    {
        return (email ?? string.Empty).Trim();
    }

    private static string NormalizeCreateStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return PlatformAuthConstants.InactiveStatus;
        }

        return status.Trim().ToUpperInvariant();
    }
}
