using E_POS.Application.Modules.PlatformAdministration.Contracts;
using E_POS.Application.Modules.PlatformAdministration.Dtos;
using E_POS.Application.Modules.PlatformAdministration.Mappers;
using E_POS.Domain.Modules.PlatformAdministration.Constants;
using E_POS.Domain.Modules.PlatformAdministration.Entities;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace E_POS.Infrastructure.Modules.PlatformAdministration.Repositories;

public sealed class PlatformRoleRepository : IPlatformRoleRepository
{
    private static readonly HashSet<string> BusinessPermissionCodes =
        PlatformPermissionCodes.All.ToHashSet(StringComparer.Ordinal);

    private readonly EPosDbContext _dbContext;

    public PlatformRoleRepository(EPosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PlatformRoleListResponse> GetRolesAsync(CancellationToken cancellationToken)
    {
        var roles = await _dbContext.PlatformRoles
            .AsNoTracking()
            .OrderByDescending(role => role.RoleCode == PlatformRoleCodes.SuperAdministrator)
            .ThenBy(role => role.Name)
            .Select(role => new
            {
                role.Id,
                role.RoleCode,
                role.Name,
                role.Description,
                role.Status,
                role.CreatedAt,
                role.UpdatedAt,
                PermissionCount = (
                    from rolePermission in _dbContext.PlatformRolePermissions
                    join permission in _dbContext.PlatformPermissions
                        on rolePermission.PlatformPermissionId equals permission.Id
                    where rolePermission.PlatformRoleId == role.Id &&
                          permission.Status == PlatformAuthConstants.ActiveStatus &&
                          BusinessPermissionCodes.Contains(permission.PermissionCode)
                    select rolePermission.Id).Count(),
                UserCount = _dbContext.PlatformUserRoles.Count(userRole =>
                    userRole.PlatformRoleId == role.Id)
            })
            .ToListAsync(cancellationToken);

        var items = roles
            .Select(role => PlatformRoleMapper.ToListItem(
                role.Id,
                role.RoleCode,
                role.Name,
                role.Description,
                role.Status,
                role.PermissionCount,
                role.UserCount,
                role.CreatedAt,
                role.UpdatedAt ?? role.CreatedAt))
            .ToList();

        return new PlatformRoleListResponse(items);
    }

    public async Task<PlatformRoleDetailResponse?> GetRoleByIdAsync(
        Guid roleId,
        CancellationToken cancellationToken)
    {
        var role = await _dbContext.PlatformRoles
            .AsNoTracking()
            .Where(item => item.Id == roleId)
            .Select(item => new
            {
                item.Id,
                item.RoleCode,
                item.Name,
                item.Description,
                item.Status,
                item.CreatedAt,
                item.UpdatedAt,
                PermissionCount = (
                    from rolePermission in _dbContext.PlatformRolePermissions
                    join permission in _dbContext.PlatformPermissions
                        on rolePermission.PlatformPermissionId equals permission.Id
                    where rolePermission.PlatformRoleId == item.Id &&
                          permission.Status == PlatformAuthConstants.ActiveStatus &&
                          BusinessPermissionCodes.Contains(permission.PermissionCode)
                    select rolePermission.Id).Count(),
                UserCount = _dbContext.PlatformUserRoles.Count(userRole =>
                    userRole.PlatformRoleId == item.Id)
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (role is null)
        {
            return null;
        }

        return PlatformRoleMapper.ToDetail(
            PlatformRoleMapper.ToListItem(
                role.Id,
                role.RoleCode,
                role.Name,
                role.Description,
                role.Status,
                role.PermissionCount,
                role.UserCount,
                role.CreatedAt,
                role.UpdatedAt ?? role.CreatedAt));
    }

    public Task<PlatformRole?> GetRoleEntityByIdAsync(Guid roleId, CancellationToken cancellationToken)
    {
        return _dbContext.PlatformRoles
            .FirstOrDefaultAsync(role => role.Id == roleId, cancellationToken);
    }

    public Task<bool> RoleCodeExistsAsync(string roleCode, CancellationToken cancellationToken)
    {
        return _dbContext.PlatformRoles
            .AsNoTracking()
            .AnyAsync(role => role.RoleCode == roleCode, cancellationToken);
    }

    public async Task AddRoleAsync(PlatformRole role, CancellationToken cancellationToken)
    {
        _dbContext.PlatformRoles.Add(role);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateRoleAsync(PlatformRole role, CancellationToken cancellationToken)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<PlatformRolePermissionsResponse?> GetRolePermissionsAsync(
        IReadOnlyList<PlatformPermissionDto> availablePermissions,
        Guid roleId,
        CancellationToken cancellationToken)
    {
        var role = await _dbContext.PlatformRoles
            .AsNoTracking()
            .Where(item => item.Id == roleId)
            .Select(item => new
            {
                item.Id,
                item.RoleCode,
                item.Name,
                item.CreatedAt,
                item.UpdatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (role is null)
        {
            return null;
        }

        var assignedPermissionCodes = await (
            from rolePermission in _dbContext.PlatformRolePermissions.AsNoTracking()
            join permission in _dbContext.PlatformPermissions.AsNoTracking()
                on rolePermission.PlatformPermissionId equals permission.Id
            where rolePermission.PlatformRoleId == role.Id &&
                  permission.Status == PlatformAuthConstants.ActiveStatus &&
                  BusinessPermissionCodes.Contains(permission.PermissionCode)
            orderby permission.PermissionCode
            select permission.PermissionCode)
            .ToListAsync(cancellationToken);

        return new PlatformRolePermissionsResponse(
            role.Id,
            role.RoleCode,
            role.Name,
            assignedPermissionCodes,
            availablePermissions,
            role.UpdatedAt ?? role.CreatedAt);
    }

    public async Task<IReadOnlyDictionary<string, Guid>> GetActiveBusinessPermissionIdMapAsync(
        CancellationToken cancellationToken)
    {
        var businessPermissionCodes = PlatformPermissionCodes.All.ToHashSet(StringComparer.Ordinal);

        var permissions = await _dbContext.PlatformPermissions
            .AsNoTracking()
            .Where(permission =>
                permission.Status == PlatformAuthConstants.ActiveStatus &&
                businessPermissionCodes.Contains(permission.PermissionCode))
            .Select(permission => new { permission.PermissionCode, permission.Id })
            .ToListAsync(cancellationToken);

        return permissions.ToDictionary(
            permission => permission.PermissionCode,
            permission => permission.Id,
            StringComparer.Ordinal);
    }

    public async Task ReplaceRolePermissionsAsync(
        Guid roleId,
        IReadOnlyList<Guid> permissionIds,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var existing = await _dbContext.PlatformRolePermissions
            .Where(rolePermission => rolePermission.PlatformRoleId == roleId)
            .ToListAsync(cancellationToken);

        _dbContext.PlatformRolePermissions.RemoveRange(existing);

        var requestedPermissionIds = permissionIds
            .Where(permissionId => permissionId != Guid.Empty)
            .Distinct()
            .ToList();

        foreach (var permissionId in requestedPermissionIds)
        {
            _dbContext.PlatformRolePermissions.Add(PlatformRolePermission.Create(
                Guid.NewGuid(),
                roleId,
                permissionId,
                "Platform role permission assignment.",
                now));
        }

        var role = await _dbContext.PlatformRoles
            .FirstAsync(item => item.Id == roleId, cancellationToken);

        role.TouchUpdatedAt(now);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
