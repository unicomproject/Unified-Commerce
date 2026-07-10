using E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;
using E_POS.Domain.Modules.Platform.Subscription.Constants;
using E_POS.Infrastructure.Modules.Platform.Subscription.Entitlements;
using Microsoft.EntityFrameworkCore;

namespace E_POS.Infrastructure.Modules.Platform.PlatformAdmin.Repositories;

public sealed partial class PlatformTenantRepository
{
    public async Task<PlatformTenantEntitlementOptionsResponse?> GetEntitlementOptionsAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var tenantExists = await _dbContext.Tenants
            .AsNoTracking()
            .AnyAsync(x => x.Id == tenantId, cancellationToken);

        if (!tenantExists)
        {
            return null;
        }

        var subscriptionRows = await (
            from tenantSubscription in _dbContext.TenantSubscriptions.AsNoTracking()
            join plan in _dbContext.SubscriptionPlans.AsNoTracking()
                on tenantSubscription.SubscriptionPlanId equals plan.Id
            where tenantSubscription.TenantId == tenantId
            orderby tenantSubscription.CreatedAt descending
            select new
            {
                tenantSubscription.SubscriptionPlanId,
                plan.PlanCode,
                plan.Name,
                tenantSubscription.SubscriptionStatus,
                tenantSubscription.CreatedAt
            })
            .ToListAsync(cancellationToken);

        var currentSubscription = subscriptionRows
            .FirstOrDefault(x => IsAnyStatus(x.SubscriptionStatus, CurrentSubscriptionStatuses))
            ?? subscriptionRows.FirstOrDefault();

        var enabledFeatures = await (
            from entitlement in _dbContext.TenantFeatureEntitlements.AsNoTracking()
            join feature in _dbContext.PlatformFeatures.AsNoTracking()
                on entitlement.PlatformFeatureId equals feature.Id
            where entitlement.TenantId == tenantId
            select new EntitlementReadRow(
                entitlement.EntitlementStatus,
                entitlement.IsEnabled,
                entitlement.RevokedAt,
                entitlement.EffectiveFrom,
                entitlement.EffectiveUntil,
                feature.Id,
                feature.FeatureCode))
            .ToListAsync(cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var enabled = enabledFeatures
            .Where(x => TenantEntitlementEffectivePredicate.IsEnabled(
                x.EntitlementStatus,
                x.IsEnabled,
                x.RevokedAt,
                x.EffectiveFrom,
                x.EffectiveUntil,
                now))
            .ToList();

        var enabledFeatureIds = enabled
            .Select(x => x.FeatureId)
            .Distinct()
            .ToList();

        var enabledFeatureCodes = enabled
            .Select(x => x.FeatureCode)
            .Distinct()
            .ToList();

        var plans = await BuildEntitlementPlanOptionsAsync(cancellationToken);
        var catalogModules = await BuildEntitlementCatalogModulesAsync(cancellationToken);

        return new PlatformTenantEntitlementOptionsResponse(
            tenantId,
            currentSubscription?.SubscriptionPlanId,
            currentSubscription?.PlanCode,
            currentSubscription?.Name,
            enabledFeatureIds,
            enabledFeatureCodes,
            plans,
            catalogModules);
    }

    private async Task<IReadOnlyList<PlatformTenantEntitlementPlanOptionDto>> BuildEntitlementPlanOptionsAsync(
        CancellationToken cancellationToken)
    {
        var activePlans = await _dbContext.SubscriptionPlans
            .AsNoTracking()
            .Where(plan => plan.Status.ToLower() == SubscriptionPlanConstants.Status.Active)
            .OrderBy(plan => plan.Name)
            .Select(plan => new
            {
                plan.Id,
                plan.PlanCode,
                plan.Name,
                plan.Status
            })
            .ToListAsync(cancellationToken);

        var planFeatureRows = await (
            from planFeature in _dbContext.SubscriptionPlanFeatures.AsNoTracking()
            join feature in _dbContext.PlatformFeatures.AsNoTracking()
                on planFeature.PlatformFeatureId equals feature.Id
            where planFeature.Status == SubscriptionPlanConstants.PlanFeatureStatus.Included &&
                  feature.Status == "ACTIVE"
            select new
            {
                planFeature.SubscriptionPlanId,
                feature.Id,
                feature.FeatureCode
            })
            .ToListAsync(cancellationToken);

        var includedFeaturesByPlan = planFeatureRows
            .GroupBy(x => x.SubscriptionPlanId)
            .ToDictionary(
                group => group.Key,
                group => new
                {
                    FeatureIds = group.Select(x => x.Id).Distinct().ToList(),
                    FeatureCodes = group.Select(x => x.FeatureCode).Distinct().ToList()
                });

        return activePlans
            .Select(plan =>
            {
                includedFeaturesByPlan.TryGetValue(plan.Id, out var planFeatures);
                return new PlatformTenantEntitlementPlanOptionDto(
                    plan.Id,
                    plan.PlanCode,
                    plan.Name,
                    plan.Status,
                    planFeatures?.FeatureIds ?? [],
                    planFeatures?.FeatureCodes ?? []);
            })
            .ToList();
    }

    private async Task<IReadOnlyList<PlatformTenantEntitlementCatalogModuleDto>> BuildEntitlementCatalogModulesAsync(
        CancellationToken cancellationToken)
    {
        var modules = await _dbContext.PlatformModules
            .AsNoTracking()
            .Where(module => module.Status == "ACTIVE")
            .OrderBy(module => module.SortOrder)
            .ThenBy(module => module.Name)
            .Select(module => new PlatformTenantEntitlementCatalogModuleDto(
                module.Id,
                module.ModuleCode,
                module.Name,
                _dbContext.PlatformFeatures
                    .Where(feature => feature.PlatformModuleId == module.Id && feature.Status == "ACTIVE")
                    .OrderBy(feature => feature.SortOrder)
                    .ThenBy(feature => feature.Name)
                    .Select(feature => new PlatformTenantEntitlementCatalogFeatureDto(
                        feature.Id,
                        feature.FeatureCode,
                        feature.Name,
                        feature.Description))
                    .ToList()))
            .ToListAsync(cancellationToken);

        return modules;
    }
}



