using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;

namespace E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;

public interface IPlatformModulesCatalogService
{
    Task<ApplicationResult<PlatformModulesCatalogResponse>> GetModulesAsync(
        Guid platformUserId,
        CancellationToken cancellationToken);
}

