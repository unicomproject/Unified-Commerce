using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;

namespace E_POS.Application.Modules.Tenant.TenantFoundation.Contracts;

public interface ISettingDefinitionRepository
{
    Task<IReadOnlyList<SettingDefinition>> GetActiveByKeysAsync(
        IReadOnlyCollection<string> settingKeys,
        CancellationToken cancellationToken);

    Task<IReadOnlySet<Guid>> GetExistingSettingDefinitionIdsForTenantAsync(
        Guid tenantId,
        CancellationToken cancellationToken);
}
