using E_POS.Application.Modules.Platform.Subscription.Dtos;
using E_POS.Domain.Modules.Platform.Subscription.Entities;

namespace E_POS.Application.Modules.Platform.Subscription.Contracts;

public interface IPlatformSubscriptionPlanRepository
{
    Task<SubscriptionPlanListResponse> GetPlansAsync(
        SubscriptionPlanListQuery query,
        SubscriptionPlanPermissionFlags permissionFlags,
        CancellationToken cancellationToken);

    Task<SubscriptionPlanCatalogResponse> GetCatalogAsync(CancellationToken cancellationToken);

    Task<bool> PlanCodeExistsAsync(string planCode, CancellationToken cancellationToken);

    Task<SubscriptionPlan?> GetPlanEntityByIdAsync(Guid planId, CancellationToken cancellationToken);

    Task<SubscriptionPlanMutationResponse?> GetPlanByIdAsync(
        Guid planId,
        SubscriptionPlanPermissionFlags permissionFlags,
        CancellationToken cancellationToken);

    Task AddPlanAsync(SubscriptionPlan plan, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);

    Task ReplacePlanFeaturesAsync(
        Guid planId,
        IReadOnlyList<Guid> featureIds,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task<IReadOnlySet<Guid>> GetActiveFeatureIdsAsync(
        IReadOnlyCollection<Guid> featureIds,
        CancellationToken cancellationToken);

    Task<int> GetFeatureCountAsync(Guid planId, CancellationToken cancellationToken);

    Task UpsertLegacyPlanLimitsAsync(
        Guid planId,
        int? maxOutlets,
        int? maxUsers,
        int? maxTills,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<string, decimal?>> GetPlanLimitValuesByKeyAsync(
        Guid planId,
        CancellationToken cancellationToken);
}

public sealed record SubscriptionPlanPermissionFlags(
    bool CanCreate,
    bool CanEdit,
    bool CanDuplicate,
    bool CanArchive,
    bool CanDelete);


