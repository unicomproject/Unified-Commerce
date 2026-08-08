using E_POS.Application.Modules.Tenant.TenantFoundation.Contracts;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Constants;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace E_POS.Infrastructure.Modules.Tenant.TenantFoundation.Repositories;

public sealed class SettingDefinitionRepository : ISettingDefinitionRepository
{
    private readonly EPosDbContext _dbContext;

    public SettingDefinitionRepository(EPosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<SettingDefinition>> GetActiveByKeysAsync(
        IReadOnlyCollection<string> settingKeys,
        CancellationToken cancellationToken)
    {
        if (settingKeys.Count == 0)
        {
            return [];
        }

        var keys = settingKeys
            .Where(key => !string.IsNullOrWhiteSpace(key))
            .Select(key => key.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();

        return await _dbContext.SettingDefinitions
            .AsNoTracking()
            .Where(definition =>
                keys.Contains(definition.SettingKey) &&
                definition.Status == TenantSettingKeys.SettingDefinitionStatusActive)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlySet<Guid>> GetExistingSettingDefinitionIdsForTenantAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var ids = await _dbContext.TenantSettings
            .AsNoTracking()
            .Where(setting => setting.TenantId == tenantId)
            .Select(setting => setting.SettingDefinitionId)
            .ToListAsync(cancellationToken);

        return ids.ToHashSet();
    }
}
