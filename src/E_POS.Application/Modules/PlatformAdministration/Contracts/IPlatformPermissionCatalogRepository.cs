using E_POS.Application.Modules.PlatformAdministration.Dtos;

namespace E_POS.Application.Modules.PlatformAdministration.Contracts;

public interface IPlatformPermissionCatalogRepository
{
    Task<IReadOnlyList<PlatformPermissionCatalogItem>> GetActiveBusinessPermissionsAsync(
        CancellationToken cancellationToken);
}
