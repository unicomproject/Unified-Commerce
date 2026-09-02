using E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;
using E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace E_POS.Infrastructure.Modules.Platform.PlatformAdmin.Repositories;

public sealed class PlatformModulesCatalogRepository : IPlatformModulesCatalogRepository
{
    private readonly EPosDbContext _dbContext;

    public PlatformModulesCatalogRepository(EPosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<PlatformModulesCatalogModuleDto>> GetActiveModulesAsync(
        string? scopeFilter,
        CancellationToken cancellationToken)
    {
        var normalizedScope = scopeFilter?.Trim().ToUpperInvariant();

        var modulesQuery = _dbContext.PlatformModules
            .AsNoTracking()
            .Where(module => module.Status == "ACTIVE" &&
                _dbContext.PlatformFeatures.Any(feature =>
                    feature.PlatformModuleId == module.Id &&
                    feature.Status == "ACTIVE" &&
                    (string.IsNullOrEmpty(normalizedScope) || normalizedScope == "ALL" || feature.Scope.ToUpper() == normalizedScope)));

        if (!string.IsNullOrEmpty(normalizedScope) && normalizedScope != "ALL")
        {
            modulesQuery = modulesQuery.Where(module => module.Scope.ToUpper() == normalizedScope);
        }

        var modules = await modulesQuery
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
                module.Scope,
                Features = _dbContext.PlatformFeatures
                    .Where(feature =>
                        feature.PlatformModuleId == module.Id &&
                        feature.Status == "ACTIVE" &&
                        (string.IsNullOrEmpty(normalizedScope) || normalizedScope == "ALL" || feature.Scope.ToUpper() == normalizedScope))
                    .OrderBy(feature => feature.SortOrder)
                    .ThenBy(feature => feature.Name)
                    .Select(feature => new
                    {
                        feature.Id,
                        feature.FeatureCode,
                        feature.Name,
                        feature.Description,
                        feature.SortOrder,
                        feature.Status,
                        feature.Scope,
                        TenantPermissions = _dbContext.PermissionDefinitions
                            .Where(permission =>
                                permission.FeatureId == feature.Id &&
                                permission.IsActive &&
                                (string.IsNullOrEmpty(normalizedScope) || normalizedScope == "ALL" || permission.Scope.ToUpper() == normalizedScope))
                            .OrderBy(permission => permission.PermissionCode)
                            .Select(permission => new PlatformModulesCatalogPermissionDto(
                                permission.Id,
                                permission.PermissionCode,
                                permission.PermissionCode,
                                permission.Description,
                                permission.ActionType,
                                permission.Scope,
                                permission.IsActive))
                            .ToList(),
                        PlatformPermissions = _dbContext.PlatformPermissions
                            .Where(permission =>
                                permission.PlatformFeatureId == feature.Id &&
                                permission.Status == "ACTIVE" &&
                                (string.IsNullOrEmpty(normalizedScope) || normalizedScope == "ALL" || normalizedScope == "PLATFORM"))
                            .OrderBy(permission => permission.PermissionCode)
                            .Select(permission => new PlatformModulesCatalogPermissionDto(
                                permission.Id,
                                permission.PermissionCode,
                                permission.Name,
                                permission.Description,
                                permission.PermissionCode.EndsWith(".view", StringComparison.OrdinalIgnoreCase) ? "VIEW" : "MANAGE",
                                "PLATFORM",
                                true))
                            .ToList()
                    })
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
                module.Features
                    .Select(feature => new PlatformModulesCatalogFeatureDto(
                        feature.Id,
                        feature.FeatureCode,
                        feature.Name,
                        feature.Description,
                        feature.SortOrder,
                        feature.Status,
                        feature.TenantPermissions.Concat(feature.PlatformPermissions).ToList(),
                        feature.Scope))
                    .ToList(),
                module.Scope))
            .ToList();

    }

}


