using E_POS.Application.Modules.PlatformAdministration.Contracts;
using E_POS.Application.Modules.PlatformAdministration.Dtos;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace E_POS.Infrastructure.Modules.PlatformAdministration.Repositories;

public sealed class PlatformModulesCatalogRepository : IPlatformModulesCatalogRepository
{
    private readonly EPosDbContext _dbContext;

    public PlatformModulesCatalogRepository(EPosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<PlatformModulesCatalogModuleDto>> GetActiveModulesAsync(
        CancellationToken cancellationToken)
    {
        var modules = await _dbContext.PlatformModules
            .AsNoTracking()
            .Where(module => module.Status == "ACTIVE")
            .OrderBy(module => module.SortOrder)
            .ThenBy(module => module.Name)
            .Select(module => new
            {
                module.Id,
                module.ModuleCode,
                module.Name,
                module.Description,
                module.SortOrder,
                module.Status,
                Features = _dbContext.PlatformFeatures
                    .Where(feature =>
                        feature.PlatformModuleId == module.Id &&
                        feature.Status == "ACTIVE")
                    .OrderBy(feature => feature.SortOrder)
                    .ThenBy(feature => feature.Name)
                    .Select(feature => new PlatformModulesCatalogFeatureDto(
                        feature.Id,
                        feature.FeatureCode,
                        feature.Name,
                        feature.Description,
                        feature.SortOrder,
                        feature.Status))
                    .ToList()
            })
            .ToListAsync(cancellationToken);

        return modules
            .Select(module => new PlatformModulesCatalogModuleDto(
                module.Id,
                module.ModuleCode,
                module.Name,
                module.Description,
                module.SortOrder,
                module.Status,
                module.Features))
            .ToList();
    }
}
