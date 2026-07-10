using E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;
using E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;
using E_POS.Application.Modules.Platform.PlatformAdmin.Mappers;
using E_POS.Domain.Modules.Platform.PlatformAdmin.Constants;
using E_POS.Domain.Modules.Platform.PlatformAdmin.Entities;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace E_POS.Infrastructure.Modules.Platform.PlatformAdmin.Repositories;

public sealed class PlatformUserRepository : IPlatformUserRepository
{
    private readonly EPosDbContext _dbContext;

    public PlatformUserRepository(EPosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PlatformUserListResponse> GetUsersAsync(CancellationToken cancellationToken)
    {
        var users = await _dbContext.PlatformUsers
            .AsNoTracking()
            .OrderBy(user => user.Email)
            .Select(user => new
            {
                user.Id,
                user.Email,
                user.Status,
                user.PasswordHash,
                user.CreatedAt,
                user.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        if (users.Count == 0)
        {
            return new PlatformUserListResponse([]);
        }

        var userIds = users.Select(user => user.Id).ToList();
        var roleAssignments = await LoadRoleAssignmentsAsync(userIds, cancellationToken);
        var permissionCounts = await LoadPermissionCountsAsync(userIds, cancellationToken);
        var lastLogins = await LoadLastLoginTimesAsync(userIds, cancellationToken);

        var items = users
            .Select(user =>
            {
                roleAssignments.TryGetValue(user.Id, out var roles);
                roles ??= [];

                return PlatformUserMapper.ToListItem(
                    user.Id,
                    user.Email,
                    user.Status,
                    user.PasswordHash,
                    roles.Select(role => role.RoleCode).ToList(),
                    roles.Select(role => role.Name).ToList(),
                    permissionCounts.GetValueOrDefault(user.Id),
                    lastLogins.GetValueOrDefault(user.Id),
                    user.CreatedAt,
                    user.UpdatedAt ?? user.CreatedAt);
            })
            .ToList();

        return new PlatformUserListResponse(items);
    }

    public async Task<PlatformUserDetailResponse?> GetUserByIdAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var user = await _dbContext.PlatformUsers
            .AsNoTracking()
            .Where(item => item.Id == userId)
            .Select(item => new
            {
                item.Id,
                item.Email,
                item.Status,
                item.PasswordHash,
                item.CreatedAt,
                item.UpdatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (user is null)
        {
            return null;
        }

        var roleAssignments = await LoadRoleAssignmentsAsync([userId], cancellationToken);
        roleAssignments.TryGetValue(userId, out var roles);
        roles ??= [];

        var permissionCounts = await LoadPermissionCountsAsync([userId], cancellationToken);
        var lastLogins = await LoadLastLoginTimesAsync([userId], cancellationToken);

        return PlatformUserMapper.ToDetail(
            user.Id,
            user.Email,
            user.Status,
            user.PasswordHash,
            roles.Select(role => role.RoleCode).ToList(),
            roles.Select(role => role.Name).ToList(),
            permissionCounts.GetValueOrDefault(userId),
            lastLogins.GetValueOrDefault(userId),
            user.CreatedAt,
            user.UpdatedAt ?? user.CreatedAt);
    }

    public Task<PlatformUser?> GetUserEntityByIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        return _dbContext.PlatformUsers
            .FirstOrDefaultAsync(user => user.Id == userId, cancellationToken);
    }

    public Task<bool> EmailExistsAsync(
        string normalizedEmail,
        Guid? excludingUserId,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.PlatformUsers
            .AsNoTracking()
            .Where(user => user.NormalizedEmail == normalizedEmail);

        if (excludingUserId is not null)
        {
            query = query.Where(user => user.Id != excludingUserId.Value);
        }

        return query.AnyAsync(cancellationToken);
    }

    public async Task AddUserWithRolesAsync(
        PlatformUser user,
        IReadOnlyList<Guid> roleIds,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        _dbContext.PlatformUsers.Add(user);

        foreach (var roleId in roleIds.Distinct())
        {
            _dbContext.PlatformUserRoles.Add(PlatformUserRole.Create(
                Guid.NewGuid(),
                user.Id,
                roleId,
                "Platform user role assignment.",
                now));
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task UpdateUserAsync(PlatformUser user, CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ReplaceUserRolesAsync(
        Guid userId,
        IReadOnlyList<Guid> roleIds,
        DateTimeOffset now,
        Guid? actorPlatformUserId,
        CancellationToken cancellationToken)
    {
        var targetRoleIds = roleIds
            .Where(roleId => roleId != Guid.Empty)
            .Distinct()
            .ToHashSet();

        var activeAssignments = await _dbContext.PlatformUserRoles
            .Where(userRole =>
                userRole.PlatformUserId == userId &&
                userRole.RevokedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var assignment in activeAssignments.Where(item => !targetRoleIds.Contains(item.PlatformRoleId)))
        {
            assignment.Revoke(actorPlatformUserId, "Platform user role reassignment.", now);
        }

        var activeRoleIds = activeAssignments
            .Select(item => item.PlatformRoleId)
            .ToHashSet();

        foreach (var roleId in targetRoleIds.Where(item => !activeRoleIds.Contains(item)))
        {
            _dbContext.PlatformUserRoles.Add(PlatformUserRole.Create(
                Guid.NewGuid(),
                userId,
                roleId,
                "Platform user role assignment.",
                now,
                actorPlatformUserId));
        }

        var user = await _dbContext.PlatformUsers
            .FirstAsync(item => item.Id == userId, cancellationToken);

        user.TouchUpdatedAt(now);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ResolvedPlatformRole>> ResolveActiveRolesAsync(
        IReadOnlyList<Guid>? roleIds,
        IReadOnlyList<string>? roleCodes,
        CancellationToken cancellationToken)
    {
        var requestedRoleIds = roleIds?.Where(id => id != Guid.Empty).Distinct().ToList() ?? [];
        var requestedRoleCodes = roleCodes?
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Select(code => code.Trim().ToLowerInvariant())
            .Distinct(StringComparer.Ordinal)
            .ToList() ?? [];

        if (requestedRoleIds.Count == 0 && requestedRoleCodes.Count == 0)
        {
            return [];
        }

        var query = _dbContext.PlatformRoles
            .AsNoTracking()
            .Where(role => role.Status == PlatformAuthConstants.ActiveStatus);

        if (requestedRoleIds.Count > 0 && requestedRoleCodes.Count > 0)
        {
            query = query.Where(role =>
                requestedRoleIds.Contains(role.Id) ||
                requestedRoleCodes.Contains(role.RoleCode));
        }
        else if (requestedRoleIds.Count > 0)
        {
            query = query.Where(role => requestedRoleIds.Contains(role.Id));
        }
        else
        {
            query = query.Where(role => requestedRoleCodes.Contains(role.RoleCode));
        }

        var roles = await query
            .Select(role => new ResolvedPlatformRole(role.Id, role.RoleCode, role.Name))
            .ToListAsync(cancellationToken);

        return roles;
    }

    public Task<bool> UserHasActiveRoleCodeAsync(
        Guid userId,
        string roleCode,
        CancellationToken cancellationToken)
    {
        return _dbContext.PlatformUserRoles
            .AsNoTracking()
            .AnyAsync(
                userRole =>
                    userRole.PlatformUserId == userId &&
                    userRole.RevokedAt == null &&
                    _dbContext.PlatformRoles.Any(role =>
                        role.Id == userRole.PlatformRoleId &&
                        role.RoleCode == roleCode &&
                        role.Status == PlatformAuthConstants.ActiveStatus),
                cancellationToken);
    }

    public Task<int> CountActiveSuperAdministratorsAsync(
        Guid? excludingUserId,
        CancellationToken cancellationToken)
    {
        var query =
            from user in _dbContext.PlatformUsers.AsNoTracking()
            join userRole in _dbContext.PlatformUserRoles.AsNoTracking()
                on user.Id equals userRole.PlatformUserId
            join role in _dbContext.PlatformRoles.AsNoTracking()
                on userRole.PlatformRoleId equals role.Id
            where user.Status == PlatformAuthConstants.ActiveStatus &&
                  userRole.RevokedAt == null &&
                  role.RoleCode == PlatformRoleCodes.SuperAdministrator &&
                  role.Status == PlatformAuthConstants.ActiveStatus
            select user.Id;

        if (excludingUserId is not null)
        {
            query = query.Where(userId => userId != excludingUserId.Value);
        }

        return query.Distinct().CountAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<string>> GetUserActiveRoleCodesAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        return await (
            from userRole in _dbContext.PlatformUserRoles.AsNoTracking()
            join role in _dbContext.PlatformRoles.AsNoTracking()
                on userRole.PlatformRoleId equals role.Id
            where userRole.PlatformUserId == userId &&
                  userRole.RevokedAt == null &&
                  role.Status == PlatformAuthConstants.ActiveStatus
            orderby role.RoleCode
            select role.RoleCode)
            .ToListAsync(cancellationToken);
    }

    private async Task<Dictionary<Guid, List<(string RoleCode, string Name)>>> LoadRoleAssignmentsAsync(
        IReadOnlyList<Guid> userIds,
        CancellationToken cancellationToken)
    {
        var assignments = await (
            from userRole in _dbContext.PlatformUserRoles.AsNoTracking()
            join role in _dbContext.PlatformRoles.AsNoTracking()
                on userRole.PlatformRoleId equals role.Id
            where userRole.PlatformUserId != null &&
                  userRole.RevokedAt == null &&
                  userIds.Contains(userRole.PlatformUserId.Value)
            orderby role.RoleCode
            select new
            {
                UserId = userRole.PlatformUserId!.Value,
                role.RoleCode,
                role.Name
            })
            .ToListAsync(cancellationToken);

        return assignments
            .GroupBy(item => item.UserId)
            .ToDictionary(
                group => group.Key,
                group => group
                    .Select(item => (item.RoleCode, item.Name))
                    .ToList());
    }

    private async Task<Dictionary<Guid, int>> LoadPermissionCountsAsync(
        IReadOnlyList<Guid> userIds,
        CancellationToken cancellationToken)
    {
        var directPermissions = await (
            from userPermission in _dbContext.PlatformUserPermissions.AsNoTracking()
            join permission in _dbContext.PlatformPermissions.AsNoTracking()
                on userPermission.PlatformPermissionId equals permission.Id
            where userPermission.PlatformUserId != null &&
                  userPermission.RevokedAt == null &&
                  userIds.Contains(userPermission.PlatformUserId.Value) &&
                  permission.Status == PlatformAuthConstants.ActiveStatus
            select new
            {
                UserId = userPermission.PlatformUserId!.Value,
                permission.PermissionCode
            })
            .ToListAsync(cancellationToken);

        var rolePermissions = await (
            from userRole in _dbContext.PlatformUserRoles.AsNoTracking()
            join role in _dbContext.PlatformRoles.AsNoTracking()
                on userRole.PlatformRoleId equals role.Id
            join rolePermission in _dbContext.PlatformRolePermissions.AsNoTracking()
                on role.Id equals rolePermission.PlatformRoleId
            join permission in _dbContext.PlatformPermissions.AsNoTracking()
                on rolePermission.PlatformPermissionId equals permission.Id
            where userRole.PlatformUserId != null &&
                  userRole.RevokedAt == null &&
                  userIds.Contains(userRole.PlatformUserId.Value) &&
                  role.Status == PlatformAuthConstants.ActiveStatus &&
                  rolePermission.RevokedAt == null &&
                  permission.Status == PlatformAuthConstants.ActiveStatus
            select new
            {
                UserId = userRole.PlatformUserId!.Value,
                permission.PermissionCode
            })
            .ToListAsync(cancellationToken);

        return directPermissions
            .Concat(rolePermissions)
            .GroupBy(item => item.UserId)
            .ToDictionary(
                group => group.Key,
                group => group.Select(item => item.PermissionCode).Distinct(StringComparer.Ordinal).Count());
    }

    private async Task<Dictionary<Guid, DateTimeOffset>> LoadLastLoginTimesAsync(
        IReadOnlyList<Guid> userIds,
        CancellationToken cancellationToken)
    {
        var audits = await _dbContext.PlatformLoginAudits
            .AsNoTracking()
            .Where(audit =>
                audit.PlatformUserId != null &&
                userIds.Contains(audit.PlatformUserId.Value) &&
                (audit.LoginStatus ?? audit.LoginResult) == PlatformAuthConstants.SuccessLoginResult)
            .GroupBy(audit => audit.PlatformUserId!.Value)
            .Select(group => new
            {
                UserId = group.Key,
                LastLoginAt = group.Max(item => item.AttemptedAt ?? item.CreatedAt)
            })
            .ToListAsync(cancellationToken);

        return audits.ToDictionary(item => item.UserId, item => item.LastLoginAt);
    }
}



