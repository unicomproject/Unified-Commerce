using E_POS.Application.Modules.Tenant.AccessControl.Contracts;
using E_POS.Application.Modules.Tenant.AccessControl.Dtos.TenantAdmin;
using E_POS.Application.Modules.Shared.Media;
using E_POS.Domain.Modules.Platform.Subscription.Constants;
using E_POS.Domain.Modules.Tenant.AccessControl.Constants;
using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Constants;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Constants;
using E_POS.Domain.Modules.Tenant.TenantAuth.Entities;
using E_POS.Domain.Modules.Shared.Audit.Entities;
using E_POS.Domain.Modules.Shared.Integration.Entities;
using E_POS.Infrastructure.Modules.Platform.Subscription.Entitlements;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace E_POS.Infrastructure.Modules.Tenant.AccessControl.Repositories;

public sealed class TenantAdminUserRepository : ITenantAdminUserRepository
{
    private const string SuccessLoginStatus = "SUCCESS";
    private const string OpenSessionStatus = "OPEN";
    private const string ActiveMediaStatus = "ACTIVE";
    private const string InactiveMediaStatus = "INACTIVE";
    private const string DeletePendingMediaStatus = "DELETE_PENDING";
    private const string DeletedMediaStatus = "DELETED";
    private const string ImageAssetType = "IMAGE";

    private readonly EPosDbContext _dbContext;

    public TenantAdminUserRepository(EPosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<TenantAdminUserListResponse> ListAsync(
        Guid tenantId,
        string? search,
        string? status,
        Guid? roleId,
        Guid? outletId,
        int page,
        int pageSize,
        string sortBy,
        string sortDirection,
        CancellationToken cancellationToken)
    {
        var rows = await BuildUserRowsQuery(tenantId).ToListAsync(cancellationToken);
        await AttachOutletAssignmentsAsync(tenantId, rows, cancellationToken);

        var filtered = rows;
        if (!string.IsNullOrWhiteSpace(status))
        {
            filtered = filtered
                .Where(x => string.Equals(x.Status, status.Trim(), StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        if (roleId.HasValue)
        {
            filtered = filtered.Where(x => x.RoleId == roleId.Value).ToList();
        }

        if (outletId.HasValue)
        {
            filtered = filtered.Where(x => x.OutletIds.Contains(outletId.Value)).ToList();
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToUpperInvariant();
            filtered = filtered
                .Where(x =>
                    x.FullName.ToUpperInvariant().Contains(term) ||
                    x.Email.ToUpperInvariant().Contains(term) ||
                    (!string.IsNullOrWhiteSpace(x.Phone) &&
                     x.Phone.ToUpperInvariant().Contains(term)))
                .ToList();
        }

        filtered = ApplySort(filtered, sortBy, sortDirection);

        var totalCount = filtered.Count;
        var pageItems = filtered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(MapListItem)
            .ToList();

        return new TenantAdminUserListResponse(pageItems, page, pageSize, totalCount);
    }

    public async Task<IReadOnlyList<RoleOptionResponse>> GetRoleOptionsAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.TenantRoles
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.IsActive)
            .OrderBy(x => x.RoleName)
            .Select(x => new RoleOptionResponse(
                x.Id,
                x.RoleName,
                x.RoleCode,
                x.RoleDescription))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<OutletOptionResponse>> GetOutletOptionsAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.Outlets
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId &&
                x.Status.ToUpper() != OutletConstants.DeletedStatus &&
                x.Status.ToUpper() != OutletConstants.InactiveStatus)
            .OrderBy(x => x.OutletName)
            .Select(x => new OutletOptionResponse(x.Id, x.OutletName, x.OutletCode, x.Status))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PermissionGroupResponse>> GetPermissionGroupsAsync(
        Guid tenantId,
        IReadOnlyCollection<string> actorPermissionCodes,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var rows = await (
            from permission in _dbContext.PermissionDefinitions.AsNoTracking()
            join feature in _dbContext.PlatformFeatures.AsNoTracking()
                on permission.FeatureId equals feature.Id
            where permission.IsActive &&
                  !permission.IsSystem &&
                  !permission.PermissionCode.StartsWith("platform.")
            orderby feature.SortOrder, feature.Name, permission.PermissionCode
            select new PermissionOptionRow(
                feature.Name,
                permission.Id,
                permission.PermissionCode,
                permission.ActionType,
                permission.Description,
                feature.Id,
                feature.FeatureCode,
                feature.IsCoreFeature)).ToListAsync(cancellationToken);

        var enabledFeatureIds = await GetEnabledFeatureIdsAsync(tenantId, now, cancellationToken);
        rows = rows
            .Where(row => ActorHasPermission(actorPermissionCodes, row.PermissionCode) &&
                          HasRequiredEntitlement(row.PermissionCode, row.FeatureId, row.FeatureCode, row.IsCoreFeature, enabledFeatureIds))
            .ToList();

        return rows
            .GroupBy(x => x.Name)
            .Select(group => new PermissionGroupResponse(
                group.Key,
                group.Select(x => new PermissionItemResponse(x.Id, x.PermissionCode, x.ActionType, x.Description))
                    .ToList()))
            .ToList();
    }

    public Task<bool> RoleBelongsToTenantAsync(
        Guid tenantId,
        Guid roleId,
        CancellationToken cancellationToken)
    {
        return _dbContext.TenantRoles
            .AsNoTracking()
            .AnyAsync(x => x.TenantId == tenantId && x.Id == roleId && x.IsActive, cancellationToken);
    }

    public async Task<TenantAdminUserAccessValidationResult> ValidateRoleAssignmentAsync(
        Guid tenantId,
        Guid roleId,
        IReadOnlyCollection<string> actorPermissionCodes,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var role = await _dbContext.TenantRoles
            .AsNoTracking()
            .Where(x => x.Id == roleId)
            .Select(x => new { x.TenantId, x.IsActive })
            .FirstOrDefaultAsync(cancellationToken);

        if (role is null)
        {
            return TenantAdminUserAccessValidationResult.Invalid(TenantAdminUserAccessValidationFailure.RoleNotFound);
        }

        if (role.TenantId != tenantId)
        {
            return TenantAdminUserAccessValidationResult.Invalid(TenantAdminUserAccessValidationFailure.RoleWrongTenant);
        }

        if (!role.IsActive)
        {
            return TenantAdminUserAccessValidationResult.Invalid(TenantAdminUserAccessValidationFailure.RoleInactive);
        }

        var permissionIds = await _dbContext.TenantRolePermissions
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.TenantRoleId == roleId && x.RevokedAt == null)
            .Select(x => x.PermissionDefinitionId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var validation = await ValidateAssignablePermissionsAsync(
            tenantId,
            permissionIds,
            actorPermissionCodes,
            now,
            allowSystemPermissions: true,
            enforceEntitlements: false,
            cancellationToken);

        return validation.IsValid
            ? TenantAdminUserAccessValidationResult.Valid
            : TenantAdminUserAccessValidationResult.Invalid(TenantAdminUserAccessValidationFailure.RoleNotDelegable);
    }

    public async Task<bool> OutletsBelongToTenantAsync(
        Guid tenantId,
        IReadOnlyCollection<Guid> outletIds,
        CancellationToken cancellationToken)
    {
        if (outletIds.Count == 0)
        {
            return true;
        }

        var matchCount = await _dbContext.Outlets
            .AsNoTracking()
            .CountAsync(
                x => outletIds.Contains(x.Id) && x.TenantId == tenantId && x.Status != OutletConstants.DeletedStatus,
                cancellationToken);

        return matchCount == outletIds.Distinct().Count();
    }

    public async Task<TenantAdminUserAccessValidationResult> ValidateOutletSelectionAsync(
        Guid tenantId,
        IReadOnlyCollection<Guid> outletIds,
        CancellationToken cancellationToken)
    {
        if (outletIds.Count == 0)
        {
            return TenantAdminUserAccessValidationResult.Valid;
        }

        var normalizedIds = outletIds.Distinct().ToList();
        var rows = await _dbContext.Outlets
            .AsNoTracking()
            .Where(x => normalizedIds.Contains(x.Id))
            .Select(x => new { x.Id, x.TenantId, x.Status })
            .ToListAsync(cancellationToken);

        if (rows.Count != normalizedIds.Count)
        {
            return TenantAdminUserAccessValidationResult.Invalid(TenantAdminUserAccessValidationFailure.OutletNotFound);
        }

        if (rows.Any(x => x.TenantId != tenantId))
        {
            return TenantAdminUserAccessValidationResult.Invalid(TenantAdminUserAccessValidationFailure.OutletWrongTenant);
        }

        if (rows.Any(x =>
                string.Equals(x.Status, OutletConstants.DeletedStatus, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(x.Status, OutletConstants.InactiveStatus, StringComparison.OrdinalIgnoreCase)))
        {
            return TenantAdminUserAccessValidationResult.Invalid(TenantAdminUserAccessValidationFailure.OutletInactive);
        }

        return TenantAdminUserAccessValidationResult.Valid;
    }

    public Task<bool> EmailExistsForTenantAsync(
        Guid tenantId,
        string normalizedEmail,
        Guid? excludeUserId,
        CancellationToken cancellationToken)
    {
        return _dbContext.TenantUsers
            .AsNoTracking()
            .AnyAsync(
                x => x.TenantId == tenantId &&
                     x.Email == normalizedEmail &&
                     (!excludeUserId.HasValue || x.Id != excludeUserId.Value),
                cancellationToken);
    }

    public async Task<bool> PermissionIdsExistAsync(
        IReadOnlyCollection<Guid> permissionIds,
        CancellationToken cancellationToken)
    {
        if (permissionIds.Count == 0)
        {
            return true;
        }

        var matchCount = await _dbContext.PermissionDefinitions
            .AsNoTracking()
            .CountAsync(x => permissionIds.Contains(x.Id) && x.IsActive, cancellationToken);

        return matchCount == permissionIds.Distinct().Count();
    }

    public Task<TenantAdminUserAccessValidationResult> ValidatePermissionOverridesAsync(
        Guid tenantId,
        IReadOnlyCollection<Guid> permissionIds,
        IReadOnlyCollection<string> actorPermissionCodes,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        return ValidateAssignablePermissionsAsync(
            tenantId,
            permissionIds,
            actorPermissionCodes,
            now,
            allowSystemPermissions: false,
            enforceEntitlements: true,
            cancellationToken);
    }

    public async Task<TenantAdminUserProfileMediaValidationResult> ValidateProfileMediaAsync(
        Guid tenantId,
        Guid mediaAssetId,
        Guid? targetUserId,
        CancellationToken cancellationToken)
    {
        var mediaAsset = await _dbContext.MediaAssets
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == mediaAssetId, cancellationToken);

        if (mediaAsset is null)
        {
            return TenantAdminUserProfileMediaValidationResult.Invalid(
                TenantAdminUserProfileMediaValidationFailure.NotFound);
        }

        if (mediaAsset.TenantId != tenantId)
        {
            return TenantAdminUserProfileMediaValidationResult.Invalid(
                TenantAdminUserProfileMediaValidationFailure.WrongTenant);
        }

        if (!string.Equals(mediaAsset.AssetType, ImageAssetType, StringComparison.OrdinalIgnoreCase))
        {
            return TenantAdminUserProfileMediaValidationResult.Invalid(
                TenantAdminUserProfileMediaValidationFailure.NotImage);
        }

        if (string.Equals(mediaAsset.Status, DeletedMediaStatus, StringComparison.OrdinalIgnoreCase))
        {
            return TenantAdminUserProfileMediaValidationResult.Invalid(
                TenantAdminUserProfileMediaValidationFailure.Deleted);
        }

        if (string.Equals(mediaAsset.Status, DeletePendingMediaStatus, StringComparison.OrdinalIgnoreCase))
        {
            return TenantAdminUserProfileMediaValidationResult.Invalid(
                TenantAdminUserProfileMediaValidationFailure.Expired);
        }

        if (!string.Equals(mediaAsset.Status, ActiveMediaStatus, StringComparison.OrdinalIgnoreCase))
        {
            return TenantAdminUserProfileMediaValidationResult.Invalid(
                TenantAdminUserProfileMediaValidationFailure.NotAttachable);
        }

        var attachedToOtherUser = await _dbContext.TenantUsers
            .AsNoTracking()
            .AnyAsync(
                x => x.TenantId == tenantId &&
                     x.ProfileImageUrl == mediaAssetId &&
                     (!targetUserId.HasValue || x.Id != targetUserId.Value),
                cancellationToken);
        if (attachedToOtherUser)
        {
            return TenantAdminUserProfileMediaValidationResult.Invalid(
                TenantAdminUserProfileMediaValidationFailure.IncompatibleOwner);
        }

        var attachedToOutlet = await _dbContext.Outlets
            .AsNoTracking()
            .AnyAsync(
                x => x.TenantId == tenantId &&
                     x.PrimaryImageMediaAssetId == mediaAssetId &&
                     x.Status != OutletConstants.DeletedStatus,
                cancellationToken);
        if (attachedToOutlet)
        {
            return TenantAdminUserProfileMediaValidationResult.Invalid(
                TenantAdminUserProfileMediaValidationFailure.IncompatibleOwner);
        }

        return TenantAdminUserProfileMediaValidationResult.Valid(
            MediaUrlResolver.PreferMediaAsset(mediaAsset.PublicUrl, null, mediaAsset.StorageKey));
    }

    public async Task<Guid> CreateAsync(
        TenantUser user,
        Guid roleId,
        IReadOnlyCollection<Guid> outletIds,
        IReadOnlyCollection<Guid> overriddenPermissionIds,
        UserInvite? invite,
        TenantUserInviteDeliverySecret? deliverySecret,
        IntegrationOutboxMessage? outboxMessage,
        IReadOnlyCollection<AuditLog> auditLogs,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        _dbContext.TenantUsers.Add(user);

        if (outletIds.Count == 0)
        {
            _dbContext.TenantUserRoles.Add(TenantUserRole.Create(
                Guid.NewGuid(),
                user.TenantId,
                user.Id,
                roleId,
                null,
                now));
        }
        else
        {
            foreach (var outletId in outletIds.Distinct())
            {
                _dbContext.OutletUserRoles.Add(OutletUserRole.Create(
                    Guid.NewGuid(),
                    user.TenantId,
                    outletId,
                    user.Id,
                    roleId,
                    null,
                    now));
            }
        }

        foreach (var permissionId in overriddenPermissionIds.Distinct())
        {
            _dbContext.TenantUserPermissions.Add(TenantUserPermission.Create(
                Guid.NewGuid(),
                user.TenantId,
                user.Id,
                permissionId,
                null,
                now));
        }

        if (invite is not null)
        {
            _dbContext.UserInvites.Add(invite);
        }
        if (deliverySecret is not null) _dbContext.TenantUserInviteDeliverySecrets.Add(deliverySecret);
        if (outboxMessage is not null) _dbContext.IntegrationOutboxMessages.Add(outboxMessage);
        if (auditLogs.Count > 0) _dbContext.AuditLogs.AddRange(auditLogs);

        await _dbContext.SaveChangesAsync(cancellationToken);
        return user.Id;
    }

    public async Task<TenantAdminUserInviteMutationResult> ResendInviteAsync(
        Guid tenantId,
        Guid actorUserId,
        Guid userId,
        string inviteTokenHash,
        string encryptedToken,
        string keyVersion,
        DateTimeOffset expiresAt,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var user = await LockTenantUserAsync(tenantId, userId, cancellationToken);
            if (user is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return new TenantAdminUserInviteMutationResult(TenantAdminUserInviteMutationStatus.NotFound);
            }

            if (!string.Equals(user.AccountStatus, TenantUserConstants.StatusInvited, StringComparison.OrdinalIgnoreCase))
            {
                await transaction.RollbackAsync(cancellationToken);
                return new TenantAdminUserInviteMutationResult(TenantAdminUserInviteMutationStatus.NotEligible);
            }

            var openInvites = await LockOpenInvitesAsync(tenantId, userId, cancellationToken);
            if (!openInvites.Any(openInvite => openInvite.IsUsableAt(now)))
            {
                await transaction.RollbackAsync(cancellationToken);
                return new TenantAdminUserInviteMutationResult(TenantAdminUserInviteMutationStatus.NoUsableInvite);
            }

            var oldInviteIds = openInvites.Select(openInvite => openInvite.Id).ToArray();
            foreach (var openInvite in openInvites)
            {
                openInvite.Revoke(now);
            }

            var oldSecrets = await _dbContext.TenantUserInviteDeliverySecrets
                .Where(secret =>
                    secret.TenantId == tenantId &&
                    secret.TenantUserId == userId &&
                    oldInviteIds.Contains(secret.InviteId) &&
                    secret.PurgedAt == null)
                .ToListAsync(cancellationToken);
            foreach (var secret in oldSecrets)
            {
                secret.Purge(now);
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            var roleId = await GetActiveRoleAssignmentIdAsync(tenantId, userId, cancellationToken);
            var newInvite = UserInvite.CreatePending(
                Guid.NewGuid(),
                tenantId,
                user.Email,
                TenantUser.NormalizeEmail(user.Email),
                roleId,
                null,
                inviteTokenHash,
                expiresAt,
                now,
                userId);
            var deliverySecret = TenantUserInviteDeliverySecret.Create(
                Guid.NewGuid(),
                tenantId,
                userId,
                newInvite.Id,
                encryptedToken,
                keyVersion,
                newInvite.ExpiresAt,
                now);
            var nextOutboxSequence = ((await _dbContext.IntegrationOutboxMessages
                .Where(message => message.AggregateType == "TENANT_USER" && message.AggregateId == userId)
                .MaxAsync(message => (int?)message.AggregateSequence, cancellationToken)) ?? 0) + 1;
            var outbox = IntegrationOutboxMessage.Create(
                Guid.NewGuid(),
                "tenant.user_invited",
                "TENANT_USER",
                userId,
                nextOutboxSequence,
                tenantId,
                Guid.NewGuid(),
                null,
                System.Text.Json.JsonSerializer.Serialize(new { tenantId, tenantUserId = userId, inviteId = newInvite.Id }),
                $"tenant.user_invited:{newInvite.Id:N}",
                now);

            _dbContext.UserInvites.Add(newInvite);
            _dbContext.TenantUserInviteDeliverySecrets.Add(deliverySecret);
            _dbContext.IntegrationOutboxMessages.Add(outbox);
            _dbContext.AuditLogs.Add(NewInviteAudit(tenantId, actorUserId, userId, "user.invite_resent", newInvite.Id, now));
            await _dbContext.SaveChangesAsync(cancellationToken);

            var response = await GetDetailAsync(tenantId, userId, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return response is null
                ? new TenantAdminUserInviteMutationResult(TenantAdminUserInviteMutationStatus.NotFound)
                : TenantAdminUserInviteMutationResult.Success(response, newInvite.Id);
        }
        catch
        {
            _dbContext.ChangeTracker.Clear();
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    public async Task<TenantAdminUserInviteMutationResult> RevokeInviteAsync(
        Guid tenantId,
        Guid actorUserId,
        Guid userId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var user = await LockTenantUserAsync(tenantId, userId, cancellationToken);
            if (user is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return new TenantAdminUserInviteMutationResult(TenantAdminUserInviteMutationStatus.NotFound);
            }

            if (!string.Equals(user.AccountStatus, TenantUserConstants.StatusInvited, StringComparison.OrdinalIgnoreCase))
            {
                await transaction.RollbackAsync(cancellationToken);
                return new TenantAdminUserInviteMutationResult(TenantAdminUserInviteMutationStatus.NotEligible);
            }

            var openInvites = await LockOpenInvitesAsync(tenantId, userId, cancellationToken);
            if (openInvites.Count > 0)
            {
                var inviteIds = openInvites.Select(invite => invite.Id).ToArray();
                foreach (var invite in openInvites)
                {
                    invite.Revoke(now);
                }

                var secrets = await _dbContext.TenantUserInviteDeliverySecrets
                    .Where(secret =>
                        secret.TenantId == tenantId &&
                        secret.TenantUserId == userId &&
                        inviteIds.Contains(secret.InviteId) &&
                        secret.PurgedAt == null)
                    .ToListAsync(cancellationToken);
                foreach (var secret in secrets)
                {
                    secret.Purge(now);
                }

                _dbContext.AuditLogs.Add(NewInviteAudit(
                    tenantId,
                    actorUserId,
                    userId,
                    "user.invite_revoked",
                    openInvites.OrderByDescending(invite => invite.CreatedAt).First().Id,
                    now));
                await _dbContext.SaveChangesAsync(cancellationToken);
            }

            var response = await GetDetailAsync(tenantId, userId, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return response is null
                ? new TenantAdminUserInviteMutationResult(TenantAdminUserInviteMutationStatus.NotFound)
                : TenantAdminUserInviteMutationResult.Success(response);
        }
        catch
        {
            _dbContext.ChangeTracker.Clear();
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    public async Task<TenantAdminUserDetailResponse?> GetDetailAsync(
        Guid tenantId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var user = await _dbContext.TenantUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == userId, cancellationToken);

        if (user is null)
        {
            return null;
        }

        var roleAssignment = await GetActiveRoleAssignmentAsync(tenantId, userId, cancellationToken);
        var outletIds = await GetActiveOutletIdsAsync(tenantId, userId, cancellationToken);
        var outlets = outletIds.Count == 0
            ? new List<OutletOptionResponse>()
            : await _dbContext.Outlets
                .AsNoTracking()
                .Where(x => outletIds.Contains(x.Id))
                .OrderBy(x => x.OutletName)
                .Select(x => new OutletOptionResponse(x.Id, x.OutletName, x.OutletCode, x.Status))
                .ToListAsync(cancellationToken);

        var overriddenPermissionIds = await _dbContext.TenantUserPermissions
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.TenantUserId == userId && x.RevokedAt == null)
            .Select(x => x.PermissionDefinitionId)
            .ToListAsync(cancellationToken);

        var lastActiveAt = await _dbContext.TenantLoginAudits
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.UserId == userId && x.LoginStatus == SuccessLoginStatus)
            .OrderByDescending(x => x.AttemptedAt)
            .Select(x => (DateTimeOffset?)x.AttemptedAt)
            .FirstOrDefaultAsync(cancellationToken);

        var accessSummary = await GetEffectiveAccessSummaryAsync(
            tenantId,
            userId,
            user.AccountStatus,
            cancellationToken);
        var profileImageUrl = await ResolveProfileImageUrlAsync(tenantId, user.ProfileImageUrl, cancellationToken);

        return new TenantAdminUserDetailResponse(
            user.Id,
            user.FullName,
            user.Email,
            user.UnmaskedPhone ?? user.Phone,
            roleAssignment?.RoleId,
            roleAssignment?.RoleName ?? "-",
            outlets,
            FormatStatus(user.AccountStatus),
            overriddenPermissionIds.Count > 0,
            overriddenPermissionIds,
            lastActiveAt,
            user.CreatedAt,
            profileImageUrl,
            roleAssignment?.RoleDescription,
            accessSummary.OutletCount,
            accessSummary,
            user.EmployeeId,
            user.StaffCode);
    }

    public Task<TenantUser?> GetEditableAsync(
        Guid tenantId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        return _dbContext.TenantUsers
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == userId, cancellationToken);
    }

    public async Task ReplaceAssignmentsAsync(
        Guid tenantId,
        Guid userId,
        Guid roleId,
        IReadOnlyCollection<Guid> outletIds,
        bool permissionOverrideEnabled,
        IReadOnlyCollection<Guid> overriddenPermissionIds,
        Guid actingUserId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var existingTenantRoles = await _dbContext.TenantUserRoles
            .Where(x => x.TenantId == tenantId && x.TenantUserId == userId && x.RevokedAt == null)
            .ToListAsync(cancellationToken);

        var existingOutletRoles = await _dbContext.OutletUserRoles
            .Where(x => x.TenantId == tenantId && x.TenantUserId == userId && x.RevokedAt == null)
            .ToListAsync(cancellationToken);

        var keepsExistingTenantRole =
            outletIds.Count == 0 &&
            existingOutletRoles.Count == 0 &&
            existingTenantRoles.Count == 1 &&
            existingTenantRoles[0].TenantRoleId == roleId;

        if (!keepsExistingTenantRole)
        {
            foreach (var role in existingTenantRoles)
            {
                role.Revoke(now);
            }

            foreach (var role in existingOutletRoles)
            {
                role.Revoke(actingUserId, now);
            }

            if (outletIds.Count == 0)
            {
                _dbContext.TenantUserRoles.Add(TenantUserRole.Create(Guid.NewGuid(), tenantId, userId, roleId, actingUserId, now));
            }
            else
            {
                foreach (var outletId in outletIds.Distinct())
                {
                    _dbContext.OutletUserRoles.Add(OutletUserRole.Create(
                        Guid.NewGuid(),
                        tenantId,
                        outletId,
                        userId,
                        roleId,
                        actingUserId,
                        now));
                }
            }
        }

        var existingPermissions = await _dbContext.TenantUserPermissions
            .Where(x => x.TenantId == tenantId && x.TenantUserId == userId && x.RevokedAt == null)
            .ToListAsync(cancellationToken);
        foreach (var permission in existingPermissions)
        {
            permission.Revoke(now);
        }

        if (permissionOverrideEnabled)
        {
            foreach (var permissionId in overriddenPermissionIds.Distinct())
            {
                _dbContext.TenantUserPermissions.Add(TenantUserPermission.Create(
                    Guid.NewGuid(),
                    tenantId,
                    userId,
                    permissionId,
                    actingUserId,
                    now));
            }
        }
    }

    public async Task ApplyProfileMediaChangeAsync(
        Guid tenantId,
        Guid userId,
        Guid actorUserId,
        Guid? previousMediaAssetId,
        Guid? nextMediaAssetId,
        string auditAction,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (previousMediaAssetId.HasValue && previousMediaAssetId != nextMediaAssetId)
        {
            var previousStillReferenced = await _dbContext.TenantUsers
                .AsNoTracking()
                .AnyAsync(
                    x => x.TenantId == tenantId &&
                         x.Id != userId &&
                         x.ProfileImageUrl == previousMediaAssetId.Value,
                    cancellationToken);

            if (!previousStillReferenced)
            {
                var previousAsset = await _dbContext.MediaAssets
                    .FirstOrDefaultAsync(
                        x => x.TenantId == tenantId && x.Id == previousMediaAssetId.Value,
                        cancellationToken);
                previousAsset?.MarkInactive(actorUserId, now);
            }
        }

        _dbContext.AuditLogs.Add(new AuditLog
        {
            TenantId = tenantId,
            ActorUserId = actorUserId,
            ActorType = "TENANT_USER",
            EntityType = "TENANT_USER",
            EntityId = userId,
            Action = auditAction,
            NewValues = System.Text.Json.JsonSerializer.Serialize(new
            {
                previousMediaAssetId,
                mediaAssetId = nextMediaAssetId
            }),
            CreatedAt = now
        });
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<bool> HasSalesReferencesAsync(
        Guid tenantId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        return _dbContext.SalesOrders
            .AsNoTracking()
            .AnyAsync(
                order => order.TenantId == tenantId && order.CreatedByTenantUserId == userId,
                cancellationToken);
    }

    public Task<bool> HasActiveTillSessionAsync(
        Guid tenantId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        return _dbContext.TillSessions
            .AsNoTracking()
            .AnyAsync(
                session => session.TenantId == tenantId &&
                           session.OpenedByTenantUserId == userId &&
                           session.Status == OpenSessionStatus,
                cancellationToken);
    }

    private Task<TenantUser?> LockTenantUserAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken) =>
        _dbContext.TenantUsers
            .FromSqlInterpolated($@"SELECT * FROM tenant_users WHERE tenant_id = {tenantId} AND id = {userId} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);

    private Task<List<UserInvite>> LockOpenInvitesAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken) =>
        _dbContext.UserInvites
            .FromSqlInterpolated($@"SELECT * FROM user_invites
                WHERE tenant_id = {tenantId}
                  AND tenant_user_id = {userId}
                  AND invite_status IN ('PENDING','SENT')
                FOR UPDATE")
            .ToListAsync(cancellationToken);

    private async Task<Guid?> GetActiveRoleAssignmentIdAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken)
    {
        var tenantRoleId = await _dbContext.TenantUserRoles
            .AsNoTracking()
            .Where(role => role.TenantId == tenantId && role.TenantUserId == userId && role.RevokedAt == null)
            .OrderByDescending(role => role.AssignedAt)
            .Select(role => (Guid?)role.TenantRoleId)
            .FirstOrDefaultAsync(cancellationToken);
        if (tenantRoleId.HasValue)
        {
            return tenantRoleId;
        }

        return await _dbContext.OutletUserRoles
            .AsNoTracking()
            .Where(role => role.TenantId == tenantId && role.TenantUserId == userId && role.RevokedAt == null)
            .OrderByDescending(role => role.AssignedAt)
            .Select(role => (Guid?)role.TenantRoleId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static AuditLog NewInviteAudit(
        Guid tenantId,
        Guid actorUserId,
        Guid userId,
        string action,
        Guid inviteId,
        DateTimeOffset now) => new()
        {
            TenantId = tenantId,
            ActorUserId = actorUserId,
            ActorType = "TENANT_USER",
            EntityType = "TENANT_USER",
            EntityId = userId,
            Action = action,
            NewValues = System.Text.Json.JsonSerializer.Serialize(new { inviteId }),
            CreatedAt = now
        };

    private async Task<(Guid RoleId, string RoleName, string? RoleDescription)?> GetActiveRoleAssignmentAsync(
        Guid tenantId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var tenantRole = await (
            from userRole in _dbContext.TenantUserRoles.AsNoTracking()
            join role in _dbContext.TenantRoles.AsNoTracking() on userRole.TenantRoleId equals role.Id
            where userRole.TenantId == tenantId && userRole.TenantUserId == userId && userRole.RevokedAt == null
            orderby userRole.AssignedAt descending
            select new { role.Id, role.RoleName, role.RoleDescription }
        ).FirstOrDefaultAsync(cancellationToken);

        if (tenantRole is not null)
        {
            return (tenantRole.Id, tenantRole.RoleName, tenantRole.RoleDescription);
        }

        var outletRole = await (
            from userRole in _dbContext.OutletUserRoles.AsNoTracking()
            join role in _dbContext.TenantRoles.AsNoTracking() on userRole.TenantRoleId equals role.Id
            where userRole.TenantId == tenantId && userRole.TenantUserId == userId && userRole.RevokedAt == null
            orderby userRole.AssignedAt descending
            select new { role.Id, role.RoleName, role.RoleDescription }
        ).FirstOrDefaultAsync(cancellationToken);

        return outletRole is null
            ? null
            : (outletRole.Id, outletRole.RoleName, outletRole.RoleDescription);
    }

    private async Task<List<Guid>> GetActiveOutletIdsAsync(
        Guid tenantId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var roleOutletIds = _dbContext.OutletUserRoles
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.TenantUserId == userId && x.RevokedAt == null)
            .Select(x => x.OutletId);

        var permissionOutletIds = _dbContext.OutletUserPermissions
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.TenantUserId == userId && x.RevokedAt == null)
            .Select(x => x.OutletId);

        return await roleOutletIds
            .Concat(permissionOutletIds)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    private async Task<TenantAdminUserAccessSummaryResponse> GetEffectiveAccessSummaryAsync(
        Guid tenantId,
        Guid userId,
        string accountStatus,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(accountStatus, TenantUserConstants.StatusActive, StringComparison.OrdinalIgnoreCase))
        {
            return new TenantAdminUserAccessSummaryResponse(0, 0, 0);
        }

        var outletIds = await GetEffectiveOutletIdsAsync(tenantId, userId, cancellationToken);

        var tenantRolePermissionIds =
            from userRole in _dbContext.TenantUserRoles.AsNoTracking()
            join role in _dbContext.TenantRoles.AsNoTracking() on userRole.TenantRoleId equals role.Id
            join rolePermission in _dbContext.TenantRolePermissions.AsNoTracking() on role.Id equals rolePermission.TenantRoleId
            where userRole.TenantId == tenantId &&
                  userRole.TenantUserId == userId &&
                  userRole.RevokedAt == null &&
                  role.TenantId == tenantId &&
                  role.IsActive &&
                  rolePermission.TenantId == tenantId &&
                  rolePermission.RevokedAt == null
            select rolePermission.PermissionDefinitionId;

        var outletRolePermissionIds =
            from userRole in _dbContext.OutletUserRoles.AsNoTracking()
            join outlet in _dbContext.Outlets.AsNoTracking() on userRole.OutletId equals outlet.Id
            join role in _dbContext.TenantRoles.AsNoTracking() on userRole.TenantRoleId equals role.Id
            join rolePermission in _dbContext.TenantRolePermissions.AsNoTracking() on role.Id equals rolePermission.TenantRoleId
            where userRole.TenantId == tenantId &&
                  userRole.TenantUserId == userId &&
                  userRole.RevokedAt == null &&
                  outlet.TenantId == tenantId &&
                  outlet.Status.ToUpper() != OutletConstants.DeletedStatus &&
                  outlet.Status.ToUpper() != OutletConstants.InactiveStatus &&
                  role.TenantId == tenantId &&
                  role.IsActive &&
                  rolePermission.TenantId == tenantId &&
                  rolePermission.RevokedAt == null
            select rolePermission.PermissionDefinitionId;

        var directPermissionIds = _dbContext.TenantUserPermissions
            .AsNoTracking()
            .Where(permission =>
                permission.TenantId == tenantId &&
                permission.TenantUserId == userId &&
                permission.RevokedAt == null)
            .Select(permission => permission.PermissionDefinitionId);

        var outletPermissionIds =
            from permission in _dbContext.OutletUserPermissions.AsNoTracking()
            join outlet in _dbContext.Outlets.AsNoTracking() on permission.OutletId equals outlet.Id
            where permission.TenantId == tenantId &&
                  permission.TenantUserId == userId &&
                  permission.RevokedAt == null &&
                  outlet.TenantId == tenantId &&
                  outlet.Status.ToUpper() != OutletConstants.DeletedStatus &&
                  outlet.Status.ToUpper() != OutletConstants.InactiveStatus
            select permission.PermissionDefinitionId;

        var permissionIds = await tenantRolePermissionIds
            .Concat(outletRolePermissionIds)
            .Concat(directPermissionIds)
            .Concat(outletPermissionIds)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (permissionIds.Count == 0)
        {
            return new TenantAdminUserAccessSummaryResponse(outletIds.Count, 0, 0);
        }

        var effectivePermissions = await _dbContext.PermissionDefinitions
            .AsNoTracking()
            .Where(permission => permissionIds.Contains(permission.Id) && permission.IsActive)
            .Select(permission => new { permission.Id, permission.ModuleId })
            .ToListAsync(cancellationToken);

        return new TenantAdminUserAccessSummaryResponse(
            outletIds.Count,
            effectivePermissions.Select(permission => permission.ModuleId).Distinct().Count(),
            effectivePermissions.Select(permission => permission.Id).Distinct().Count());
    }

    private async Task<List<Guid>> GetEffectiveOutletIdsAsync(
        Guid tenantId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var assignedOutletIds = await GetActiveOutletIdsAsync(tenantId, userId, cancellationToken);
        var outlets = _dbContext.Outlets
            .AsNoTracking()
            .Where(outlet =>
                outlet.TenantId == tenantId &&
                outlet.Status.ToUpper() != OutletConstants.DeletedStatus &&
                outlet.Status.ToUpper() != OutletConstants.InactiveStatus);

        if (assignedOutletIds.Count > 0)
        {
            outlets = outlets.Where(outlet => assignedOutletIds.Contains(outlet.Id));
        }

        return await outlets
            .Select(outlet => outlet.Id)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    private async Task<string?> ResolveProfileImageUrlAsync(
        Guid tenantId,
        Guid? profileMediaAssetId,
        CancellationToken cancellationToken)
    {
        if (!profileMediaAssetId.HasValue)
        {
            return null;
        }

        var mediaAsset = await _dbContext.MediaAssets
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId &&
                x.Id == profileMediaAssetId.Value &&
                x.Status == ActiveMediaStatus)
            .Select(x => new { x.PublicUrl, x.StorageKey })
            .FirstOrDefaultAsync(cancellationToken);

        return mediaAsset is null
            ? null
            : MediaUrlResolver.PreferMediaAsset(mediaAsset.PublicUrl, null, mediaAsset.StorageKey);
    }

    private IQueryable<UserRow> BuildUserRowsQuery(Guid tenantId)
    {
        return from user in _dbContext.TenantUsers.AsNoTracking()
               where user.TenantId == tenantId
               let tenantRole = (
                   from userRole in _dbContext.TenantUserRoles.AsNoTracking()
                   join role in _dbContext.TenantRoles.AsNoTracking() on userRole.TenantRoleId equals role.Id
                   where userRole.TenantId == tenantId && userRole.TenantUserId == user.Id && userRole.RevokedAt == null
                   orderby userRole.AssignedAt descending
                   select new { role.Id, role.RoleName, role.RoleDescription }
               ).FirstOrDefault()
               let outletRole = (
                   from userRole in _dbContext.OutletUserRoles.AsNoTracking()
                   join role in _dbContext.TenantRoles.AsNoTracking() on userRole.TenantRoleId equals role.Id
                   where userRole.TenantId == tenantId && userRole.TenantUserId == user.Id && userRole.RevokedAt == null
                   orderby userRole.AssignedAt descending
                   select new { role.Id, role.RoleName, role.RoleDescription }
               ).FirstOrDefault()
               let lastActiveAt = _dbContext.TenantLoginAudits
                   .Where(x => x.TenantId == tenantId && x.UserId == user.Id && x.LoginStatus == SuccessLoginStatus)
                   .OrderByDescending(x => x.AttemptedAt)
                   .Select(x => (DateTimeOffset?)x.AttemptedAt)
                   .FirstOrDefault()
               select new UserRow
               {
                   UserId = user.Id,
                   FullName = user.FullName,
                   Email = user.Email,
                   Phone = user.UnmaskedPhone ?? user.Phone,
                   RoleId = tenantRole != null ? tenantRole.Id : (outletRole != null ? outletRole.Id : (Guid?)null),
                   RoleName = tenantRole != null ? tenantRole.RoleName : (outletRole != null ? outletRole.RoleName : "-"),
                   RoleDescription = tenantRole != null
                       ? tenantRole.RoleDescription
                       : (outletRole != null ? outletRole.RoleDescription : null),
                   Status = user.AccountStatus,
                   LastActiveAt = lastActiveAt,
               };
    }

    private async Task AttachOutletAssignmentsAsync(
        Guid tenantId,
        List<UserRow> rows,
        CancellationToken cancellationToken)
    {
        if (rows.Count == 0)
        {
            return;
        }

        var userIds = rows.Select(x => x.UserId).ToList();
        var activeOutletCount = await _dbContext.Outlets
            .AsNoTracking()
            .CountAsync(
                outlet => outlet.TenantId == tenantId &&
                          outlet.Status.ToUpper() != OutletConstants.DeletedStatus &&
                          outlet.Status.ToUpper() != OutletConstants.InactiveStatus,
                cancellationToken);
        var assignments = await (
            from userRole in _dbContext.OutletUserRoles.AsNoTracking()
            join outlet in _dbContext.Outlets.AsNoTracking() on userRole.OutletId equals outlet.Id
            where userRole.TenantId == tenantId &&
                  userIds.Contains(userRole.TenantUserId) &&
                  userRole.RevokedAt == null
            select new
            {
                userRole.TenantUserId,
                outlet.Id,
                outlet.OutletName,
                outlet.OutletCode,
                outlet.Status,
            }
        ).ToListAsync(cancellationToken);

        var directPermissionAssignments = await (
            from permission in _dbContext.OutletUserPermissions.AsNoTracking()
            join outlet in _dbContext.Outlets.AsNoTracking() on permission.OutletId equals outlet.Id
            where permission.TenantId == tenantId &&
                  userIds.Contains(permission.TenantUserId) &&
                  permission.RevokedAt == null &&
                  outlet.TenantId == tenantId
            select new
            {
                permission.TenantUserId,
                outlet.Id,
                outlet.OutletName,
                outlet.OutletCode,
                outlet.Status,
            }
        ).ToListAsync(cancellationToken);

        var byUser = assignments
            .Concat(directPermissionAssignments)
            .GroupBy(x => x.TenantUserId)
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var row in rows)
        {
            if (byUser.TryGetValue(row.UserId, out var outlets))
            {
                row.OutletIds = outlets.Select(x => x.Id).Distinct().ToList();
                row.Outlets = outlets
                    .GroupBy(x => x.Id)
                    .Select(group => group.First())
                    .Select(outlet => new OutletOptionResponse(
                        outlet.Id,
                        outlet.OutletName,
                        outlet.OutletCode,
                        outlet.Status))
                    .ToList();
            }

            row.OutletCount = string.Equals(
                row.Status,
                TenantUserConstants.StatusActive,
                StringComparison.OrdinalIgnoreCase)
                ? (row.Outlets.Count == 0 ? activeOutletCount : row.Outlets.Count)
                : 0;
        }
    }

    private static List<UserRow> ApplySort(
        List<UserRow> rows,
        string sortBy,
        string sortDirection)
    {
        var descending = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);

        return (sortBy?.Trim().ToLowerInvariant() ?? "name") switch
        {
            "email" => descending
                ? rows.OrderByDescending(x => x.Email).ToList()
                : rows.OrderBy(x => x.Email).ToList(),
            "role" => descending
                ? rows.OrderByDescending(x => x.RoleName).ToList()
                : rows.OrderBy(x => x.RoleName).ToList(),
            "status" => descending
                ? rows.OrderByDescending(x => x.Status).ToList()
                : rows.OrderBy(x => x.Status).ToList(),
            "lastactive" or "lastactiveat" => descending
                ? rows.OrderByDescending(x => x.LastActiveAt).ToList()
                : rows.OrderBy(x => x.LastActiveAt).ToList(),
            _ => descending
                ? rows.OrderByDescending(x => x.FullName).ToList()
                : rows.OrderBy(x => x.FullName).ToList(),
        };
    }

    private static TenantAdminUserListItemResponse MapListItem(UserRow row)
    {
        return new TenantAdminUserListItemResponse(
            row.UserId,
            row.FullName,
            row.Email,
            row.Phone,
            row.RoleId,
            row.RoleName,
            row.Outlets.Count == 0 ? "All Outlets" : string.Join(", ", row.Outlets.Select(x => x.OutletName)),
            FormatStatus(row.Status),
            row.LastActiveAt,
            row.RoleDescription,
            row.Outlets,
            row.OutletCount);
    }

    private static string FormatStatus(string status)
    {
        return status.Trim().ToUpperInvariant() switch
        {
            TenantUserConstants.StatusActive => "Active",
            TenantUserConstants.StatusInactive => "Inactive",
            TenantUserConstants.StatusInvited => "Invited",
            _ => status,
        };
    }

    private async Task<TenantAdminUserAccessValidationResult> ValidateAssignablePermissionsAsync(
        Guid tenantId,
        IReadOnlyCollection<Guid> permissionIds,
        IReadOnlyCollection<string> actorPermissionCodes,
        DateTimeOffset now,
        bool allowSystemPermissions,
        bool enforceEntitlements,
        CancellationToken cancellationToken)
    {
        if (permissionIds.Count == 0)
        {
            return TenantAdminUserAccessValidationResult.Valid;
        }

        var normalizedIds = permissionIds.Distinct().ToList();
        var rows = await (
            from permission in _dbContext.PermissionDefinitions.AsNoTracking()
            join feature in _dbContext.PlatformFeatures.AsNoTracking()
                on permission.FeatureId equals feature.Id
            where normalizedIds.Contains(permission.Id)
            select new AssignablePermissionRow(
                permission.Id,
                permission.PermissionCode,
                permission.IsActive,
                permission.IsSystem,
                feature.Id,
                feature.FeatureCode,
                feature.IsCoreFeature)).ToListAsync(cancellationToken);

        if (rows.Count != normalizedIds.Count)
        {
            return TenantAdminUserAccessValidationResult.Invalid(TenantAdminUserAccessValidationFailure.PermissionNotFound);
        }

        if (rows.Any(row => !row.IsActive))
        {
            return TenantAdminUserAccessValidationResult.Invalid(TenantAdminUserAccessValidationFailure.PermissionInactive);
        }

        if (!allowSystemPermissions && rows.Any(row => row.IsSystem))
        {
            return TenantAdminUserAccessValidationResult.Invalid(TenantAdminUserAccessValidationFailure.PermissionNotAssignable);
        }

        if (rows.Any(row => TenantAdminBootstrapPermissionCatalog.IsPlatformOnlyPermission(row.PermissionCode)))
        {
            return TenantAdminUserAccessValidationResult.Invalid(TenantAdminUserAccessValidationFailure.InvalidScope);
        }

        if (rows.Any(row => !ActorHasPermission(actorPermissionCodes, row.PermissionCode)))
        {
            return TenantAdminUserAccessValidationResult.Invalid(TenantAdminUserAccessValidationFailure.ActorCannotDelegate);
        }

        if (enforceEntitlements)
        {
            var enabledFeatureIds = await GetEnabledFeatureIdsAsync(tenantId, now, cancellationToken);
            if (rows.Any(row => !HasRequiredEntitlement(
                    row.PermissionCode,
                    row.FeatureId,
                    row.FeatureCode,
                    row.IsCoreFeature,
                    enabledFeatureIds)))
            {
                return TenantAdminUserAccessValidationResult.Invalid(TenantAdminUserAccessValidationFailure.TenantEntitlementMissing);
            }
        }

        return TenantAdminUserAccessValidationResult.Valid;
    }

    private async Task<HashSet<Guid>> GetEnabledFeatureIdsAsync(
        Guid tenantId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var rows = await _dbContext.TenantFeatureEntitlements
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId)
            .Select(x => new
            {
                x.PlatformFeatureId,
                x.EntitlementStatus,
                x.IsEnabled,
                x.RevokedAt,
                x.EffectiveFrom,
                x.EffectiveUntil,
            })
            .ToListAsync(cancellationToken);

        return rows
            .Where(x => TenantEntitlementEffectivePredicate.IsEnabled(
                x.EntitlementStatus,
                x.IsEnabled,
                x.RevokedAt,
                x.EffectiveFrom,
                x.EffectiveUntil,
                now))
            .Select(x => x.PlatformFeatureId)
            .ToHashSet();
    }

    private static bool ActorHasPermission(IReadOnlyCollection<string> actorPermissionCodes, string permissionCode) =>
        actorPermissionCodes.Contains(permissionCode, StringComparer.OrdinalIgnoreCase);

    private static bool HasRequiredEntitlement(
        string permissionCode,
        Guid featureId,
        string featureCode,
        bool isCoreFeature,
        HashSet<Guid> enabledFeatureIds)
    {
        if (isCoreFeature ||
            TenantAdminBootstrapPermissionCatalog.BasePermissionCodes.Contains(permissionCode, StringComparer.OrdinalIgnoreCase) ||
            !PlatformTenantFeatureCodes.IsKnownFeatureCode(featureCode))
        {
            return true;
        }

        return enabledFeatureIds.Contains(featureId);
    }

    private sealed record PermissionOptionRow(
        string Name,
        Guid Id,
        string PermissionCode,
        string ActionType,
        string? Description,
        Guid FeatureId,
        string FeatureCode,
        bool IsCoreFeature);

    private sealed record AssignablePermissionRow(
        Guid Id,
        string PermissionCode,
        bool IsActive,
        bool IsSystem,
        Guid FeatureId,
        string FeatureCode,
        bool IsCoreFeature);

    private sealed class UserRow
    {
        public Guid UserId { get; init; }
        public string FullName { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public string? Phone { get; init; }
        public Guid? RoleId { get; init; }
        public string RoleName { get; init; } = string.Empty;
        public string? RoleDescription { get; init; }
        public List<Guid> OutletIds { get; set; } = new();
        public List<OutletOptionResponse> Outlets { get; set; } = new();
        public int OutletCount { get; set; }
        public string Status { get; init; } = string.Empty;
        public DateTimeOffset? LastActiveAt { get; init; }
    }
}
