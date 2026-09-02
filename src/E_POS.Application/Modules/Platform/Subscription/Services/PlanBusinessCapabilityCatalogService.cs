using E_POS.Application.Modules.Platform.Subscription.Contracts;
using E_POS.Application.Modules.Platform.Subscription.Dtos;
using E_POS.Domain.Modules.Platform.PlatformAdmin.Constants;
using E_POS.Domain.Modules.Platform.Subscription.Constants;

namespace E_POS.Application.Modules.Platform.Subscription.Services;

public sealed class PlanBusinessCapabilityCatalogService : IPlanBusinessCapabilityCatalogService
{
    private readonly IPlatformSubscriptionPlanRepository _repository;

    public PlanBusinessCapabilityCatalogService(IPlatformSubscriptionPlanRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<Guid>> GetMandatoryCoreFeatureIdsAsync(CancellationToken cancellationToken)
    {
        var activeFeatures = await _repository.GetActiveTenantFeaturesAsync(cancellationToken);
        var coreFeatureCodes = new[]
        {
            PlatformTenantFeatureCodes.TenantProfile,
            PlatformTenantFeatureCodes.TenantSettings
        };

        return activeFeatures
            .Where(f => coreFeatureCodes.Contains(f.FeatureCode, StringComparer.OrdinalIgnoreCase))
            .Select(f => f.Id)
            .ToList();
    }

    public async Task<IReadOnlyList<PlanBusinessModuleDto>> GetPlanBusinessModulesAsync(
        IReadOnlyCollection<Guid>? selectedFeatureIds,
        CancellationToken cancellationToken)
    {
        var selectedSet = selectedFeatureIds is not null
            ? selectedFeatureIds.ToHashSet()
            : new HashSet<Guid>();

        var activeFeatures = await _repository.GetActiveTenantFeaturesAsync(cancellationToken);

        var featuresByCanonicalCode = activeFeatures
            .GroupBy(f => PlatformTenantFeatureCodes.NormalizeToCanonicalOrSelf(f.FeatureCode), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => g.First(),
                StringComparer.OrdinalIgnoreCase);

        var resultModules = new List<PlanBusinessModuleDto>();

        foreach (var bmDef in BusinessCapabilityCatalog.Modules)
        {
            var capDtos = bmDef.Capabilities
                .Select(c => new PlanBusinessCapabilityDto(
                    c.Code,
                    c.Name,
                    c.Description,
                    c.CommercialClassification,
                    c.MappedTechnicalFeatureCodes))
                .ToList();

            var mappedFeatureCodes = bmDef.Capabilities
                .SelectMany(c => c.MappedTechnicalFeatureCodes)
                .Select(PlatformTenantFeatureCodes.NormalizeToCanonicalOrSelf)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var techFeatureDtos = new List<PlanTechnicalFeatureDto>();

            foreach (var featureCode in mappedFeatureCodes)
            {
                if (!featuresByCanonicalCode.TryGetValue(featureCode, out var feature))
                {
                    continue;
                }

                var isPlanEligible = CommercialSubscriptionFeatureCatalog.IsCommercialSubscriptionSelectable(feature.FeatureCode);
                var isCore = string.Equals(feature.FeatureCode, PlatformTenantFeatureCodes.TenantProfile, StringComparison.OrdinalIgnoreCase) ||
                             string.Equals(feature.FeatureCode, PlatformTenantFeatureCodes.TenantSettings, StringComparison.OrdinalIgnoreCase);

                string classification;
                string selectionBehavior;

                if (isCore)
                {
                    classification = CommercialClassification.CoreAlwaysIncluded;
                    selectionBehavior = "CORE_REQUIRED";
                }
                else if (isPlanEligible)
                {
                    classification = CommercialClassification.PlanSelectable;
                    selectionBehavior = "OPTIONAL";
                }
                else
                {
                    classification = CommercialClassification.CoreEntitlementIndependent;
                    selectionBehavior = "NOT_SELECTABLE";
                }

                var isSelected = isCore || selectedSet.Contains(feature.Id);

                techFeatureDtos.Add(new PlanTechnicalFeatureDto(
                    feature.Id,
                    feature.FeatureCode,
                    feature.Name,
                    feature.Description,
                    IsActive: string.Equals(feature.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase),
                    IsPlanEligible: isPlanEligible,
                    CommercialClassification: classification,
                    SelectionBehavior: selectionBehavior,
                    IsSelected: isSelected));
            }

            string moduleSelectionState;
            if (string.Equals(bmDef.CommercialState, "CORE", StringComparison.OrdinalIgnoreCase))
            {
                moduleSelectionState = "CORE";
            }
            else
            {
                var optionalFeatures = techFeatureDtos
                    .Where(f => string.Equals(f.SelectionBehavior, "OPTIONAL", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (optionalFeatures.Count == 0)
                {
                    moduleSelectionState = "CORE";
                }
                else
                {
                    var selectedOptionalCount = optionalFeatures.Count(f => f.IsSelected);
                    if (selectedOptionalCount == optionalFeatures.Count)
                    {
                        moduleSelectionState = "INCLUDED";
                    }
                    else if (selectedOptionalCount > 0)
                    {
                        moduleSelectionState = "PARTIALLY_INCLUDED";
                    }
                    else
                    {
                        moduleSelectionState = "NOT_INCLUDED";
                    }
                }
            }

            resultModules.Add(new PlanBusinessModuleDto(
                bmDef.Code,
                bmDef.Name,
                bmDef.Description,
                bmDef.DisplayOrder,
                bmDef.CurrentR1Status,
                bmDef.CommercialState,
                capDtos,
                techFeatureDtos,
                moduleSelectionState));
        }

        return resultModules;
    }
}
