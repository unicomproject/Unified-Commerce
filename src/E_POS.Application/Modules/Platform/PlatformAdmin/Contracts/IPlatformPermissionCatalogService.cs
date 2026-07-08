using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;

namespace E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;

public interface IPlatformPermissionCatalogService
{
    Task<ApplicationResult<PlatformPermissionCatalogResponse>> GetCatalogAsync(
        Guid platformUserId,
        CancellationToken cancellationToken);

    Task<ApplicationResult<PlatformPermissionFlatResponse>> GetFlatCatalogAsync(
        Guid platformUserId,
        CancellationToken cancellationToken);
}

