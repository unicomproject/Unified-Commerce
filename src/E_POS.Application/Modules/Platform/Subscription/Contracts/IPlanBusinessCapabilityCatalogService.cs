using E_POS.Application.Modules.Platform.Subscription.Dtos;

namespace E_POS.Application.Modules.Platform.Subscription.Contracts;

public interface IPlanBusinessCapabilityCatalogService
{
    Task<IReadOnlyList<PlanBusinessModuleDto>> GetPlanBusinessModulesAsync(
        IReadOnlyCollection<Guid>? selectedFeatureIds,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<Guid>> GetMandatoryCoreFeatureIdsAsync(
        CancellationToken cancellationToken);
}
