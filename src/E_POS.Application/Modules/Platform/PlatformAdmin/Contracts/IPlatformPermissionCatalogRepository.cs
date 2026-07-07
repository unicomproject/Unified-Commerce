using E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;

namespace E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;

public interface IPlatformPermissionCatalogRepository
{
    Task<IReadOnlyList<PlatformPermissionCatalogItem>> GetActiveBusinessPermissionsAsync(
        CancellationToken cancellationToken);
}

