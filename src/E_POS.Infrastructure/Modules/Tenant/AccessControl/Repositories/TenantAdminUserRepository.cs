using E_POS.Application.Modules.Tenant.AccessControl.Contracts;
using E_POS.Application.Modules.Tenant.AccessControl.Dtos.TenantAdmin;
using E_POS.Domain.Modules.Tenant.AccessControl.Constants;
using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Constants;
using E_POS.Domain.Modules.Tenant.TenantAuth.Entities;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace E_POS.Infrastructure.Modules.Tenant.AccessControl.Repositories;

public sealed class TenantAdminUserRepository : ITenantAdminUserRepository
{
    private const string SuccessLoginStatus = "SUCCESS";
    private const string OpenSessionStatus = "OPEN";

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
                    x.Email.ToUpperInvariant().Contains(term))
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
            .Select(x => new RoleOptionResponse(x.Id, x.RoleName, x.RoleCode))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<OutletOptionResponse>> GetOutletOptionsAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.Outlets
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Status != OutletConstants.DeletedStatus)
            .OrderBy(x => x.OutletName)
            .Select(x => new OutletOptionResponse(x.Id, x.OutletName, x.OutletCode, x.Status))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PermissionGroupResponse>> GetPermissionGroupsAsync(
        CancellationToken cancellationToken)
    {
        var rows = await (
            from permission in _dbContext.PermissionDefinitions.AsNoTracking()
            join feature in _dbContext.PlatformFeatures.AsNoTracking()
                on permission.FeatureId equals feature.Id
            where permission.IsActive
            orderby feature.SortOrder, feature.Name, permission.PermissionCode
            select new
            {
                feature.Name,
                permission.Id,
                permission.PermissionCode,
                permission.ActionType,
                permission.Description,
            }).ToListAsync(cancellationToken);

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

    public async Task<Guid> CreateAsync(
        TenantUser user,
        Guid roleId,
        IReadOnlyCollection<Guid> outletIds,
        IReadOnlyCollection<Guid> overriddenPermissionIds,
        UserInvite? invite,
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

        await _dbContext.SaveChangesAsync(cancellationToken);
        return user.Id;
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
            null);
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
        foreach (var role in existingTenantRoles)
        {
            role.Revoke(now);
        }

        var existingOutletRoles = await _dbContext.OutletUserRoles
            .Where(x => x.TenantId == tenantId && x.TenantUserId == userId && x.RevokedAt == null)
            .ToListAsync(cancellationToken);
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

    private async Task<(Guid RoleId, string RoleName)?> GetActiveRoleAssignmentAsync(
        Guid tenantId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var tenantRole = await (
            from userRole in _dbContext.TenantUserRoles.AsNoTracking()
            join role in _dbContext.TenantRoles.AsNoTracking() on userRole.TenantRoleId equals role.Id
            where userRole.TenantId == tenantId && userRole.TenantUserId == userId && userRole.RevokedAt == null
            orderby userRole.AssignedAt descending
            select new { role.Id, role.RoleName }
        ).FirstOrDefaultAsync(cancellationToken);

        if (tenantRole is not null)
        {
            return (tenantRole.Id, tenantRole.RoleName);
        }

        var outletRole = await (
            from userRole in _dbContext.OutletUserRoles.AsNoTracking()
            join role in _dbContext.TenantRoles.AsNoTracking() on userRole.TenantRoleId equals role.Id
            where userRole.TenantId == tenantId && userRole.TenantUserId == userId && userRole.RevokedAt == null
            orderby userRole.AssignedAt descending
            select new { role.Id, role.RoleName }
        ).FirstOrDefaultAsync(cancellationToken);

        return outletRole is null ? null : (outletRole.Id, outletRole.RoleName);
    }

    private async Task<List<Guid>> GetActiveOutletIdsAsync(
        Guid tenantId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.OutletUserRoles
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.TenantUserId == userId && x.RevokedAt == null)
            .Select(x => x.OutletId)
            .Distinct()
            .ToListAsync(cancellationToken);
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
                   select new { role.Id, role.RoleName }
               ).FirstOrDefault()
               let outletRole = (
                   from userRole in _dbContext.OutletUserRoles.AsNoTracking()
                   join role in _dbContext.TenantRoles.AsNoTracking() on userRole.TenantRoleId equals role.Id
                   where userRole.TenantId == tenantId && userRole.TenantUserId == user.Id && userRole.RevokedAt == null
                   orderby userRole.AssignedAt descending
                   select new { role.Id, role.RoleName }
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
        var assignments = await (
            from userRole in _dbContext.OutletUserRoles.AsNoTracking()
            join outlet in _dbContext.Outlets.AsNoTracking() on userRole.OutletId equals outlet.Id
            where userRole.TenantId == tenantId &&
                  userIds.Contains(userRole.TenantUserId) &&
                  userRole.RevokedAt == null
            select new { userRole.TenantUserId, outlet.Id, Name = outlet.OutletName }
        ).ToListAsync(cancellationToken);

        var byUser = assignments
            .GroupBy(x => x.TenantUserId)
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var row in rows)
        {
            if (byUser.TryGetValue(row.UserId, out var outlets))
            {
                row.OutletIds = outlets.Select(x => x.Id).Distinct().ToList();
                row.OutletNames = outlets.Select(x => x.Name).Distinct().ToList();
            }
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
            row.OutletNames.Count == 0 ? "All Outlets" : string.Join(", ", row.OutletNames),
            FormatStatus(row.Status),
            row.LastActiveAt);
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

    private sealed class UserRow
    {
        public Guid UserId { get; init; }
        public string FullName { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public string? Phone { get; init; }
        public Guid? RoleId { get; init; }
        public string RoleName { get; init; } = string.Empty;
        public List<Guid> OutletIds { get; set; } = new();
        public List<string> OutletNames { get; set; } = new();
        public string Status { get; init; } = string.Empty;
        public DateTimeOffset? LastActiveAt { get; init; }
    }
}
