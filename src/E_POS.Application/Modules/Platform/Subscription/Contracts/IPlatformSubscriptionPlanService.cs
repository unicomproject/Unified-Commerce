using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Platform.Subscription.Dtos;

namespace E_POS.Application.Modules.Platform.Subscription.Contracts;

public interface IPlatformSubscriptionPlanService
{
    Task<ApplicationResult<SubscriptionPlanListResponse>> GetPlansAsync(
        SubscriptionPlanListQuery query,
        Guid platformUserId,
        CancellationToken cancellationToken);

    Task<ApplicationResult<SubscriptionPlanCatalogResponse>> GetCatalogAsync(
        Guid platformUserId,
        CancellationToken cancellationToken);

    Task<ApplicationResult<SubscriptionPlanMutationResponse>> CreateDraftAsync(
        CreateSubscriptionPlanRequest request,
        Guid platformUserId,
        CancellationToken cancellationToken);

    Task<ApplicationResult<SubscriptionPlanMutationResponse>> UpdatePricingAsync(
        Guid planId,
        UpdateSubscriptionPlanPricingRequest request,
        Guid platformUserId,
        CancellationToken cancellationToken);

    Task<ApplicationResult<SubscriptionPlanMutationResponse>> UpdateLimitsAsync(
        Guid planId,
        UpdateSubscriptionPlanLimitsRequest request,
        Guid platformUserId,
        CancellationToken cancellationToken);

    Task<ApplicationResult<SubscriptionPlanMutationResponse>> UpdateFeaturesAsync(
        Guid planId,
        UpdateSubscriptionPlanFeaturesRequest request,
        Guid platformUserId,
        CancellationToken cancellationToken);

    Task<ApplicationResult<SubscriptionPlanMutationResponse>> PublishAsync(
        Guid planId,
        Guid platformUserId,
        CancellationToken cancellationToken);
}

