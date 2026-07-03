using E_POS.Application.Common.Models;
using E_POS.Application.Modules.PlatformAdministration.Dtos;

namespace E_POS.Application.Modules.PlatformAdministration.Contracts;

public interface IPlatformModulesCatalogService
{
    Task<ApplicationResult<PlatformModulesCatalogResponse>> GetModulesAsync(
        Guid platformUserId,
        CancellationToken cancellationToken);
}
