using System.Text.Json;
using E_POS.Application.Modules.Tenant.AccessControl.Contracts;
using E_POS.Application.Modules.Tenant.AccessControl.Dtos.TenantAdmin;
using E_POS.Domain.Modules.Platform.Subscription.Constants;
using E_POS.Domain.Modules.Shared.Audit.Entities;
using E_POS.Domain.Modules.Tenant.AccessControl.Constants;
using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Constants;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Constants;
using E_POS.Infrastructure.Modules.Platform.Subscription.Entitlements;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace E_POS.Infrastructure.Modules.Tenant.AccessControl.Repositories;

public sealed class TenantAdminRoleRepository : ITenantAdminRoleRepository
{
    private const string TenantWideScope = TenantRoleSetupCatalog.TenantWideScope;
    private const string SelectedOutletsScope = TenantRoleSetupCatalog.SelectedOutletsScope;

    private static readonly string[] AdministrativePermissionCodes =
    [
        TenantAdminUserPermissions.Manage,
        TenantAdminUserPermissions.RolesManage,
        TenantAdminUserPermissions.RolesPermissionsUpdate,
        TenantAdminUserPermissions.RolesAssignmentsUpdate
    ];

    private readonly EPosDbContext _dbContext;

    public TenantAdminRoleRepository(EPosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<TenantAdminRoleListResponse> ListAsync(
        Guid tenantId,
        string? search,
        string? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.TenantRoles.AsNoTracking().Where(role => role.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(role =>
                role.RoleName.ToLower().Contains(term) ||
                role.RoleCode.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            var normalized = status.Trim().ToUpperInvariant();
            if (normalized is "ACTIVE") query = query.Where(role => role.IsActive);
            if (normalized is "INACTIVE" or "DISABLED") query = query.Where(role => !role.IsActive);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var roles = await query
            .OrderBy(role => role.RoleName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(role => new
            {
                role.Id,
                role.RoleCode,
                role.RoleName,
                role.RoleDescription,
                role.IsActive,
                role.IsCustom,
                role.CreatedAt,
                role.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        var roleIds = roles.Select(role => role.Id).ToArray();
        var permissionCounts = await ActivePermissionCountsAsync(tenantId, roleIds, cancellationToken);
        var userCounts = await ActiveAssignmentCountsAsync(tenantId, roleIds, cancellationToken);

        var items = roles.Select(role => new TenantAdminRoleListItemResponse(
            role.Id,
            role.RoleCode,
            role.RoleName,
            role.RoleDescription,
            role.IsActive,
            role.IsCustom != true,
            permissionCounts.GetValueOrDefault(role.Id),
            userCounts.GetValueOrDefault(role.Id),
            role.CreatedAt,
            NormalizeUpdatedAt(role.UpdatedAt, role.CreatedAt))).ToArray();

        return new TenantAdminRoleListResponse(
            items,
            page,
            pageSize,
            totalCount,
            totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize));
    }

    public async Task<TenantAdminRoleDetailResponse?> GetDetailAsync(
        Guid tenantId,
        Guid roleId,
        CancellationToken cancellationToken)
    {
        var role = await _dbContext.TenantRoles
            .AsNoTracking()
            .Where(item => item.TenantId == tenantId && item.Id == roleId)
            .Select(item => new
            {
                item.Id,
                item.RoleCode,
                item.RoleName,
                item.RoleDescription,
                item.IsActive,
                item.IsCustom,
                item.CreatedAt,
                item.UpdatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (role is null) return null;

        var permissionCounts = await ActivePermissionCountsAsync(tenantId, [roleId], cancellationToken);
        var userCounts = await ActiveAssignmentCountsAsync(tenantId, [roleId], cancellationToken);

        return new TenantAdminRoleDetailResponse(
            role.Id,
            role.RoleCode,
            role.RoleName,
            role.RoleDescription,
            role.IsActive,
            role.IsCustom != true,
            permissionCounts.GetValueOrDefault(role.Id),
            userCounts.GetValueOrDefault(role.Id),
            role.CreatedAt,
            NormalizeUpdatedAt(role.UpdatedAt, role.CreatedAt));
    }

    public async Task<IReadOnlyList<TenantRoleSetupOptionResponse>> GetSetupRoleOptionsAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var roles = await _dbContext.TenantRoles
            .AsNoTracking()
            .Where(role =>
                role.TenantId == tenantId &&
                TenantRoleSetupCatalog.SupportedRoleCodes.Contains(role.RoleCode))
            .OrderBy(role => role.RoleName)
            .Select(role => new
            {
                role.Id,
                role.RoleCode,
                role.RoleName,
                role.RoleDescription,
                role.IsActive,
                role.IsCustom,
                role.CreatedAt,
                role.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        var roleIds = roles.Select(role => role.Id).ToArray();
        var permissionCounts = await ActivePermissionCountsAsync(tenantId, roleIds, cancellationToken);
        var userCounts = await ActiveAssignmentCountsAsync(tenantId, roleIds, cancellationToken);

        return roles
            .Select(role => new TenantRoleSetupOptionResponse(
                role.Id,
                role.RoleCode,
                role.RoleName,
                role.RoleDescription,
                role.IsActive,
                role.IsCustom != true,
                permissionCounts.GetValueOrDefault(role.Id),
                userCounts.GetValueOrDefault(role.Id),
                NormalizeUpdatedAt(role.UpdatedAt, role.CreatedAt)))
            .ToArray();
    }

    public Task<TenantRole?> GetEditableAsync(Guid tenantId, Guid roleId, CancellationToken cancellationToken)
    {
        return _dbContext.TenantRoles
            .FirstOrDefaultAsync(role => role.TenantId == tenantId && role.Id == roleId, cancellationToken);
    }

    public Task AddAsync(TenantRole role, CancellationToken cancellationToken)
    {
        _dbContext.TenantRoles.Add(role);
        return Task.CompletedTask;
    }

    public Task<bool> RoleCodeExistsAsync(
        Guid tenantId,
        string roleCode,
        Guid? excludeRoleId,
        CancellationToken cancellationToken)
    {
        return _dbContext.TenantRoles.AnyAsync(role =>
            role.TenantId == tenantId &&
            role.RoleCode == roleCode &&
            (!excludeRoleId.HasValue || role.Id != excludeRoleId.Value),
            cancellationToken);
    }

    public Task<bool> RoleNameExistsAsync(
        Guid tenantId,
        string roleName,
        Guid? excludeRoleId,
        CancellationToken cancellationToken)
    {
        return _dbContext.TenantRoles.AnyAsync(role =>
            role.TenantId == tenantId &&
            role.RoleName == roleName &&
            (!excludeRoleId.HasValue || role.Id != excludeRoleId.Value),
            cancellationToken);
    }

    public async Task<IReadOnlyList<PermissionDefinition>> GetAssignablePermissionsByCodeAsync(
        Guid tenantId,
        IReadOnlyCollection<string> permissionCodes,
        IReadOnlyCollection<string> actorPermissionCodes,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (permissionCodes.Count == 0) return [];

        var normalizedCodes = permissionCodes.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var rows = await (
            from permission in _dbContext.PermissionDefinitions
            join module in _dbContext.PlatformModules.AsNoTracking()
                on permission.ModuleId equals module.Id
            join feature in _dbContext.PlatformFeatures.AsNoTracking()
                on permission.FeatureId equals feature.Id
            where normalizedCodes.Contains(permission.PermissionCode)
            select new
            {
                Permission = permission,
                ModuleStatus = module.Status,
                feature.Id,
                feature.FeatureCode,
                FeatureStatus = feature.Status,
                feature.IsCoreFeature
            }).ToListAsync(cancellationToken);

        if (rows.Count != normalizedCodes.Count) return [];

        var enabledFeatureIds = await GetEnabledFeatureIdsAsync(tenantId, now, cancellationToken);
        return rows
            .Where(row =>
                row.Permission.IsActive &&
                row.ModuleStatus == "ACTIVE" &&
                row.FeatureStatus == "ACTIVE" &&
                !TenantAdminBootstrapPermissionCatalog.IsPlatformOnlyPermission(row.Permission.PermissionCode) &&
                ActorHasPermission(actorPermissionCodes, row.Permission.PermissionCode) &&
                HasRequiredEntitlement(
                    row.Permission.PermissionCode,
                    row.Id,
                    row.FeatureCode,
                    row.IsCoreFeature,
                    enabledFeatureIds))
            .Select(row => row.Permission)
            .ToArray();
    }

    public async Task<TenantPermissionCatalogResponse> GetPermissionCatalogAsync(
        Guid tenantId,
        IReadOnlyCollection<string> actorPermissionCodes,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var rows = await (
            from permission in _dbContext.PermissionDefinitions.AsNoTracking()
            join module in _dbContext.PlatformModules.AsNoTracking()
                on permission.ModuleId equals module.Id
            join feature in _dbContext.PlatformFeatures.AsNoTracking()
                on permission.FeatureId equals feature.Id
            where permission.IsActive &&
                  module.Status == "ACTIVE" &&
                  feature.Status == "ACTIVE" &&
                  permission.Scope == "TENANT" &&
                  module.Scope == "TENANT" &&
                  feature.Scope == "TENANT" &&
                  !permission.PermissionCode.StartsWith("platform.")

            orderby module.SortOrder, module.Name, feature.SortOrder, feature.Name, permission.PermissionCode
            select new CatalogRow(
                module.Id,
                module.ModuleCode,
                module.Name,
                module.Description,
                module.SortOrder,
                module.Status,
                module.IsCoreModule,
                feature.Id,
                feature.FeatureCode,
                feature.Name,
                feature.Description,
                feature.SortOrder,
                feature.Status,
                feature.IsCoreFeature,
                permission.Id,
                permission.PermissionCode,
                permission.ActionType,
                permission.Description,
                permission.IsActive)).ToListAsync(cancellationToken);

        var enabledFeatureIds = await GetEnabledFeatureIdsAsync(tenantId, now, cancellationToken);
        rows = rows
            .Where(row =>
                ActorHasPermission(actorPermissionCodes, row.PermissionCode) &&
                HasRequiredEntitlement(row.PermissionCode, row.FeatureId, row.FeatureCode, row.IsCoreFeature, enabledFeatureIds))
            .ToList();

        var modules = rows
            .GroupBy(row => new
            {
                row.ModuleId,
                row.ModuleCode,
                row.ModuleName,
                row.ModuleDescription,
                row.ModuleSortOrder,
                row.ModuleStatus
            })
            .Select(moduleGroup => new TenantPermissionCatalogModuleResponse(
                moduleGroup.Key.ModuleId,
                moduleGroup.Key.ModuleCode,
                moduleGroup.Key.ModuleName,
                moduleGroup.Key.ModuleDescription,
                "TENANT",
                moduleGroup.Key.ModuleSortOrder,
                IsActiveStatus(moduleGroup.Key.ModuleStatus),
                moduleGroup
                    .GroupBy(row => new
                    {
                        row.FeatureId,
                        row.FeatureCode,
                        row.FeatureName,
                        row.FeatureDescription,
                        row.FeatureSortOrder,
                        row.FeatureStatus
                    })
                    .Select(featureGroup => new TenantPermissionCatalogFeatureResponse(
                        featureGroup.Key.FeatureId,
                        featureGroup.Key.FeatureCode,
                        featureGroup.Key.FeatureName,
                        featureGroup.Key.FeatureDescription,
                        featureGroup.Key.FeatureCode,
                        featureGroup.Key.FeatureSortOrder,
                        IsActiveStatus(featureGroup.Key.FeatureStatus),
                        featureGroup.Select(row => new TenantPermissionCatalogPermissionResponse(
                            row.PermissionDefinitionId,
                            row.PermissionCode,
                            HumanizePermission(row.PermissionCode, row.ActionType),
                            row.PermissionDescription,
                            row.ActionType,
                            "TENANT",
                            0,
                            row.PermissionIsActive,
                            "TENANT",
                            true,
                            null)).ToArray()))
                    .ToArray()))
            .ToArray();

        return new TenantPermissionCatalogResponse(modules);
    }

    public async Task<TenantRolePermissionsResponse?> GetPermissionsAsync(
        Guid tenantId,
        Guid roleId,
        CancellationToken cancellationToken)
    {
        var role = await _dbContext.TenantRoles
            .AsNoTracking()
            .Where(item => item.TenantId == tenantId && item.Id == roleId)
            .Select(item => new { item.Id, item.RoleCode, item.RoleName, item.IsCustom, item.CreatedAt, item.UpdatedAt })
            .FirstOrDefaultAsync(cancellationToken);

        if (role is null) return null;

        var permissions = await (
            from rolePermission in _dbContext.TenantRolePermissions.AsNoTracking()
            join permission in _dbContext.PermissionDefinitions.AsNoTracking()
                on rolePermission.PermissionDefinitionId equals permission.Id
            where rolePermission.TenantId == tenantId &&
                  rolePermission.TenantRoleId == roleId &&
                  rolePermission.RevokedAt == null
            orderby permission.PermissionCode
            select new { permission.Id, permission.PermissionCode }).ToListAsync(cancellationToken);

        return new TenantRolePermissionsResponse(
            role.Id,
            role.RoleCode,
            role.RoleName,
            "TENANT",
            role.IsCustom != true,
            permissions.Select(item => item.PermissionCode).ToArray(),
            permissions.Select(item => item.Id).ToArray(),
            NormalizeUpdatedAt(role.UpdatedAt, role.CreatedAt));
    }

    public async Task ReplacePermissionsAsync(
        Guid tenantId,
        Guid roleId,
        IReadOnlyCollection<Guid> permissionIds,
        Guid actorUserId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var requested = permissionIds.ToHashSet();
        var existing = await _dbContext.TenantRolePermissions
            .Where(item => item.TenantId == tenantId && item.TenantRoleId == roleId)
            .ToListAsync(cancellationToken);

        foreach (var permission in existing)
        {
            if (requested.Contains(permission.PermissionDefinitionId))
            {
                if (permission.RevokedAt.HasValue) permission.Reactivate(actorUserId, now);
            }
            else if (!permission.RevokedAt.HasValue)
            {
                permission.Revoke(actorUserId, now);
            }
        }

        var existingIds = existing.Select(item => item.PermissionDefinitionId).ToHashSet();
        foreach (var permissionId in requested.Where(permissionId => !existingIds.Contains(permissionId)))
        {
            _dbContext.TenantRolePermissions.Add(TenantRolePermission.Create(
                Guid.NewGuid(),
                tenantId,
                roleId,
                permissionId,
                actorUserId,
                now));
        }
    }

    public async Task<TenantRoleAssignmentsResponse?> GetAssignmentsAsync(
        Guid tenantId,
        Guid roleId,
        CancellationToken cancellationToken)
    {
        var role = await _dbContext.TenantRoles
            .AsNoTracking()
            .Where(item => item.TenantId == tenantId && item.Id == roleId)
            .Select(item => new { item.Id, item.RoleCode, item.RoleName, item.IsCustom, item.CreatedAt, item.UpdatedAt })
            .FirstOrDefaultAsync(cancellationToken);

        if (role is null) return null;

        var tenantAssignments = await (
            from assignment in _dbContext.TenantUserRoles.AsNoTracking()
            join user in _dbContext.TenantUsers.AsNoTracking()
                on assignment.TenantUserId equals user.Id
            where assignment.TenantId == tenantId &&
                  assignment.TenantRoleId == roleId &&
                  assignment.RevokedAt == null
            select new TenantAdminRoleAssignmentResponse(
                user.Id,
                user.FullName,
                user.Email,
                TenantWideScope,
                Array.Empty<Guid>())).ToListAsync(cancellationToken);

        var outletRows = await (
            from assignment in _dbContext.OutletUserRoles.AsNoTracking()
            join user in _dbContext.TenantUsers.AsNoTracking()
                on assignment.TenantUserId equals user.Id
            where assignment.TenantId == tenantId &&
                  assignment.TenantRoleId == roleId &&
                  assignment.RevokedAt == null
            select new
            {
                user.Id,
                user.FullName,
                user.Email,
                assignment.OutletId
            }).ToListAsync(cancellationToken);

        var outletAssignments = outletRows
            .GroupBy(row => new { row.Id, row.FullName, row.Email })
            .Select(group => new TenantAdminRoleAssignmentResponse(
                group.Key.Id,
                group.Key.FullName,
                group.Key.Email,
                SelectedOutletsScope,
                group.Select(row => row.OutletId).Distinct().OrderBy(id => id).ToArray()))
            .ToArray();

        return new TenantRoleAssignmentsResponse(
            role.Id,
            role.RoleCode,
            role.RoleName,
            role.IsCustom != true,
            tenantAssignments.Concat(outletAssignments).OrderBy(item => item.FullName).ToArray(),
            NormalizeUpdatedAt(role.UpdatedAt, role.CreatedAt));
    }

    public async Task<RoleAssignmentValidationResult> ValidateAssignmentsAsync(
        Guid tenantId,
        IReadOnlyCollection<TenantAdminRoleAssignmentRequest> assignments,
        CancellationToken cancellationToken)
    {
        if (assignments.Count == 0) return RoleAssignmentValidationResult.Valid;

        if (assignments.Any(item => item.AccessScope is not TenantWideScope and not SelectedOutletsScope))
        {
            return RoleAssignmentValidationResult.Invalid(RoleAssignmentValidationFailure.InvalidAccessScope);
        }

        if (assignments.Any(item => item.AccessScope == SelectedOutletsScope && (item.OutletIds is null || item.OutletIds.Count == 0)))
        {
            return RoleAssignmentValidationResult.Invalid(RoleAssignmentValidationFailure.MissingOutletSelection);
        }

        var userIds = assignments.Select(item => item.UserId).Distinct().ToArray();
        var userCount = await _dbContext.TenantUsers
            .AsNoTracking()
            .CountAsync(user => user.TenantId == tenantId && userIds.Contains(user.Id), cancellationToken);
        if (userCount != userIds.Length)
        {
            return RoleAssignmentValidationResult.Invalid(RoleAssignmentValidationFailure.UserNotFound);
        }

        var outletIds = assignments
            .SelectMany(item => item.OutletIds ?? Array.Empty<Guid>())
            .Distinct()
            .ToArray();
        if (outletIds.Length == 0) return RoleAssignmentValidationResult.Valid;

        var outletRows = await _dbContext.Outlets
            .AsNoTracking()
            .Where(outlet => outletIds.Contains(outlet.Id))
            .Select(outlet => new { outlet.Id, outlet.TenantId, outlet.Status })
            .ToListAsync(cancellationToken);

        if (outletRows.Count != outletIds.Length || outletRows.Any(outlet => outlet.TenantId != tenantId))
        {
            return RoleAssignmentValidationResult.Invalid(RoleAssignmentValidationFailure.OutletNotFound);
        }

        if (outletRows.Any(outlet =>
                (outlet.Status.ToUpper() == OutletConstants.InactiveStatus ||
                 outlet.Status.ToUpper() == OutletConstants.DeletedStatus)))
        {
            return RoleAssignmentValidationResult.Invalid(RoleAssignmentValidationFailure.OutletInactive);
        }

        return RoleAssignmentValidationResult.Valid;
    }

    public async Task ReplaceAssignmentsAsync(
        Guid tenantId,
        Guid roleId,
        IReadOnlyCollection<TenantAdminRoleAssignmentRequest> assignments,
        Guid actorUserId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var requestedTenantUsers = assignments
            .Where(item => item.AccessScope == TenantWideScope)
            .Select(item => item.UserId)
            .ToHashSet();
        var requestedOutletRoles = assignments
            .Where(item => item.AccessScope == SelectedOutletsScope)
            .SelectMany(item => (item.OutletIds ?? Array.Empty<Guid>()).Select(outletId => new RoleOutletAssignmentKey(item.UserId, outletId)))
            .ToHashSet();

        var existingTenantRoles = await _dbContext.TenantUserRoles
            .Where(item => item.TenantId == tenantId && item.TenantRoleId == roleId)
            .ToListAsync(cancellationToken);
        foreach (var assignment in existingTenantRoles)
        {
            if (requestedTenantUsers.Contains(assignment.TenantUserId))
            {
                if (assignment.RevokedAt.HasValue) assignment.Reactivate(actorUserId, now);
            }
            else if (!assignment.RevokedAt.HasValue)
            {
                assignment.Revoke(now);
            }
        }

        var existingTenantUserIds = existingTenantRoles.Select(item => item.TenantUserId).ToHashSet();
        foreach (var userId in requestedTenantUsers.Where(userId => !existingTenantUserIds.Contains(userId)))
        {
            _dbContext.TenantUserRoles.Add(TenantUserRole.Create(Guid.NewGuid(), tenantId, userId, roleId, actorUserId, now));
        }

        var existingOutletRoles = await _dbContext.OutletUserRoles
            .Where(item => item.TenantId == tenantId && item.TenantRoleId == roleId)
            .ToListAsync(cancellationToken);
        foreach (var assignment in existingOutletRoles)
        {
            var key = new RoleOutletAssignmentKey(assignment.TenantUserId, assignment.OutletId);
            if (requestedOutletRoles.Contains(key))
            {
                if (assignment.RevokedAt.HasValue) assignment.Reactivate(actorUserId, now);
            }
            else if (!assignment.RevokedAt.HasValue)
            {
                assignment.Revoke(actorUserId, now);
            }
        }

        var existingOutletKeys = existingOutletRoles
            .Select(item => new RoleOutletAssignmentKey(item.TenantUserId, item.OutletId))
            .ToHashSet();
        foreach (var assignment in requestedOutletRoles.Where(item => !existingOutletKeys.Contains(item)))
        {
            _dbContext.OutletUserRoles.Add(OutletUserRole.Create(
                Guid.NewGuid(),
                tenantId,
                assignment.OutletId,
                assignment.UserId,
                roleId,
                actorUserId,
                now));
        }
    }

    public async Task<bool> WouldRemoveLastAdminAsync(
        Guid tenantId,
        Guid roleId,
        IReadOnlyCollection<Guid>? replacementPermissionIds,
        bool? replacementIsActive,
        CancellationToken cancellationToken)
    {
        var criticalPermissionIds = await _dbContext.PermissionDefinitions
            .AsNoTracking()
            .Where(permission => permission.IsActive && AdministrativePermissionCodes.Contains(permission.PermissionCode))
            .Select(permission => permission.Id)
            .ToArrayAsync(cancellationToken);

        if (criticalPermissionIds.Length == 0) return false;

        var currentRoleHasCritical = await _dbContext.TenantRolePermissions
            .AsNoTracking()
            .AnyAsync(item =>
                item.TenantId == tenantId &&
                item.TenantRoleId == roleId &&
                item.RevokedAt == null &&
                criticalPermissionIds.Contains(item.PermissionDefinitionId),
                cancellationToken);

        if (!currentRoleHasCritical) return false;
        if (replacementIsActive == true) return false;
        if (replacementPermissionIds is not null && replacementPermissionIds.Any(criticalPermissionIds.Contains)) return false;

        var otherTenantRolePath = await (
            from userRole in _dbContext.TenantUserRoles.AsNoTracking()
            join user in _dbContext.TenantUsers.AsNoTracking()
                on userRole.TenantUserId equals user.Id
            join role in _dbContext.TenantRoles.AsNoTracking()
                on userRole.TenantRoleId equals role.Id
            join rolePermission in _dbContext.TenantRolePermissions.AsNoTracking()
                on role.Id equals rolePermission.TenantRoleId
            where userRole.TenantId == tenantId &&
                  userRole.TenantRoleId != roleId &&
                  userRole.RevokedAt == null &&
                  role.IsActive &&
                  user.AccountStatus == TenantUserConstants.StatusActive &&
                  rolePermission.TenantId == tenantId &&
                  rolePermission.RevokedAt == null &&
                  criticalPermissionIds.Contains(rolePermission.PermissionDefinitionId)
            select user.Id).AnyAsync(cancellationToken);
        if (otherTenantRolePath) return false;

        var otherOutletRolePath = await (
            from outletRole in _dbContext.OutletUserRoles.AsNoTracking()
            join user in _dbContext.TenantUsers.AsNoTracking()
                on outletRole.TenantUserId equals user.Id
            join role in _dbContext.TenantRoles.AsNoTracking()
                on outletRole.TenantRoleId equals role.Id
            join rolePermission in _dbContext.TenantRolePermissions.AsNoTracking()
                on role.Id equals rolePermission.TenantRoleId
            where outletRole.TenantId == tenantId &&
                  outletRole.TenantRoleId != roleId &&
                  outletRole.RevokedAt == null &&
                  role.IsActive &&
                  user.AccountStatus == TenantUserConstants.StatusActive &&
                  rolePermission.TenantId == tenantId &&
                  rolePermission.RevokedAt == null &&
                  criticalPermissionIds.Contains(rolePermission.PermissionDefinitionId)
            select user.Id).AnyAsync(cancellationToken);
        if (otherOutletRolePath) return false;

        return !await (
            from userPermission in _dbContext.TenantUserPermissions.AsNoTracking()
            join user in _dbContext.TenantUsers.AsNoTracking()
                on userPermission.TenantUserId equals user.Id
            where userPermission.TenantId == tenantId &&
                  userPermission.RevokedAt == null &&
                  user.AccountStatus == TenantUserConstants.StatusActive &&
                  criticalPermissionIds.Contains(userPermission.PermissionDefinitionId)
            select user.Id).AnyAsync(cancellationToken);
    }

    public async Task<bool> WouldReplaceAssignmentsRemoveLastAdminAsync(
        Guid tenantId,
        Guid roleId,
        IReadOnlyCollection<TenantAdminRoleAssignmentRequest> replacementAssignments,
        CancellationToken cancellationToken)
    {
        var criticalPermissionIds = await _dbContext.PermissionDefinitions
            .AsNoTracking()
            .Where(permission => permission.IsActive && AdministrativePermissionCodes.Contains(permission.PermissionCode))
            .Select(permission => permission.Id)
            .ToArrayAsync(cancellationToken);

        if (criticalPermissionIds.Length == 0) return false;

        var roleHasCritical = await (
            from role in _dbContext.TenantRoles.AsNoTracking()
            join rolePermission in _dbContext.TenantRolePermissions.AsNoTracking()
                on role.Id equals rolePermission.TenantRoleId
            where role.TenantId == tenantId &&
                  role.Id == roleId &&
                  role.IsActive &&
                  rolePermission.TenantId == tenantId &&
                  rolePermission.RevokedAt == null &&
                  criticalPermissionIds.Contains(rolePermission.PermissionDefinitionId)
            select role.Id).AnyAsync(cancellationToken);

        if (!roleHasCritical) return false;

        var replacementUserIds = replacementAssignments
            .Select(assignment => assignment.UserId)
            .Where(userId => userId != Guid.Empty)
            .Distinct()
            .ToArray();

        if (replacementUserIds.Length > 0)
        {
            var replacementActiveUserExists = await _dbContext.TenantUsers
                .AsNoTracking()
                .AnyAsync(user =>
                    user.TenantId == tenantId &&
                    replacementUserIds.Contains(user.Id) &&
                    user.AccountStatus == TenantUserConstants.StatusActive,
                    cancellationToken);

            if (replacementActiveUserExists) return false;
        }

        var otherTenantRolePath = await (
            from userRole in _dbContext.TenantUserRoles.AsNoTracking()
            join user in _dbContext.TenantUsers.AsNoTracking()
                on userRole.TenantUserId equals user.Id
            join role in _dbContext.TenantRoles.AsNoTracking()
                on userRole.TenantRoleId equals role.Id
            join rolePermission in _dbContext.TenantRolePermissions.AsNoTracking()
                on role.Id equals rolePermission.TenantRoleId
            where userRole.TenantId == tenantId &&
                  userRole.TenantRoleId != roleId &&
                  userRole.RevokedAt == null &&
                  role.TenantId == tenantId &&
                  role.IsActive &&
                  user.TenantId == tenantId &&
                  user.AccountStatus == TenantUserConstants.StatusActive &&
                  rolePermission.TenantId == tenantId &&
                  rolePermission.RevokedAt == null &&
                  criticalPermissionIds.Contains(rolePermission.PermissionDefinitionId)
            select user.Id).AnyAsync(cancellationToken);
        if (otherTenantRolePath) return false;

        var otherOutletRolePath = await (
            from outletRole in _dbContext.OutletUserRoles.AsNoTracking()
            join user in _dbContext.TenantUsers.AsNoTracking()
                on outletRole.TenantUserId equals user.Id
            join role in _dbContext.TenantRoles.AsNoTracking()
                on outletRole.TenantRoleId equals role.Id
            join rolePermission in _dbContext.TenantRolePermissions.AsNoTracking()
                on role.Id equals rolePermission.TenantRoleId
            where outletRole.TenantId == tenantId &&
                  outletRole.TenantRoleId != roleId &&
                  outletRole.RevokedAt == null &&
                  role.TenantId == tenantId &&
                  role.IsActive &&
                  user.TenantId == tenantId &&
                  user.AccountStatus == TenantUserConstants.StatusActive &&
                  rolePermission.TenantId == tenantId &&
                  rolePermission.RevokedAt == null &&
                  criticalPermissionIds.Contains(rolePermission.PermissionDefinitionId)
            select user.Id).AnyAsync(cancellationToken);
        if (otherOutletRolePath) return false;

        return !await (
            from userPermission in _dbContext.TenantUserPermissions.AsNoTracking()
            join user in _dbContext.TenantUsers.AsNoTracking()
                on userPermission.TenantUserId equals user.Id
            where userPermission.TenantId == tenantId &&
                  userPermission.RevokedAt == null &&
                  user.TenantId == tenantId &&
                  user.AccountStatus == TenantUserConstants.StatusActive &&
                  criticalPermissionIds.Contains(userPermission.PermissionDefinitionId)
            select user.Id).AnyAsync(cancellationToken);
    }

    public Task AddAuditAsync(
        Guid tenantId,
        Guid actorUserId,
        Guid roleId,
        string action,
        object? payload,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        _dbContext.AuditLogs.Add(new AuditLog
        {
            TenantId = tenantId,
            ActorUserId = actorUserId,
            ActorType = "TENANT_USER",
            EntityType = "TENANT_ROLE",
            EntityId = roleId,
            Action = action,
            NewValues = payload is null ? null : JsonSerializer.Serialize(payload),
            CreatedAt = now
        });
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<Dictionary<Guid, int>> ActivePermissionCountsAsync(
        Guid tenantId,
        IReadOnlyCollection<Guid> roleIds,
        CancellationToken cancellationToken)
    {
        if (roleIds.Count == 0) return [];
        return await _dbContext.TenantRolePermissions
            .AsNoTracking()
            .Where(item => item.TenantId == tenantId && roleIds.Contains(item.TenantRoleId) && item.RevokedAt == null)
            .GroupBy(item => item.TenantRoleId)
            .Select(group => new { RoleId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.RoleId, item => item.Count, cancellationToken);
    }

    private async Task<Dictionary<Guid, int>> ActiveAssignmentCountsAsync(
        Guid tenantId,
        IReadOnlyCollection<Guid> roleIds,
        CancellationToken cancellationToken)
    {
        if (roleIds.Count == 0) return [];

        var tenantCounts = await _dbContext.TenantUserRoles
            .AsNoTracking()
            .Where(item => item.TenantId == tenantId && roleIds.Contains(item.TenantRoleId) && item.RevokedAt == null)
            .GroupBy(item => item.TenantRoleId)
            .Select(group => new { RoleId = group.Key, Count = group.Select(item => item.TenantUserId).Distinct().Count() })
            .ToListAsync(cancellationToken);
        var outletCounts = await _dbContext.OutletUserRoles
            .AsNoTracking()
            .Where(item => item.TenantId == tenantId && roleIds.Contains(item.TenantRoleId) && item.RevokedAt == null)
            .GroupBy(item => item.TenantRoleId)
            .Select(group => new { RoleId = group.Key, Count = group.Select(item => item.TenantUserId).Distinct().Count() })
            .ToListAsync(cancellationToken);

        return tenantCounts
            .Concat(outletCounts)
            .GroupBy(item => item.RoleId)
            .ToDictionary(group => group.Key, group => group.Sum(item => item.Count));
    }

    private async Task<HashSet<Guid>> GetEnabledFeatureIdsAsync(
        Guid tenantId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var rows = await _dbContext.TenantFeatureEntitlements
            .AsNoTracking()
            .Where(item => item.TenantId == tenantId)
            .Select(item => new
            {
                item.PlatformFeatureId,
                item.EntitlementStatus,
                item.IsEnabled,
                item.RevokedAt,
                item.EffectiveFrom,
                item.EffectiveUntil
            })
            .ToListAsync(cancellationToken);

        return rows
            .Where(item => TenantEntitlementEffectivePredicate.IsEnabled(
                item.EntitlementStatus,
                item.IsEnabled,
                item.RevokedAt,
                item.EffectiveFrom,
                item.EffectiveUntil,
                now))
            .Select(item => item.PlatformFeatureId)
            .ToHashSet();
    }

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

    private static bool ActorHasPermission(IReadOnlyCollection<string> actorPermissionCodes, string permissionCode) =>
        actorPermissionCodes.Contains(permissionCode, StringComparer.OrdinalIgnoreCase);

    private static bool IsActiveStatus(string status) =>
        string.Equals(status, "ACTIVE", StringComparison.OrdinalIgnoreCase);

    private static DateTimeOffset NormalizeUpdatedAt(DateTimeOffset? updatedAt, DateTimeOffset? createdAt) =>
        updatedAt ?? createdAt ?? DateTimeOffset.UnixEpoch;

    private static string HumanizePermission(string permissionCode, string actionType)
    {
        if (!string.IsNullOrWhiteSpace(actionType)) return actionType.Trim();
        var tail = permissionCode.Split('.', StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
        return string.IsNullOrWhiteSpace(tail) ? permissionCode : tail;
    }

    private sealed record RoleOutletAssignmentKey(Guid UserId, Guid OutletId);

    private sealed record CatalogRow(
        Guid ModuleId,
        string ModuleCode,
        string ModuleName,
        string? ModuleDescription,
        int ModuleSortOrder,
        string ModuleStatus,
        bool IsCoreModule,
        Guid FeatureId,
        string FeatureCode,
        string FeatureName,
        string? FeatureDescription,
        int FeatureSortOrder,
        string FeatureStatus,
        bool IsCoreFeature,
        Guid PermissionDefinitionId,
        string PermissionCode,
        string ActionType,
        string? PermissionDescription,
        bool PermissionIsActive);
}
