using E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;
using E_POS.Domain.Modules.Platform.PlatformAdmin.Constants;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace E_POS.Infrastructure.Modules.Platform.PlatformAdmin.Repositories;

public sealed class PlatformPermissionRepository : IPlatformPermissionRepository
{
    private readonly EPosDbContext _dbContext;

    public PlatformPermissionRepository(EPosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlySet<string>> GetActivePermissionCodesAsync(
        Guid platformUserId,
        CancellationToken cancellationToken)
    {
        var directPermissions =
            from userPermission in _dbContext.PlatformUserPermissions.AsNoTracking()
            join permission in _dbContext.PlatformPermissions.AsNoTracking()
                on userPermission.PlatformPermissionId equals permission.Id
            where userPermission.PlatformUserId == platformUserId &&
                  permission.Status == PlatformAuthConstants.ActiveStatus
            select permission.PermissionCode;

        var rolePermissions =
            from userRole in _dbContext.PlatformUserRoles.AsNoTracking()
            join role in _dbContext.PlatformRoles.AsNoTracking()
                on userRole.PlatformRoleId equals role.Id
            join rolePermission in _dbContext.PlatformRolePermissions.AsNoTracking()
                on role.Id equals rolePermission.PlatformRoleId
            join permission in _dbContext.PlatformPermissions.AsNoTracking()
                on rolePermission.PlatformPermissionId equals permission.Id
            where userRole.PlatformUserId == platformUserId &&
                  role.Status == PlatformAuthConstants.ActiveStatus &&
                  permission.Status == PlatformAuthConstants.ActiveStatus
            select permission.PermissionCode;

        var codes = await directPermissions
            .Union(rolePermissions)
            .Where(x => x != string.Empty)
            .ToListAsync(cancellationToken);

        return codes.ToHashSet(StringComparer.Ordinal);
    }
}



