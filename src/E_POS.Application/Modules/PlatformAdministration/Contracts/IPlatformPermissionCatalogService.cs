using E_POS.Application.Common.Models;
using E_POS.Application.Modules.PlatformAdministration.Dtos;

namespace E_POS.Application.Modules.PlatformAdministration.Contracts;

public interface IPlatformPermissionCatalogService
{
    Task<ApplicationResult<PlatformPermissionCatalogResponse>> GetCatalogAsync(
        Guid platformUserId,
        CancellationToken cancellationToken);

    Task<ApplicationResult<PlatformPermissionFlatResponse>> GetFlatCatalogAsync(
        Guid platformUserId,
        CancellationToken cancellationToken);
}
