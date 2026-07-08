using System.Net.Mail;
using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Common.Security;
using E_POS.Application.Modules.Tenant.AccessControl.Contracts;
using E_POS.Application.Modules.Tenant.AccessControl.Dtos.TenantAdmin;
using E_POS.Domain.Modules.Tenant.AccessControl.Constants;
using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using E_POS.Domain.Modules.Tenant.TenantAuth.Entities;

namespace E_POS.Application.Modules.Tenant.AccessControl.Services;

public sealed class TenantAdminUserService : ITenantAdminUserService
{
    private static readonly ApplicationError PermissionDenied = new(
        "user.permission_denied",
        "Permission denied for user management.");
    private static readonly ApplicationError NotFound = new("user.not_found", "User was not found.");
    private static readonly ApplicationError RoleNotFound = new(
        "user.role_not_found",
        "Role was not found for this tenant.");
    private static readonly ApplicationError OutletNotFound = new(
        "user.outlet_not_found",
        "One or more outlets were not found for this tenant.");
    private static readonly ApplicationError InvalidPermissions = new(
        "user.invalid_permissions",
        "One or more selected permissions are invalid.");

    private readonly ITenantAdminUserRepository _repository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IPasswordHashService _passwordHashService;

    public TenantAdminUserService(
        ITenantAdminUserRepository repository,
        IDateTimeProvider dateTimeProvider,
        IPasswordHashService passwordHashService)
    {
        _repository = repository;
        _dateTimeProvider = dateTimeProvider;
        _passwordHashService = passwordHashService;
    }

    public async Task<ApplicationResult<TenantAdminUserListResponse>> ListAsync(
        TenantRequestContext context,
        string? search,
        string? status,
        Guid? roleId,
        Guid? outletId,
        int page,
        int pageSize,
        string? sortBy,
        string? sortDirection,
        CancellationToken cancellationToken)
    {
        var accessError = ValidateAccess(context, TenantAdminUserPermissions.View, TenantAdminUserPermissions.Manage);
        if (accessError is not null)
        {
            return ApplicationResult<TenantAdminUserListResponse>.Failure(accessError);
        }

        if (roleId.HasValue && !await _repository.RoleBelongsToTenantAsync(context.TenantId, roleId.Value, cancellationToken))
        {
            return ApplicationResult<TenantAdminUserListResponse>.Failure(RoleNotFound);
        }

        if (outletId.HasValue &&
            !await _repository.OutletsBelongToTenantAsync(context.TenantId, new[] { outletId.Value }, cancellationToken))
        {
            return ApplicationResult<TenantAdminUserListResponse>.Failure(OutletNotFound);
        }

        var safePage = Math.Max(1, page);
        var safePageSize = Math.Clamp(pageSize, 1, 100);
        var response = await _repository.ListAsync(
            context.TenantId,
            search,
            status,
            roleId,
            outletId,
            safePage,
            safePageSize,
            sortBy ?? "name",
            sortDirection ?? "asc",
            cancellationToken);

        return ApplicationResult<TenantAdminUserListResponse>.Success(response);
    }

    public async Task<ApplicationResult<TenantAdminUserCreateOptionsResponse>> GetCreateOptionsAsync(
        TenantRequestContext context,
        CancellationToken cancellationToken)
    {
        var accessError = ValidateAccessAny(
            context,
            TenantAdminUserPermissions.Create,
            TenantAdminUserPermissions.Invite,
            TenantAdminUserPermissions.Manage);
        if (accessError is not null)
        {
            return ApplicationResult<TenantAdminUserCreateOptionsResponse>.Failure(accessError);
        }

        var roles = await _repository.GetRoleOptionsAsync(context.TenantId, cancellationToken);
        var outlets = await _repository.GetOutletOptionsAsync(context.TenantId, cancellationToken);
        var permissionGroups = await _repository.GetPermissionGroupsAsync(cancellationToken);

        return ApplicationResult<TenantAdminUserCreateOptionsResponse>.Success(
            new TenantAdminUserCreateOptionsResponse(roles, outlets, permissionGroups));
    }

    public async Task<ApplicationResult<TenantAdminUserDetailResponse>> CreateAsync(
        TenantRequestContext context,
        TenantAdminUserCreateRequest request,
        CancellationToken cancellationToken)
    {
        var accessError = ValidateAccessAny(
            context,
            TenantAdminUserPermissions.Create,
            TenantAdminUserPermissions.Invite,
            TenantAdminUserPermissions.Manage);
        if (accessError is not null)
        {
            return ApplicationResult<TenantAdminUserDetailResponse>.Failure(accessError);
        }

        var validationError = ValidateWriteRequest(
            request.FullName,
            request.Email,
            request.PhoneNumber,
            request.RoleId);
        if (validationError is not null)
        {
            return ApplicationResult<TenantAdminUserDetailResponse>.Failure(validationError);
        }

        if (!await _repository.RoleBelongsToTenantAsync(context.TenantId, request.RoleId, cancellationToken))
        {
            return ApplicationResult<TenantAdminUserDetailResponse>.Failure(RoleNotFound);
        }

        var outletIds = request.OutletIds ?? Array.Empty<Guid>();
        if (outletIds.Count > 0 &&
            !await _repository.OutletsBelongToTenantAsync(context.TenantId, outletIds, cancellationToken))
        {
            return ApplicationResult<TenantAdminUserDetailResponse>.Failure(OutletNotFound);
        }

        var normalizedEmail = TenantUser.NormalizeEmail(request.Email);
        if (await _repository.EmailExistsForTenantAsync(context.TenantId, normalizedEmail, null, cancellationToken))
        {
            return ApplicationResult<TenantAdminUserDetailResponse>.Failure(new ApplicationError(
                "user.duplicate_email",
                "A user with this email already exists for this tenant."));
        }

        var permissionOverrideEnabled = request.PermissionOverrideEnabled &&
            context.HasPermission(TenantAdminUserPermissions.PermissionOverride);
        var overriddenPermissionIds = permissionOverrideEnabled
            ? (request.OverriddenPermissionIds ?? Array.Empty<Guid>())
            : Array.Empty<Guid>();
        if (overriddenPermissionIds.Count > 0 &&
            !await _repository.PermissionIdsExistAsync(overriddenPermissionIds, cancellationToken))
        {
            return ApplicationResult<TenantAdminUserDetailResponse>.Failure(InvalidPermissions);
        }

        var now = _dateTimeProvider.UtcNow;
        var trimmedFullName = request.FullName.Trim();
        var trimmedPhone = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim();
        var userId = Guid.NewGuid();

        TenantUser user;
        UserInvite? invite = null;

        if (request.SendInviteEmail)
        {
            user = TenantUser.CreatePendingInvite(
                userId,
                context.TenantId,
                request.Email.Trim(),
                trimmedFullName,
                trimmedPhone,
                trimmedPhone,
                now);

            invite = UserInvite.CreatePending(
                Guid.NewGuid(),
                context.TenantId,
                request.Email.Trim(),
                normalizedEmail,
                request.RoleId,
                null,
                Guid.NewGuid().ToString("N"),
                now.AddDays(7),
                now);
        }
        else
        {
            var placeholderPassword = _passwordHashService.HashPassword(Guid.NewGuid().ToString("N"));
            user = TenantUser.Create(
                userId,
                context.TenantId,
                request.Email.Trim(),
                trimmedFullName,
                trimmedPhone,
                trimmedPhone,
                placeholderPassword,
                "pbkdf2_embedded",
                TenantUserConstants.StatusInactive,
                "admin",
                "admin",
                "HQ",
                now);
        }

        await _repository.CreateAsync(user, request.RoleId, outletIds, overriddenPermissionIds, invite, now, cancellationToken);

        var response = await _repository.GetDetailAsync(context.TenantId, userId, cancellationToken);
        return response is null
            ? ApplicationResult<TenantAdminUserDetailResponse>.Failure(NotFound)
            : ApplicationResult<TenantAdminUserDetailResponse>.Success(response);
    }

    public async Task<ApplicationResult<TenantAdminUserDetailResponse>> GetByIdAsync(
        TenantRequestContext context,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var accessError = ValidateAccessAny(
            context,
            TenantAdminUserPermissions.DetailsView,
            TenantAdminUserPermissions.View,
            TenantAdminUserPermissions.Manage);
        if (accessError is not null)
        {
            return ApplicationResult<TenantAdminUserDetailResponse>.Failure(accessError);
        }

        var response = await _repository.GetDetailAsync(context.TenantId, userId, cancellationToken);
        return response is null
            ? ApplicationResult<TenantAdminUserDetailResponse>.Failure(NotFound)
            : ApplicationResult<TenantAdminUserDetailResponse>.Success(response);
    }

    public async Task<ApplicationResult<TenantAdminUserDetailResponse>> UpdateAsync(
        TenantRequestContext context,
        Guid userId,
        TenantAdminUserUpdateRequest request,
        CancellationToken cancellationToken)
    {
        var accessError = ValidateAccess(context, TenantAdminUserPermissions.Update, TenantAdminUserPermissions.Manage);
        if (accessError is not null)
        {
            return ApplicationResult<TenantAdminUserDetailResponse>.Failure(accessError);
        }

        var validationError = ValidateWriteRequest(
            request.FullName,
            request.Email,
            request.PhoneNumber,
            request.RoleId);
        if (validationError is not null)
        {
            return ApplicationResult<TenantAdminUserDetailResponse>.Failure(validationError);
        }

        if (!TenantUserConstants.StatusActive.Equals(request.Status, StringComparison.OrdinalIgnoreCase) &&
            !TenantUserConstants.StatusInactive.Equals(request.Status, StringComparison.OrdinalIgnoreCase) &&
            !TenantUserConstants.StatusInvited.Equals(request.Status, StringComparison.OrdinalIgnoreCase))
        {
            return ApplicationResult<TenantAdminUserDetailResponse>.Failure(ValidationFailed(
                "Status must be Active, Inactive, or Invited."));
        }

        var user = await _repository.GetEditableAsync(context.TenantId, userId, cancellationToken);
        if (user is null)
        {
            return ApplicationResult<TenantAdminUserDetailResponse>.Failure(NotFound);
        }

        if (!await _repository.RoleBelongsToTenantAsync(context.TenantId, request.RoleId, cancellationToken))
        {
            return ApplicationResult<TenantAdminUserDetailResponse>.Failure(RoleNotFound);
        }

        var outletIds = request.OutletIds ?? Array.Empty<Guid>();
        if (outletIds.Count > 0 &&
            !await _repository.OutletsBelongToTenantAsync(context.TenantId, outletIds, cancellationToken))
        {
            return ApplicationResult<TenantAdminUserDetailResponse>.Failure(OutletNotFound);
        }

        var normalizedEmail = TenantUser.NormalizeEmail(request.Email);
        if (await _repository.EmailExistsForTenantAsync(context.TenantId, normalizedEmail, userId, cancellationToken))
        {
            return ApplicationResult<TenantAdminUserDetailResponse>.Failure(new ApplicationError(
                "user.duplicate_email",
                "A user with this email already exists for this tenant."));
        }

        var permissionOverrideEnabled = request.PermissionOverrideEnabled &&
            context.HasPermission(TenantAdminUserPermissions.PermissionOverride);
        var overriddenPermissionIds = permissionOverrideEnabled
            ? (request.OverriddenPermissionIds ?? Array.Empty<Guid>())
            : Array.Empty<Guid>();
        if (overriddenPermissionIds.Count > 0 &&
            !await _repository.PermissionIdsExistAsync(overriddenPermissionIds, cancellationToken))
        {
            return ApplicationResult<TenantAdminUserDetailResponse>.Failure(InvalidPermissions);
        }

        var now = _dateTimeProvider.UtcNow;
        user.UpdateProfile(request.FullName.Trim(), request.Email.Trim(), request.PhoneNumber?.Trim(), request.Status.Trim().ToUpperInvariant(), now);

        await _repository.ReplaceAssignmentsAsync(
            context.TenantId,
            userId,
            request.RoleId,
            outletIds,
            permissionOverrideEnabled,
            overriddenPermissionIds,
            context.UserId,
            now,
            cancellationToken);

        await _repository.SaveChangesAsync(cancellationToken);

        var response = await _repository.GetDetailAsync(context.TenantId, userId, cancellationToken);
        return response is null
            ? ApplicationResult<TenantAdminUserDetailResponse>.Failure(NotFound)
            : ApplicationResult<TenantAdminUserDetailResponse>.Success(response);
    }

    public async Task<ApplicationResult> DeleteAsync(
        TenantRequestContext context,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var accessError = ValidateAccess(context, TenantAdminUserPermissions.Delete, TenantAdminUserPermissions.Manage);
        if (accessError is not null)
        {
            return ApplicationResult.Failure(accessError);
        }

        var user = await _repository.GetEditableAsync(context.TenantId, userId, cancellationToken);
        if (user is null)
        {
            return ApplicationResult.Failure(NotFound);
        }

        if (userId == context.UserId)
        {
            return ApplicationResult.Failure(new ApplicationError(
                "user.cannot_delete_self",
                "You cannot delete your own account."));
        }

        if (await _repository.HasActiveTillSessionAsync(context.TenantId, userId, cancellationToken))
        {
            return ApplicationResult.Failure(new ApplicationError(
                "user.delete_conflict",
                "User cannot be disabled while an active till session is open."));
        }

        // Users with sales/session history are always disabled (soft-delete) rather than hard-deleted,
        // consistent with the "prefer disable/deactivate" rule for referenced records.
        var now = _dateTimeProvider.UtcNow;
        user.Disable(now);
        await _repository.SaveChangesAsync(cancellationToken);
        return ApplicationResult.Success();
    }

    private static ApplicationError? ValidateWriteRequest(
        string fullName,
        string email,
        string? phoneNumber,
        Guid roleId)
    {
        if (string.IsNullOrWhiteSpace(fullName) || fullName.Trim().Length > 120)
        {
            return ValidationFailed("Full name is required and must be 120 characters or less.");
        }

        if (string.IsNullOrWhiteSpace(email) || email.Trim().Length > 255 || !MailAddress.TryCreate(email.Trim(), out _))
        {
            return ValidationFailed("A valid email address is required.");
        }

        if (!string.IsNullOrWhiteSpace(phoneNumber) && phoneNumber.Trim().Length > 20)
        {
            return ValidationFailed("Phone number must be 20 characters or less.");
        }

        if (roleId == Guid.Empty)
        {
            return ValidationFailed("Role is required.");
        }

        return null;
    }

    private static ApplicationError ValidationFailed(string message) =>
        new("user.validation_failed", message);

    private static ApplicationError? ValidateAccess(
        TenantRequestContext context,
        string requiredPermission,
        string managePermission)
    {
        if (context.TenantId == Guid.Empty || context.UserId == Guid.Empty)
        {
            return new ApplicationError("user.invalid_tenant_context", "Invalid tenant context.");
        }

        return context.HasPermission(requiredPermission) || context.HasPermission(managePermission)
            ? null
            : PermissionDenied;
    }

    private static ApplicationError? ValidateAccessAny(
        TenantRequestContext context,
        params string[] permissions)
    {
        if (context.TenantId == Guid.Empty || context.UserId == Guid.Empty)
        {
            return new ApplicationError("user.invalid_tenant_context", "Invalid tenant context.");
        }

        return permissions.Any(context.HasPermission) ? null : PermissionDenied;
    }
}
