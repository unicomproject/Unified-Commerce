using E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;

namespace E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;

public interface IPlatformModulesCatalogRepository
{
    Task<IReadOnlyList<PlatformModulesCatalogModuleDto>> GetActiveModulesAsync(
        CancellationToken cancellationToken);
}

