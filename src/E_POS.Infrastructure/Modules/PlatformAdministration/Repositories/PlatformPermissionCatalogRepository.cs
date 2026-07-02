using E_POS.Application.Modules.PlatformAdministration.Contracts;
using E_POS.Application.Modules.PlatformAdministration.Dtos;
using E_POS.Domain.Modules.PlatformAdministration.Constants;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace E_POS.Infrastructure.Modules.PlatformAdministration.Repositories;

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
