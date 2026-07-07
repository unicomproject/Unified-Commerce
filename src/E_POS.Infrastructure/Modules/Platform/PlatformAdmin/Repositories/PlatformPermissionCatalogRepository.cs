using E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;
using E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;
using E_POS.Domain.Modules.Platform.PlatformAdmin.Constants;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace E_POS.Infrastructure.Modules.Platform.PlatformAdmin.Repositories;

public sealed class PlatformPermissionCatalogRepository : IPlatformPermissionCatalogRepository
{
    private readonly EPosDbContext _dbContext;

    public PlatformPermissionCatalogRepository(EPosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<PlatformPermissionCatalogItem>> GetActiveBusinessPermissionsAsync(
        CancellationToken cancellationToken)
    {
        var businessPermissionCodes = PlatformPermissionCodes.All.ToHashSet(StringComparer.Ordinal);

        var permissions = await _dbContext.PlatformPermissions
            .AsNoTracking()
            .Where(permission =>
                permission.Status == PlatformAuthConstants.ActiveStatus &&
                businessPermissionCodes.Contains(permission.PermissionCode) &&
                permission.PermissionCode != PlatformBootstrapPermissionCodes.AdminAccess)
            .OrderBy(permission => permission.PermissionCode)
            .Select(permission => new PlatformPermissionCatalogItem(
                permission.Id,
                permission.PermissionCode,
                permission.Name,
                permission.Description,
                permission.Status))
            .ToListAsync(cancellationToken);

        return permissions;
    }
}



