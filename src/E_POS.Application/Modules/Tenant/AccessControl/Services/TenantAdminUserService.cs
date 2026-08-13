using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using E_POS.Application.Common.Idempotency;
using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Common.Security;
using E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;
using E_POS.Application.Modules.Platform.Subscription.Contracts;
using E_POS.Application.Modules.Tenant.AccessControl.Contracts;
using E_POS.Application.Modules.Tenant.AccessControl.Dtos.TenantAdmin;
using E_POS.Application.Modules.Tenant.TenantAuth.Contracts;
using E_POS.Domain.Modules.Platform.Subscription.Constants;
using E_POS.Domain.Modules.Shared.Audit.Entities;
using E_POS.Domain.Modules.Shared.Integration.Entities;
using E_POS.Domain.Modules.Tenant.AccessControl.Constants;
using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using E_POS.Domain.Modules.Tenant.TenantAuth.Entities;

namespace E_POS.Application.Modules.Tenant.AccessControl.Services;

public sealed class TenantAdminUserService : ITenantAdminUserService
{
    private const string CreateUserOperation = "TENANT_ADMIN_CREATE_USER";

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
    private static readonly ApplicationError InvalidIdempotencyKey = new(
        "user.invalid_idempotency_key",
        "A valid Idempotency-Key header is required to create a user.");
    private static readonly ApplicationError InviteNotAvailable = new(
        "user.invite_not_available",
        "No usable pending invitation is available for this user.");
    private static readonly ApplicationError InvalidProfileMedia = new(
        "user.profile_media_invalid",
        "Profile image media is not valid for this user.");

    private readonly IIdempotencyService _idempotencyService;
    private readonly ITenantAdminUserRepository _repository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IPasswordHashService _passwordHashService;
    private readonly ITenantResourceLimitGuard _resourceLimitGuard;
    private readonly ITenantUserStaffCodeService _staffCodeService;
    private readonly IInvitationTokenService _invitationTokenService;
    private readonly Lazy<IInvitationDeliverySecretProtector> _deliverySecretProtector;

    public TenantAdminUserService(
        IIdempotencyService idempotencyService,
        ITenantAdminUserRepository repository,
        IDateTimeProvider dateTimeProvider,
        IPasswordHashService passwordHashService,
        ITenantResourceLimitGuard resourceLimitGuard,
        ITenantUserStaffCodeService staffCodeService,
        IInvitationTokenService invitationTokenService,
        Lazy<IInvitationDeliverySecretProtector> deliverySecretProtector)
    {
        _idempotencyService = idempotencyService;
        _repository = repository;
        _dateTimeProvider = dateTimeProvider;
        _passwordHashService = passwordHashService;
        _resourceLimitGuard = resourceLimitGuard;
        _staffCodeService = staffCodeService;
        _invitationTokenService = invitationTokenService;
        _deliverySecretProtector = deliverySecretProtector;
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

        var now = _dateTimeProvider.UtcNow;
        var roleOptions = await _repository.GetRoleOptionsAsync(context.TenantId, cancellationToken);
        var roles = new List<RoleOptionResponse>(roleOptions.Count);
        foreach (var role in roleOptions)
        {
            var roleValidation = await _repository.ValidateRoleAssignmentAsync(
                context.TenantId,
                role.RoleId,
                context.Permissions,
                now,
                cancellationToken);
            if (roleValidation.IsValid)
            {
                roles.Add(role);
            }
        }
        var outlets = await _repository.GetOutletOptionsAsync(context.TenantId, cancellationToken);
        var permissionGroups = await _repository.GetPermissionGroupsAsync(
            context.TenantId,
            context.Permissions,
            now,
            cancellationToken);

        return ApplicationResult<TenantAdminUserCreateOptionsResponse>.Success(
            new TenantAdminUserCreateOptionsResponse(
                roles,
                outlets,
                permissionGroups,
                TenantAdminUserCreateStatusPolicy.SupportedStatuses));
    }

    public async Task<ApplicationResult<TenantAdminUserDetailResponse>> CreateAsync(
        TenantRequestContext context,
        TenantAdminUserCreateRequest request,
        CancellationToken cancellationToken,
        string? idempotencyKey = null)
    {
        var keyError = ValidateIdempotencyKey(idempotencyKey);
        if (keyError is not null)
        {
            return ApplicationResult<TenantAdminUserDetailResponse>.Failure(keyError);
        }

        var requestHash = ComputeCreateRequestHash(request);
        return await _idempotencyService.ExecuteAsync(
            context.TenantId,
            context.UserId,
            CreateUserOperation,
            idempotencyKey!.Trim(),
            requestHash,
            ct => CreateCoreAsync(context, request, ct),
            cancellationToken);
    }

    private async Task<ApplicationResult<TenantAdminUserDetailResponse>> CreateCoreAsync(
        TenantRequestContext context,
        TenantAdminUserCreateRequest request,
        CancellationToken cancellationToken)
    {
        var createStatus = NormalizeCreateStatus(request);
        var accessError = createStatus == TenantUserConstants.StatusInvited
            ? ValidateAccessAny(context, TenantAdminUserPermissions.Invite, TenantAdminUserPermissions.Manage)
            : ValidateAccessAny(context, TenantAdminUserPermissions.Create, TenantAdminUserPermissions.Manage);
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
        if (createStatus is null)
            return ApplicationResult<TenantAdminUserDetailResponse>.Failure(ValidationFailed("Create status must be Inactive or Invited."));

        var now = _dateTimeProvider.UtcNow;
        var roleValidation = await _repository.ValidateRoleAssignmentAsync(
            context.TenantId,
            request.RoleId,
            context.Permissions,
            now,
            cancellationToken);
        if (!roleValidation.IsValid)
        {
            return ApplicationResult<TenantAdminUserDetailResponse>.Failure(ToAccessError(roleValidation.Failure));
        }

        var outletIds = NormalizeIds(request.OutletIds);
        var outletValidation = await _repository.ValidateOutletSelectionAsync(
            context.TenantId,
            outletIds,
            cancellationToken);
        if (!outletValidation.IsValid)
        {
            return ApplicationResult<TenantAdminUserDetailResponse>.Failure(ToAccessError(outletValidation.Failure));
        }

        var normalizedEmail = TenantUser.NormalizeEmail(request.Email);
        if (await _repository.EmailExistsForTenantAsync(context.TenantId, normalizedEmail, null, cancellationToken))
        {
            return ApplicationResult<TenantAdminUserDetailResponse>.Failure(new ApplicationError(
                "user.duplicate_email",
                "A user with this email already exists for this tenant."));
        }

        if (request.PermissionOverrideEnabled && !context.HasPermission(TenantAdminUserPermissions.PermissionOverride))
            return ApplicationResult<TenantAdminUserDetailResponse>.Failure(PermissionDenied);
        var permissionOverrideEnabled = request.PermissionOverrideEnabled;
        var overriddenPermissionIds = permissionOverrideEnabled
            ? NormalizeIds(request.OverriddenPermissionIds)
            : [];
        var permissionValidation = await _repository.ValidatePermissionOverridesAsync(
            context.TenantId,
            overriddenPermissionIds,
            context.Permissions,
            now,
            cancellationToken);
        if (!permissionValidation.IsValid)
        {
            return ApplicationResult<TenantAdminUserDetailResponse>.Failure(ToAccessError(permissionValidation.Failure));
        }

        if (request.ProfileMediaAssetId.HasValue)
        {
            var mediaValidation = await _repository.ValidateProfileMediaAsync(
                context.TenantId,
                request.ProfileMediaAssetId.Value,
                targetUserId: null,
                cancellationToken);
            if (!mediaValidation.IsValid)
            {
                return ApplicationResult<TenantAdminUserDetailResponse>.Failure(ToProfileMediaError(mediaValidation.Failure));
            }
        }

        var trimmedFullName = request.FullName.Trim();
        var trimmedPhone = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim();
        var userId = Guid.NewGuid();
        var staffCode = await _staffCodeService.GenerateAsync(context.TenantId, now, cancellationToken);

        TenantUser user;
        UserInvite? invite = null;
        TenantUserInviteDeliverySecret? deliverySecret = null;
        IntegrationOutboxMessage? outbox = null;
        var audit = new List<AuditLog>();
        var increasesCountedSeat = createStatus == TenantUserConstants.StatusInvited;

        if (increasesCountedSeat)
        {
            user = TenantUser.Create(userId, context.TenantId, request.Email.Trim(), trimmedFullName, trimmedPhone, trimmedPhone,
                TenantUserConstants.PendingInvitePasswordHash, "empty_salt", TenantUserConstants.StatusInvited, "admin", "admin", null, now,
                request.EmployeeId, staffCode);

            var rawToken = _invitationTokenService.GenerateToken();
            var protectedToken = _deliverySecretProtector.Value.Protect(rawToken);
            var inviteTokenHash = _invitationTokenService.HashToken(rawToken);
            invite = UserInvite.CreatePending(
                Guid.NewGuid(),
                context.TenantId,
                request.Email.Trim(),
                normalizedEmail,
                request.RoleId,
                null,
                inviteTokenHash,
                now.AddDays(7),
                now,
                userId);
            deliverySecret = TenantUserInviteDeliverySecret.Create(Guid.NewGuid(), context.TenantId, userId, invite.Id,
                protectedToken.Ciphertext, protectedToken.KeyVersion, invite.ExpiresAt, now);
            outbox = IntegrationOutboxMessage.Create(Guid.NewGuid(), "tenant.user_invited", "TENANT_USER", userId, 1,
                context.TenantId, Guid.NewGuid(), null,
                JsonSerializer.Serialize(new { tenantId = context.TenantId, tenantUserId = userId, inviteId = invite.Id }),
                $"tenant.user_invited:{invite.Id:N}", now);
        }
        else
        {
            user = TenantUser.Create(
                userId,
                context.TenantId,
                request.Email.Trim(),
                trimmedFullName,
                trimmedPhone,
                trimmedPhone,
                TenantUserConstants.PendingInvitePasswordHash,
                "empty_salt",
                TenantUserConstants.StatusInactive,
                "admin",
                "admin",
                null,
                now,
                request.EmployeeId,
                staffCode);
        }

        if (request.ProfileMediaAssetId.HasValue)
        {
            user.SetProfileMediaAsset(request.ProfileMediaAssetId.Value, context.UserId, now);
        }

        audit.Add(NewAudit(context, userId, "user.created", now));
        audit.Add(NewAudit(context, userId, "user.access_assigned", now));
        if (invite is not null) audit.Add(NewAudit(context, userId, "user.invited", now, invite.Id));
        if (overriddenPermissionIds.Count > 0) audit.Add(NewAudit(context, userId, "user.permission_override_changed", now));
        if (request.ProfileMediaAssetId.HasValue)
        {
            audit.Add(NewAudit(context, userId, "user.profile_image_assigned", now, mediaAssetId: request.ProfileMediaAssetId.Value));
        }

        async Task<ApplicationResult<TenantAdminUserDetailResponse>> PersistAsync(CancellationToken ct)
        {
            await _repository.CreateAsync(user, request.RoleId, outletIds, overriddenPermissionIds, invite, deliverySecret, outbox, audit, now, ct);
            var response = await _repository.GetDetailAsync(context.TenantId, userId, ct);
            return response is null
                ? ApplicationResult<TenantAdminUserDetailResponse>.Failure(NotFound)
                : ApplicationResult<TenantAdminUserDetailResponse>.Success(response);
        }

        if (!increasesCountedSeat)
        {
            return await PersistAsync(cancellationToken);
        }

        var guarded = await _resourceLimitGuard.ExecuteWithinCapacityAsync(
            context.TenantId,
            TenantSubscriptionLimitKeys.MaxUsers,
            requestedIncrease: 1,
            async ct =>
            {
                var persisted = await PersistAsync(ct);
                return persisted.IsSuccess
                    ? TenantResourceCapacityOperationResult<ApplicationResult<TenantAdminUserDetailResponse>>.Succeeded(persisted)
                    : TenantResourceCapacityOperationResult<ApplicationResult<TenantAdminUserDetailResponse>>.Aborted(persisted);
            },
            cancellationToken);

        if (!guarded.Allowed)
        {
            return ApplicationResult<TenantAdminUserDetailResponse>.Failure(
                guarded.Evaluation.ToApplicationError() ??
                new ApplicationError(SubscriptionLimitErrorCodes.LimitReached, "User subscription limit reached."));
        }

        return guarded.Value!;
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

        var now = _dateTimeProvider.UtcNow;
        var roleValidation = await _repository.ValidateRoleAssignmentAsync(
            context.TenantId,
            request.RoleId,
            context.Permissions,
            now,
            cancellationToken);
        if (!roleValidation.IsValid)
        {
            return ApplicationResult<TenantAdminUserDetailResponse>.Failure(ToAccessError(roleValidation.Failure));
        }

        var outletIds = NormalizeIds(request.OutletIds);
        var outletValidation = await _repository.ValidateOutletSelectionAsync(
            context.TenantId,
            outletIds,
            cancellationToken);
        if (!outletValidation.IsValid)
        {
            return ApplicationResult<TenantAdminUserDetailResponse>.Failure(ToAccessError(outletValidation.Failure));
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
            ? NormalizeIds(request.OverriddenPermissionIds)
            : [];
        var permissionValidation = await _repository.ValidatePermissionOverridesAsync(
            context.TenantId,
            overriddenPermissionIds,
            context.Permissions,
            now,
            cancellationToken);
        if (!permissionValidation.IsValid)
        {
            return ApplicationResult<TenantAdminUserDetailResponse>.Failure(ToAccessError(permissionValidation.Failure));
        }

        var previousStatus = user.AccountStatus;
        var nextStatus = request.Status.Trim().ToUpperInvariant();
        var increasesSeat = !CountsTowardUserLimit(previousStatus) && CountsTowardUserLimit(nextStatus);
        var previousProfileMediaAssetId = user.ProfileImageUrl;
        var profileMediaChange = NormalizeProfileMediaChange(request, previousProfileMediaAssetId);
        if (!profileMediaChange.IsValid)
        {
            return ApplicationResult<TenantAdminUserDetailResponse>.Failure(ValidationFailed(
                "Profile media action must be Keep, Replace, or Remove."));
        }

        if (profileMediaChange.RequiresValidation && profileMediaChange.NextMediaAssetId.HasValue)
        {
            var mediaValidation = await _repository.ValidateProfileMediaAsync(
                context.TenantId,
                profileMediaChange.NextMediaAssetId.Value,
                userId,
                cancellationToken);
            if (!mediaValidation.IsValid)
            {
                return ApplicationResult<TenantAdminUserDetailResponse>.Failure(ToProfileMediaError(mediaValidation.Failure));
            }
        }

        async Task PersistUpdateAsync(CancellationToken ct)
        {
            user.UpdateProfile(
                request.FullName.Trim(),
                request.Email.Trim(),
                request.PhoneNumber?.Trim(),
                nextStatus,
                now);

            if (profileMediaChange.ShouldApply)
            {
                user.SetProfileMediaAsset(profileMediaChange.NextMediaAssetId, context.UserId, now);
                await _repository.ApplyProfileMediaChangeAsync(
                    context.TenantId,
                    userId,
                    context.UserId,
                    previousProfileMediaAssetId,
                    profileMediaChange.NextMediaAssetId,
                    profileMediaChange.AuditAction!,
                    now,
                    ct);
            }

            await _repository.ReplaceAssignmentsAsync(
                context.TenantId,
                userId,
                request.RoleId,
                outletIds,
                permissionOverrideEnabled,
                overriddenPermissionIds,
                context.UserId,
                now,
                ct);

            await _repository.SaveChangesAsync(ct);
        }

        if (increasesSeat)
        {
            var guarded = await _resourceLimitGuard.ExecuteWithinCapacityAsync(
                context.TenantId,
                TenantSubscriptionLimitKeys.MaxUsers,
                requestedIncrease: 1,
                async ct =>
                {
                    await PersistUpdateAsync(ct);
                    return TenantResourceCapacityOperationResult<bool>.Succeeded(true);
                },
                cancellationToken);

            if (!guarded.Allowed)
            {
                return ApplicationResult<TenantAdminUserDetailResponse>.Failure(
                    guarded.Evaluation.ToApplicationError() ??
                    new ApplicationError(SubscriptionLimitErrorCodes.LimitReached, "User subscription limit reached."));
            }
        }
        else
        {
            await PersistUpdateAsync(cancellationToken);
        }

        var response = await _repository.GetDetailAsync(context.TenantId, userId, cancellationToken);
        return response is null
            ? ApplicationResult<TenantAdminUserDetailResponse>.Failure(NotFound)
            : ApplicationResult<TenantAdminUserDetailResponse>.Success(response);
    }

    public async Task<ApplicationResult<TenantAdminUserDetailResponse>> ResendInviteAsync(
        TenantRequestContext context,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var accessError = ValidateAccessAny(
            context,
            TenantAdminUserPermissions.Invite,
            TenantAdminUserPermissions.Manage);
        if (accessError is not null)
        {
            return ApplicationResult<TenantAdminUserDetailResponse>.Failure(accessError);
        }

        var now = _dateTimeProvider.UtcNow;
        var rawToken = _invitationTokenService.GenerateToken();
        var protectedToken = _deliverySecretProtector.Value.Protect(rawToken);
        var mutation = await _repository.ResendInviteAsync(
            context.TenantId,
            context.UserId,
            userId,
            _invitationTokenService.HashToken(rawToken),
            protectedToken.Ciphertext,
            protectedToken.KeyVersion,
            now.AddDays(7),
            now,
            cancellationToken);

        return ToInviteMutationResult(mutation);
    }

    public async Task<ApplicationResult<TenantAdminUserDetailResponse>> RevokeInviteAsync(
        TenantRequestContext context,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var accessError = ValidateAccessAny(
            context,
            TenantAdminUserPermissions.Invite,
            TenantAdminUserPermissions.Manage);
        if (accessError is not null)
        {
            return ApplicationResult<TenantAdminUserDetailResponse>.Failure(accessError);
        }

        var mutation = await _repository.RevokeInviteAsync(
            context.TenantId,
            context.UserId,
            userId,
            _dateTimeProvider.UtcNow,
            cancellationToken);

        return ToInviteMutationResult(mutation);
    }

    private static bool CountsTowardUserLimit(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return false;
        }

        var normalized = status.Trim().ToUpperInvariant();
        return normalized is TenantUserConstants.StatusActive or TenantUserConstants.StatusInvited;
    }

    private static string? NormalizeCreateStatus(TenantAdminUserCreateRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.CreateStatus) && !string.IsNullOrWhiteSpace(request.AccountStatus) &&
            !string.Equals(request.CreateStatus.Trim(), request.AccountStatus.Trim(), StringComparison.OrdinalIgnoreCase))
            return null;
        var requested = !string.IsNullOrWhiteSpace(request.AccountStatus)
            ? request.AccountStatus.Trim().ToUpperInvariant()
            : string.IsNullOrWhiteSpace(request.CreateStatus)
            ? (request.SendInviteEmail ? TenantUserConstants.StatusInvited : TenantUserConstants.StatusInactive)
            : request.CreateStatus.Trim().ToUpperInvariant();
        return TenantAdminUserCreateStatusPolicy.Normalize(requested);
    }

    private static AuditLog NewAudit(
        TenantRequestContext context,
        Guid userId,
        string action,
        DateTimeOffset now,
        Guid? inviteId = null,
        Guid? mediaAssetId = null) => new()
    {
        TenantId = context.TenantId,
        ActorUserId = context.UserId,
        ActorType = "TENANT_USER",
        EntityType = "TENANT_USER",
        EntityId = userId,
        Action = action,
        NewValues = inviteId.HasValue || mediaAssetId.HasValue
            ? JsonSerializer.Serialize(new { inviteId, mediaAssetId })
            : null,
        CreatedAt = now
    };

    private static IReadOnlyList<Guid> NormalizeIds(IReadOnlyCollection<Guid>? ids) =>
        ids is null || ids.Count == 0
            ? []
            : ids.Where(id => id != Guid.Empty).Distinct().ToList();

    private static string ComputeCreateRequestHash(TenantAdminUserCreateRequest request)
    {
        var canonical = new
        {
            fullName = request.FullName?.Trim() ?? string.Empty,
            email = string.IsNullOrWhiteSpace(request.Email)
                ? string.Empty
                : TenantUser.NormalizeEmail(request.Email),
            phoneNumber = NormalizeOptionalText(request.PhoneNumber),
            employeeId = NormalizeOptionalText(request.EmployeeId),
            roleId = request.RoleId,
            accountStatus = NormalizeCreateStatus(request) ?? "INVALID",
            outletIds = NormalizeIdsForFingerprint(request.OutletIds),
            permissionOverrideEnabled = request.PermissionOverrideEnabled,
            overriddenPermissionIds = request.PermissionOverrideEnabled
                ? NormalizeIdsForFingerprint(request.OverriddenPermissionIds)
                : [],
            profileMediaAssetId = request.ProfileMediaAssetId,
        };

        var json = JsonSerializer.Serialize(canonical);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json))).ToLowerInvariant();
    }

    private static IReadOnlyList<Guid> NormalizeIdsForFingerprint(IReadOnlyCollection<Guid>? ids) =>
        ids is null || ids.Count == 0
            ? []
            : ids.Where(id => id != Guid.Empty).Distinct().OrderBy(id => id).ToArray();

    private static string? NormalizeOptionalText(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static ApplicationError? ValidateIdempotencyKey(string? idempotencyKey)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return InvalidIdempotencyKey;
        }

        var normalized = idempotencyKey.Trim();
        if (normalized.Length > 100 || !normalized.All(IsSafeIdempotencyKeyCharacter))
        {
            return InvalidIdempotencyKey;
        }

        return null;
    }

    private static bool IsSafeIdempotencyKeyCharacter(char value) =>
        char.IsLetterOrDigit(value) || value is '-' or '_' or '.' or ':';

    private static ApplicationError ToAccessError(TenantAdminUserAccessValidationFailure failure) =>
        failure switch
        {
            TenantAdminUserAccessValidationFailure.RoleNotFound => RoleNotFound,
            TenantAdminUserAccessValidationFailure.RoleWrongTenant => new ApplicationError(
                "user.role_wrong_tenant",
                "Role does not belong to this tenant."),
            TenantAdminUserAccessValidationFailure.RoleInactive => new ApplicationError(
                "user.role_inactive",
                "Role is inactive and cannot be assigned."),
            TenantAdminUserAccessValidationFailure.RoleNotDelegable => new ApplicationError(
                "user.role_not_delegable",
                "You cannot assign a role containing permissions you cannot delegate."),
            TenantAdminUserAccessValidationFailure.OutletNotFound => OutletNotFound,
            TenantAdminUserAccessValidationFailure.OutletWrongTenant => new ApplicationError(
                "user.outlet_wrong_tenant",
                "One or more selected outlets do not belong to this tenant."),
            TenantAdminUserAccessValidationFailure.OutletInactive => new ApplicationError(
                "user.outlet_inactive",
                "One or more selected outlets are inactive or deleted."),
            TenantAdminUserAccessValidationFailure.PermissionNotFound => new ApplicationError(
                "user.permission_not_found",
                "One or more selected permissions were not found."),
            TenantAdminUserAccessValidationFailure.PermissionInactive => new ApplicationError(
                "user.permission_inactive",
                "One or more selected permissions are inactive."),
            TenantAdminUserAccessValidationFailure.PermissionNotAssignable => new ApplicationError(
                "user.permission_not_assignable",
                "One or more selected permissions cannot be assigned."),
            TenantAdminUserAccessValidationFailure.TenantEntitlementMissing => new ApplicationError(
                "user.tenant_entitlement_missing",
                "Tenant is not entitled to one or more selected permissions."),
            TenantAdminUserAccessValidationFailure.ActorCannotDelegate => new ApplicationError(
                "user.permission_not_delegable",
                "You cannot grant one or more selected permissions."),
            TenantAdminUserAccessValidationFailure.InvalidScope => new ApplicationError(
                "user.permission_invalid_scope",
                "One or more selected permissions are not valid for tenant users."),
            _ => InvalidPermissions,
        };

    private static ApplicationError ToProfileMediaError(TenantAdminUserProfileMediaValidationFailure failure) =>
        failure switch
        {
            TenantAdminUserProfileMediaValidationFailure.NotFound => new ApplicationError(
                "user.profile_media_not_found",
                "Profile image media was not found."),
            TenantAdminUserProfileMediaValidationFailure.WrongTenant => new ApplicationError(
                "user.profile_media_wrong_tenant",
                "Profile image media does not belong to this tenant."),
            TenantAdminUserProfileMediaValidationFailure.NotImage => new ApplicationError(
                "user.profile_media_not_image",
                "Profile media must be an image."),
            TenantAdminUserProfileMediaValidationFailure.Deleted => new ApplicationError(
                "user.profile_media_deleted",
                "Profile image media has been deleted."),
            TenantAdminUserProfileMediaValidationFailure.Expired => new ApplicationError(
                "user.profile_media_expired",
                "Profile image media is no longer attachable."),
            TenantAdminUserProfileMediaValidationFailure.IncompatibleOwner => new ApplicationError(
                "user.profile_media_in_use",
                "Profile image media is already attached to another record."),
            TenantAdminUserProfileMediaValidationFailure.NotAttachable => new ApplicationError(
                "user.profile_media_not_attachable",
                "Profile image media is not attachable."),
            _ => InvalidProfileMedia,
        };

    private static ProfileMediaChange NormalizeProfileMediaChange(
        TenantAdminUserUpdateRequest request,
        Guid? previousMediaAssetId)
    {
        var action = string.IsNullOrWhiteSpace(request.ProfileMediaAction)
            ? (request.ProfileMediaAssetId.HasValue ? "REPLACE" : "KEEP")
            : request.ProfileMediaAction.Trim().ToUpperInvariant();

        return action switch
        {
            "KEEP" => ProfileMediaChange.NoChange,
            "REPLACE" when request.ProfileMediaAssetId.HasValue && request.ProfileMediaAssetId != previousMediaAssetId =>
                new ProfileMediaChange(true, true, request.ProfileMediaAssetId, previousMediaAssetId.HasValue
                    ? "user.profile_image_replaced"
                    : "user.profile_image_assigned"),
            "REPLACE" when request.ProfileMediaAssetId.HasValue => ProfileMediaChange.NoChange,
            "REMOVE" when previousMediaAssetId.HasValue =>
                new ProfileMediaChange(true, false, null, "user.profile_image_removed"),
            "REMOVE" => ProfileMediaChange.NoChange,
            _ => ProfileMediaChange.Invalid,
        };
    }

    private sealed record ProfileMediaChange(
        bool IsValid,
        bool RequiresValidation,
        Guid? NextMediaAssetId,
        string? AuditAction)
    {
        public bool ShouldApply => IsValid && AuditAction is not null;
        public static ProfileMediaChange NoChange { get; } = new(true, false, null, null);
        public static ProfileMediaChange Invalid { get; } = new(false, false, null, null);
    }

    private static ApplicationResult<TenantAdminUserDetailResponse> ToInviteMutationResult(
        TenantAdminUserInviteMutationResult mutation) =>
        mutation.Status switch
        {
            TenantAdminUserInviteMutationStatus.Success when mutation.Response is not null =>
                ApplicationResult<TenantAdminUserDetailResponse>.Success(mutation.Response),
            TenantAdminUserInviteMutationStatus.NotFound =>
                ApplicationResult<TenantAdminUserDetailResponse>.Failure(NotFound),
            TenantAdminUserInviteMutationStatus.NotEligible or TenantAdminUserInviteMutationStatus.NoUsableInvite =>
                ApplicationResult<TenantAdminUserDetailResponse>.Failure(InviteNotAvailable),
            _ => ApplicationResult<TenantAdminUserDetailResponse>.Failure(InviteNotAvailable),
        };

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
