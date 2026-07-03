using E_POS.Application.Modules.PlatformAdministration.Dtos;

namespace E_POS.Application.Modules.PlatformAdministration.Contracts;

public interface IPlatformModulesCatalogRepository
{
    Task<IReadOnlyList<PlatformModulesCatalogModuleDto>> GetActiveModulesAsync(
        CancellationToken cancellationToken);
}
