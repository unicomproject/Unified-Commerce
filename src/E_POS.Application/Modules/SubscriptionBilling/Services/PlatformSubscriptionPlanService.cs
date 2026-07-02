using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.PlatformAdministration.Contracts;
using E_POS.Application.Modules.SubscriptionBilling.Contracts;
using E_POS.Application.Modules.SubscriptionBilling.Dtos;
using E_POS.Application.Modules.SubscriptionBilling.Mappers;
using E_POS.Domain.Modules.PlatformAdministration.Constants;
using E_POS.Domain.Modules.SubscriptionBilling.Constants;
using E_POS.Domain.Modules.SubscriptionBilling.Entities;

namespace E_POS.Application.Modules.SubscriptionBilling.Services;

public sealed class PlatformSubscriptionPlanService : IPlatformSubscriptionPlanService
{
    private static readonly ApplicationError AccessDenied = new(
        "platform_subscription_plans.access_denied",
        "Platform subscription plan access denied.");

    private static readonly ApplicationError NotFound = new(
        "platform_subscription_plans.not_found",
        "Subscription plan was not found.");

    private static readonly ApplicationError ValidationFailed = new(
        "platform_subscription_plans.validation_failed",
        "Subscription plan validation failed.");

    private static readonly ApplicationError Conflict = new(
        "platform_subscription_plans.conflict",
        "Subscription plan conflict.");

    private static readonly ApplicationError InvalidTransition = new(
        "platform_subscription_plans.invalid_transition",
        "Subscription plan status transition is not allowed.");

    private readonly IPlatformSubscriptionPlanRepository _repository;
    private readonly IPlatformPermissionRepository _permissionRepository;
    private readonly IPlatformPermissionChecker _permissionChecker;
    private readonly IDateTimeProvider _dateTimeProvider;

    public PlatformSubscriptionPlanService(
        IPlatformSubscriptionPlanRepository repository,
        IPlatformPermissionRepository permissionRepository,
        IPlatformPermissionChecker permissionChecker,
        IDateTimeProvider dateTimeProvider)
    {
        _repository = repository;
        _permissionRepository = permissionRepository;
        _permissionChecker = permissionChecker;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<ApplicationResult<SubscriptionPlanListResponse>> GetPlansAsync(
        SubscriptionPlanListQuery query,
        Guid platformUserId,
        CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(platformUserId, PlatformPermissionCodes.SubscriptionPlansView, cancellationToken))
        {
            return ApplicationResult<SubscriptionPlanListResponse>.Failure(AccessDenied);
        }

        var permissionFlags = await BuildPermissionFlagsAsync(platformUserId, cancellationToken);
        var response = await _repository.GetPlansAsync(query, permissionFlags, cancellationToken);
        return ApplicationResult<SubscriptionPlanListResponse>.Success(response);
    }

    public async Task<ApplicationResult<SubscriptionPlanCatalogResponse>> GetCatalogAsync(
        Guid platformUserId,
        CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(platformUserId, PlatformPermissionCodes.SubscriptionPlansView, cancellationToken))
        {
            return ApplicationResult<SubscriptionPlanCatalogResponse>.Failure(AccessDenied);
        }

        var catalog = await _repository.GetCatalogAsync(cancellationToken);
        return ApplicationResult<SubscriptionPlanCatalogResponse>.Success(catalog);
    }

    public async Task<ApplicationResult<SubscriptionPlanMutationResponse>> CreateDraftAsync(
        CreateSubscriptionPlanRequest request,
        Guid platformUserId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!await HasPermissionAsync(platformUserId, PlatformPermissionCodes.SubscriptionPlansCreate, cancellationToken))
        {
            return ApplicationResult<SubscriptionPlanMutationResponse>.Failure(AccessDenied);
        }

        var planCode = SubscriptionPlanMapper.NormalizePlanCode(request.PlanCode);
        var name = NormalizeRequiredText(request.Name);
        var billingInterval = SubscriptionPlanMapper.ToDbBillingInterval(request.BillingCycle);

        if (string.IsNullOrWhiteSpace(planCode))
        {
            return ApplicationResult<SubscriptionPlanMutationResponse>.Failure(
                ValidationFailed with { Message = "Plan code is required." });
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return ApplicationResult<SubscriptionPlanMutationResponse>.Failure(
                ValidationFailed with { Message = "Plan name is required." });
        }

        if (string.IsNullOrWhiteSpace(billingInterval))
        {
            return ApplicationResult<SubscriptionPlanMutationResponse>.Failure(
                ValidationFailed with { Message = "Billing cycle must be monthly, yearly, or one_time." });
        }

        if (await _repository.PlanCodeExistsAsync(planCode, cancellationToken))
        {
            return ApplicationResult<SubscriptionPlanMutationResponse>.Failure(
                Conflict with { Message = "A subscription plan with this plan code already exists." });
        }

        var basePrice = request.BasePrice ?? 0m;
        if (basePrice < 0)
        {
            return ApplicationResult<SubscriptionPlanMutationResponse>.Failure(
                ValidationFailed with { Message = "Base price must be greater than or equal to zero." });
        }

        ValidateLimits(request.MaxOutlets, request.MaxUsers, request.MaxTills, out var limitError);
        if (limitError is not null)
        {
            return ApplicationResult<SubscriptionPlanMutationResponse>.Failure(limitError);
        }

        var now = _dateTimeProvider.UtcNow;
        var plan = SubscriptionPlan.CreateDraft(
            Guid.NewGuid(),
            planCode,
            name,
            NormalizeOptionalText(request.Description),
            billingInterval,
            SubscriptionPlanMapper.NormalizeCurrency(request.BaseCurrency),
            basePrice,
            request.MaxOutlets,
            request.MaxUsers,
            request.MaxTills,
            now);

        await _repository.AddPlanAsync(plan, cancellationToken);

        var created = await _repository.GetPlanByIdAsync(
            plan.Id,
            await BuildPermissionFlagsAsync(platformUserId, cancellationToken),
            cancellationToken);

        return ApplicationResult<SubscriptionPlanMutationResponse>.Success(created!);
    }

    public async Task<ApplicationResult<SubscriptionPlanMutationResponse>> UpdatePricingAsync(
        Guid planId,
        UpdateSubscriptionPlanPricingRequest request,
        Guid platformUserId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!await HasPermissionAsync(platformUserId, PlatformPermissionCodes.SubscriptionPlansEdit, cancellationToken))
        {
            return ApplicationResult<SubscriptionPlanMutationResponse>.Failure(AccessDenied);
        }

        var plan = await _repository.GetPlanEntityByIdAsync(planId, cancellationToken);
        if (plan is null)
        {
            return ApplicationResult<SubscriptionPlanMutationResponse>.Failure(NotFound);
        }

        if (!IsDraft(plan))
        {
            return ApplicationResult<SubscriptionPlanMutationResponse>.Failure(
                InvalidTransition with { Message = "Only draft subscription plans can update pricing." });
        }

        if (request.BasePrice < 0)
        {
            return ApplicationResult<SubscriptionPlanMutationResponse>.Failure(
                ValidationFailed with { Message = "Base price must be greater than or equal to zero." });
        }

        plan.UpdatePricing(
            SubscriptionPlanMapper.NormalizeCurrency(request.BaseCurrency ?? plan.BaseCurrency),
            request.BasePrice,
            _dateTimeProvider.UtcNow);

        await _repository.SaveChangesAsync(cancellationToken);

        return await BuildMutationResultAsync(planId, platformUserId, cancellationToken);
    }

    public async Task<ApplicationResult<SubscriptionPlanMutationResponse>> UpdateLimitsAsync(
        Guid planId,
        UpdateSubscriptionPlanLimitsRequest request,
        Guid platformUserId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!await HasPermissionAsync(platformUserId, PlatformPermissionCodes.SubscriptionPlansEdit, cancellationToken))
        {
            return ApplicationResult<SubscriptionPlanMutationResponse>.Failure(AccessDenied);
        }

        var plan = await _repository.GetPlanEntityByIdAsync(planId, cancellationToken);
        if (plan is null)
        {
            return ApplicationResult<SubscriptionPlanMutationResponse>.Failure(NotFound);
        }

        if (!IsDraft(plan))
        {
            return ApplicationResult<SubscriptionPlanMutationResponse>.Failure(
                InvalidTransition with { Message = "Only draft subscription plans can update limits." });
        }

        var maxOutlets = request.MaxOutlets ?? plan.MaxOutlets;
        var maxUsers = request.MaxUsers ?? plan.MaxUsers;
        var maxTills = request.MaxTills ?? plan.MaxTills;

        ValidateLimits(maxOutlets, maxUsers, maxTills, out var limitError);
        if (limitError is not null)
        {
            return ApplicationResult<SubscriptionPlanMutationResponse>.Failure(limitError);
        }

        if (request.MaxOutlets is null && request.MaxUsers is null && request.MaxTills is null)
        {
            return ApplicationResult<SubscriptionPlanMutationResponse>.Failure(
                ValidationFailed with { Message = "At least one limit field is required." });
        }

        plan.UpdateLimits(maxOutlets, maxUsers, maxTills, _dateTimeProvider.UtcNow);
        await _repository.SaveChangesAsync(cancellationToken);

        return await BuildMutationResultAsync(planId, platformUserId, cancellationToken);
    }

    public async Task<ApplicationResult<SubscriptionPlanMutationResponse>> UpdateFeaturesAsync(
        Guid planId,
        UpdateSubscriptionPlanFeaturesRequest request,
        Guid platformUserId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!await HasPermissionAsync(platformUserId, PlatformPermissionCodes.SubscriptionPlansEdit, cancellationToken))
        {
            return ApplicationResult<SubscriptionPlanMutationResponse>.Failure(AccessDenied);
        }

        var plan = await _repository.GetPlanEntityByIdAsync(planId, cancellationToken);
        if (plan is null)
        {
            return ApplicationResult<SubscriptionPlanMutationResponse>.Failure(NotFound);
        }

        if (!IsDraft(plan))
        {
            return ApplicationResult<SubscriptionPlanMutationResponse>.Failure(
                InvalidTransition with { Message = "Only draft subscription plans can update features." });
        }

        var featureIds = request.FeatureIds?.Where(id => id != Guid.Empty).Distinct().ToList() ?? [];
        var activeFeatureIds = await _repository.GetActiveFeatureIdsAsync(featureIds, cancellationToken);
        var invalidFeatureIds = featureIds.Except(activeFeatureIds).ToList();

        if (invalidFeatureIds.Count > 0)
        {
            return ApplicationResult<SubscriptionPlanMutationResponse>.Failure(
                ValidationFailed with
                {
                    Message = $"Unknown or inactive feature ids: {string.Join(", ", invalidFeatureIds)}."
                });
        }

        await _repository.ReplacePlanFeaturesAsync(
            planId,
            activeFeatureIds.ToList(),
            _dateTimeProvider.UtcNow,
            cancellationToken);

        return await BuildMutationResultAsync(planId, platformUserId, cancellationToken);
    }

    public async Task<ApplicationResult<SubscriptionPlanMutationResponse>> PublishAsync(
        Guid planId,
        Guid platformUserId,
        CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(platformUserId, PlatformPermissionCodes.SubscriptionPlansEdit, cancellationToken))
        {
            return ApplicationResult<SubscriptionPlanMutationResponse>.Failure(AccessDenied);
        }

        var plan = await _repository.GetPlanEntityByIdAsync(planId, cancellationToken);
        if (plan is null)
        {
            return ApplicationResult<SubscriptionPlanMutationResponse>.Failure(NotFound);
        }

        if (!IsDraft(plan))
        {
            return ApplicationResult<SubscriptionPlanMutationResponse>.Failure(
                InvalidTransition with { Message = "Only draft subscription plans can be published." });
        }

        if (plan.PriceAmount < 0)
        {
            return ApplicationResult<SubscriptionPlanMutationResponse>.Failure(
                ValidationFailed with { Message = "Pricing must be configured before publishing." });
        }

        if (!plan.HasValidLimitsForPublish())
        {
            return ApplicationResult<SubscriptionPlanMutationResponse>.Failure(
                ValidationFailed with { Message = "At least one plan limit must be configured before publishing." });
        }

        plan.Publish(_dateTimeProvider.UtcNow);
        await _repository.SaveChangesAsync(cancellationToken);

        return await BuildMutationResultAsync(planId, platformUserId, cancellationToken);
    }

    private async Task<ApplicationResult<SubscriptionPlanMutationResponse>> BuildMutationResultAsync(
        Guid planId,
        Guid platformUserId,
        CancellationToken cancellationToken)
    {
        var response = await _repository.GetPlanByIdAsync(
            planId,
            await BuildPermissionFlagsAsync(platformUserId, cancellationToken),
            cancellationToken);

        return ApplicationResult<SubscriptionPlanMutationResponse>.Success(response!);
    }

    private async Task<SubscriptionPlanPermissionFlags> BuildPermissionFlagsAsync(
        Guid platformUserId,
        CancellationToken cancellationToken)
    {
        var permissions = await _permissionRepository.GetActivePermissionCodesAsync(
            platformUserId,
            cancellationToken);

        return new SubscriptionPlanPermissionFlags(
            CanCreate: permissions.Contains(PlatformPermissionCodes.SubscriptionPlansCreate),
            CanEdit: permissions.Contains(PlatformPermissionCodes.SubscriptionPlansEdit),
            CanDuplicate: permissions.Contains(PlatformPermissionCodes.SubscriptionPlansDuplicate),
            CanArchive: permissions.Contains(PlatformPermissionCodes.SubscriptionPlansArchive),
            CanDelete: permissions.Contains(PlatformPermissionCodes.SubscriptionPlansDelete));
    }

    private async Task<bool> HasPermissionAsync(
        Guid platformUserId,
        string permissionCode,
        CancellationToken cancellationToken)
    {
        return await _permissionChecker.HasPermissionAsync(
            platformUserId,
            permissionCode,
            cancellationToken);
    }

    private static bool IsDraft(SubscriptionPlan plan)
    {
        return string.Equals(plan.Status, SubscriptionPlanConstants.Status.Draft, StringComparison.Ordinal);
    }

    private static void ValidateLimits(
        int? maxOutlets,
        int? maxUsers,
        int? maxTills,
        out ApplicationError? error)
    {
        if (maxOutlets is < 0 || maxUsers is < 0 || maxTills is < 0)
        {
            error = ValidationFailed with { Message = "Plan limits must be greater than or equal to zero." };
            return;
        }

        error = null;
    }

    private static string NormalizeRequiredText(string? value)
    {
        return (value ?? string.Empty).Trim();
    }

    private static string? NormalizeOptionalText(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }
}
