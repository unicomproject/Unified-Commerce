using E_POS.Application.Modules.Platform.Subscription.Contracts;
using E_POS.Application.Modules.Platform.Subscription.Dtos;
using E_POS.Application.Modules.Platform.Subscription.Mappers;
using E_POS.Domain.Modules.Platform.Subscription.Constants;
using E_POS.Domain.Modules.Platform.Subscription.Entities;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace E_POS.Infrastructure.Modules.Platform.Subscription.Repositories;

public sealed class PlatformSubscriptionPlanRepository : IPlatformSubscriptionPlanRepository
{
    private const int DefaultPageSize = 10;
    private const int MaxPageSize = 100;

    private readonly EPosDbContext _dbContext;

    public PlatformSubscriptionPlanRepository(EPosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<SubscriptionPlanListResponse> GetPlansAsync(
        SubscriptionPlanListQuery query,
        SubscriptionPlanPermissionFlags permissionFlags,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        NormalizeQuery(query);

        var plansQuery = _dbContext.SubscriptionPlans.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            plansQuery = plansQuery.Where(plan =>
                EF.Functions.ILike(plan.Name, $"%{search}%") ||
                EF.Functions.ILike(plan.PlanCode, $"%{search}%"));
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            var status = SubscriptionPlanMapper.NormalizeApiStatus(query.Status);
            plansQuery = plansQuery.Where(plan => plan.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(query.BillingCycle))
        {
            var billingInterval = SubscriptionPlanMapper.ToDbBillingInterval(query.BillingCycle);
            if (!string.IsNullOrWhiteSpace(billingInterval))
            {
                plansQuery = plansQuery.Where(plan => plan.BillingInterval == billingInterval);
            }
        }

        var totalCount = await plansQuery.CountAsync(cancellationToken);
        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)query.PageSize);

        var rows = await plansQuery
            .OrderByDescending(plan => plan.UpdatedAt ?? plan.CreatedAt)
            .ThenBy(plan => plan.PlanCode)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(plan => new
            {
                plan.Id,
                plan.PlanCode,
                plan.Name,
                plan.Description,
                plan.Status,
                plan.BillingInterval,
                plan.BaseCurrency,
                plan.PriceAmount,
                plan.MaxOutlets,
                plan.MaxUsers,
                plan.MaxTills,
                plan.UpdatedAt,
                plan.CreatedAt,
                FeatureCount = _dbContext.SubscriptionPlanFeatures.Count(feature =>
                    feature.SubscriptionPlanId == plan.Id &&
                    feature.Status == SubscriptionPlanConstants.PlanFeatureStatus.Included),
                ActiveTenantCount = _dbContext.TenantSubscriptions.Count(subscription =>
                    subscription.SubscriptionPlanId == plan.Id &&
                    subscription.SubscriptionStatus == "ACTIVE")
            })
            .ToListAsync(cancellationToken);

        var items = rows
            .Select(row => new SubscriptionPlanListItemDto(
                row.Id,
                row.PlanCode,
                row.Name,
                row.Description,
                row.Status,
                SubscriptionPlanMapper.ToApiBillingCycle(row.BillingInterval),
                row.BaseCurrency,
                row.PriceAmount,
                row.MaxOutlets,
                row.MaxUsers,
                row.MaxTills,
                row.FeatureCount,
                row.ActiveTenantCount,
                CanEdit: permissionFlags.CanEdit && row.Status == SubscriptionPlanConstants.Status.Draft,
                CanDuplicate: permissionFlags.CanDuplicate,
                CanArchive: permissionFlags.CanArchive && row.Status == SubscriptionPlanConstants.Status.Active,
                CanDelete: permissionFlags.CanDelete && row.Status == SubscriptionPlanConstants.Status.Draft,
                row.UpdatedAt ?? row.CreatedAt))
            .ToList();

        return new SubscriptionPlanListResponse(
            items,
            query.PageNumber,
            query.PageSize,
            totalCount,
            totalPages,
            permissionFlags.CanCreate,
            permissionFlags.CanEdit,
            permissionFlags.CanDuplicate,
            permissionFlags.CanArchive,
            permissionFlags.CanDelete);
    }

    public async Task<SubscriptionPlanCatalogResponse> GetCatalogAsync(CancellationToken cancellationToken)
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
                Features = _dbContext.PlatformFeatures
                    .Where(feature =>
                        feature.PlatformModuleId == module.Id &&
                        feature.Status == "ACTIVE")
                    .OrderBy(feature => feature.SortOrder)
                    .ThenBy(feature => feature.Name)
                    .Select(feature => new SubscriptionPlanCatalogFeatureDto(
                        feature.Id,
                        feature.FeatureCode,
                        feature.Name,
                        feature.Description,
                        feature.SortOrder))
                    .ToList()
            })
            .ToListAsync(cancellationToken);

        var moduleDtos = modules
            .Select(module => new SubscriptionPlanCatalogModuleDto(
                module.Id,
                module.ModuleCode,
                module.Name,
                module.Description,
                module.SortOrder,
                module.Features))
            .ToList();

        return new SubscriptionPlanCatalogResponse(moduleDtos);
    }

    public Task<bool> PlanCodeExistsAsync(string planCode, CancellationToken cancellationToken)
    {
        return _dbContext.SubscriptionPlans
            .AsNoTracking()
            .AnyAsync(plan => plan.PlanCode == planCode, cancellationToken);
    }

    public Task<SubscriptionPlan?> GetPlanEntityByIdAsync(Guid planId, CancellationToken cancellationToken)
    {
        return _dbContext.SubscriptionPlans
            .FirstOrDefaultAsync(plan => plan.Id == planId, cancellationToken);
    }

    public async Task<SubscriptionPlanMutationResponse?> GetPlanByIdAsync(
        Guid planId,
        SubscriptionPlanPermissionFlags permissionFlags,
        CancellationToken cancellationToken)
    {
        var plan = await _dbContext.SubscriptionPlans
            .AsNoTracking()
            .Where(item => item.Id == planId)
            .Select(item => new
            {
                item.Id,
                item.PlanCode,
                item.Name,
                item.Status,
                item.BillingInterval,
                item.BaseCurrency,
                item.PriceAmount,
                item.MaxOutlets,
                item.MaxUsers,
                item.MaxTills,
                item.UpdatedAt,
                item.CreatedAt,
                FeatureCount = _dbContext.SubscriptionPlanFeatures.Count(feature =>
                    feature.SubscriptionPlanId == item.Id &&
                    feature.Status == SubscriptionPlanConstants.PlanFeatureStatus.Included)
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (plan is null)
        {
            return null;
        }

        return ToMutationResponse(
            plan.Id,
            plan.PlanCode,
            plan.Name,
            plan.Status,
            plan.BillingInterval,
            plan.BaseCurrency,
            plan.PriceAmount,
            plan.MaxOutlets,
            plan.MaxUsers,
            plan.MaxTills,
            plan.FeatureCount,
            plan.UpdatedAt ?? plan.CreatedAt);
    }

    public async Task AddPlanAsync(SubscriptionPlan plan, CancellationToken cancellationToken)
    {
        _dbContext.SubscriptionPlans.Add(plan);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ReplacePlanFeaturesAsync(
        Guid planId,
        IReadOnlyList<Guid> featureIds,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var existing = await _dbContext.SubscriptionPlanFeatures
            .Where(feature => feature.SubscriptionPlanId == planId)
            .ToListAsync(cancellationToken);

        _dbContext.SubscriptionPlanFeatures.RemoveRange(existing);

        var sortOrder = 1;
        foreach (var featureId in featureIds.Distinct())
        {
            _dbContext.SubscriptionPlanFeatures.Add(SubscriptionPlanFeature.CreateIncluded(
                Guid.NewGuid(),
                planId,
                featureId,
                sortOrder,
                now));

            sortOrder++;
        }

        var plan = await _dbContext.SubscriptionPlans
            .FirstAsync(item => item.Id == planId, cancellationToken);

        plan.TouchUpdatedAt(now);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlySet<Guid>> GetActiveFeatureIdsAsync(
        IReadOnlyCollection<Guid> featureIds,
        CancellationToken cancellationToken)
    {
        if (featureIds.Count == 0)
        {
            return new HashSet<Guid>();
        }

        var ids = await _dbContext.PlatformFeatures
            .AsNoTracking()
            .Where(feature => featureIds.Contains(feature.Id) && feature.Status == "ACTIVE")
            .Select(feature => feature.Id)
            .ToListAsync(cancellationToken);

        return ids.ToHashSet();
    }

    public Task<int> GetFeatureCountAsync(Guid planId, CancellationToken cancellationToken)
    {
        return _dbContext.SubscriptionPlanFeatures
            .AsNoTracking()
            .CountAsync(
                feature =>
                    feature.SubscriptionPlanId == planId &&
                    feature.Status == SubscriptionPlanConstants.PlanFeatureStatus.Included,
                cancellationToken);
    }

    public async Task UpsertLegacyPlanLimitsAsync(
        Guid planId,
        int? maxOutlets,
        int? maxUsers,
        int? maxTills,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        await UpsertPlanLimitAsync(
            planId,
            SubscriptionCatalogLimitSeedConstants.MaxOutletsLimitDefinitionId,
            maxOutlets,
            now,
            cancellationToken);

        await UpsertPlanLimitAsync(
            planId,
            SubscriptionCatalogLimitSeedConstants.MaxUsersLimitDefinitionId,
            maxUsers,
            now,
            cancellationToken);

        await UpsertPlanLimitAsync(
            planId,
            SubscriptionCatalogLimitSeedConstants.MaxTillsLimitDefinitionId,
            maxTills,
            now,
            cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyDictionary<string, decimal?>> GetPlanLimitValuesByKeyAsync(
        Guid planId,
        CancellationToken cancellationToken)
    {
        var limits = await (
            from planLimit in _dbContext.SubscriptionPlanFeatureLimits.AsNoTracking()
            join limitDefinition in _dbContext.FeatureLimitDefinitions.AsNoTracking()
                on planLimit.FeatureLimitDefinitionId equals limitDefinition.Id
            where planLimit.SubscriptionPlanId == planId
            select new
            {
                limitDefinition.LimitKey,
                planLimit.LimitValue
            })
            .ToListAsync(cancellationToken);

        return limits.ToDictionary(
            item => item.LimitKey,
            item => item.LimitValue,
            StringComparer.Ordinal);
    }

    private async Task UpsertPlanLimitAsync(
        Guid planId,
        Guid featureLimitDefinitionId,
        int? legacyLimitValue,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var existing = await _dbContext.SubscriptionPlanFeatureLimits
            .FirstOrDefaultAsync(
                item =>
                    item.SubscriptionPlanId == planId &&
                    item.FeatureLimitDefinitionId == featureLimitDefinitionId,
                cancellationToken);

        if (legacyLimitValue is null)
        {
            if (existing is not null)
            {
                _dbContext.SubscriptionPlanFeatureLimits.Remove(existing);
            }

            return;
        }

        if (existing is null)
        {
            _dbContext.SubscriptionPlanFeatureLimits.Add(SubscriptionPlanFeatureLimit.Create(
                Guid.NewGuid(),
                planId,
                featureLimitDefinitionId,
                legacyLimitValue.Value,
                isUnlimited: false,
                now));
            return;
        }

        existing.UpdateLimit(legacyLimitValue.Value, isUnlimited: false, now);
    }

    private static SubscriptionPlanMutationResponse ToMutationResponse(
        Guid id,
        string planCode,
        string name,
        string status,
        string billingInterval,
        string baseCurrency,
        decimal basePrice,
        int? maxOutlets,
        int? maxUsers,
        int? maxTills,
        int featureCount,
        DateTimeOffset updatedAt)
    {
        return new SubscriptionPlanMutationResponse(
            id,
            planCode,
            name,
            status,
            SubscriptionPlanMapper.ToApiBillingCycle(billingInterval),
            baseCurrency,
            basePrice,
            maxOutlets,
            maxUsers,
            maxTills,
            featureCount,
            updatedAt);
    }

    private static void NormalizeQuery(SubscriptionPlanListQuery query)
    {
        query.PageNumber = query.PageNumber < 1 ? 1 : query.PageNumber;
        query.PageSize = query.PageSize < 1 ? DefaultPageSize : Math.Min(query.PageSize, MaxPageSize);
    }
}



