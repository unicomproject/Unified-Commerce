using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Common.Security;
using E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;
using E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;
using E_POS.Application.Modules.Platform.PlatformAdmin.Mappers;
using E_POS.Application.Modules.Platform.Subscription.Contracts;
using E_POS.Domain.Modules.Platform.PlatformAdmin.Constants;
using E_POS.Domain.Modules.Platform.Subscription.Constants;
using E_POS.Domain.Modules.Platform.Subscription.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Constants;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;

namespace E_POS.Application.Modules.Platform.PlatformAdmin.Services;

public sealed partial class PlatformTenantService : IPlatformTenantService
{
    private const int DefaultPageSize = 10;
    private const int MaxPageSize = 100;
    private const string DefaultBaseCurrency = "LKR";
    private const string DefaultTimezone = "Asia/Colombo";
    private const string DefaultLocale = "en-LK";
    private const string DefaultOperatingMode = "unified_epos";

    private static readonly ApplicationError AccessDenied = new(
        "platform_tenants.access_denied",
        "Platform tenant access denied.");

    private static readonly ApplicationError NotFound = new(
        "platform_tenants.not_found",
        "Platform tenant not found.");

    private static readonly ApplicationError ValidationFailed = new(
        "platform_tenants.validation_failed",
        "Platform tenant validation failed.");

    private static readonly ApplicationError Conflict = new(
        "platform_tenants.conflict",
        "Platform tenant conflict.");

    private static readonly ApplicationError InvalidTransition = new(
        "platform_tenants.invalid_transition",
        "Platform tenant status transition is not allowed.");

    private static readonly HashSet<string> AllowedBillingStatuses =
    [
        TenantBillingStatusConstants.Pending,
        TenantBillingStatusConstants.Paid,
        TenantBillingStatusConstants.Overdue,
        TenantBillingStatusConstants.Failed,
        TenantBillingStatusConstants.Waived
    ];

    private readonly IPlatformTenantRepository _repository;
    private readonly IPlatformSubscriptionPlanRepository _subscriptionPlanRepository;
    private readonly IPlatformPermissionChecker _permissionChecker;
    private readonly IPlatformPermissionRepository _permissionRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IPasswordHashService _passwordHashService;

    public PlatformTenantService(
        IPlatformTenantRepository repository,
        IPlatformSubscriptionPlanRepository subscriptionPlanRepository,
        IPlatformPermissionChecker permissionChecker,
        IPlatformPermissionRepository permissionRepository,
        IDateTimeProvider dateTimeProvider,
        IPasswordHashService passwordHashService)
    {
        _repository = repository;
        _subscriptionPlanRepository = subscriptionPlanRepository;
        _permissionChecker = permissionChecker;
        _permissionRepository = permissionRepository;
        _dateTimeProvider = dateTimeProvider;
        _passwordHashService = passwordHashService;
    }

    public async Task<ApplicationResult<PlatformTenantListResponse>> GetTenantsAsync(
        PlatformTenantListQuery query,
        Guid platformUserId,
        CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(platformUserId, PlatformPermissionCodes.TenantsView, cancellationToken))
        {
            return ApplicationResult<PlatformTenantListResponse>.Failure(AccessDenied);
        }

        ArgumentNullException.ThrowIfNull(query);
        NormalizeQuery(query);

        var response = await _repository.GetTenantsAsync(query, cancellationToken);
        return ApplicationResult<PlatformTenantListResponse>.Success(response);
    }

    public async Task<ApplicationResult<PlatformTenantSummaryResponse>> GetSummaryAsync(
        Guid platformUserId,
        CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(platformUserId, PlatformPermissionCodes.TenantsView, cancellationToken))
        {
            return ApplicationResult<PlatformTenantSummaryResponse>.Failure(AccessDenied);
        }

        var response = await _repository.GetSummaryAsync(cancellationToken);
        return ApplicationResult<PlatformTenantSummaryResponse>.Success(response);
    }

    public async Task<ApplicationResult<PlatformTenantFilterOptionsResponse>> GetFilterOptionsAsync(
        Guid platformUserId,
        CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(platformUserId, PlatformPermissionCodes.TenantsView, cancellationToken))
        {
            return ApplicationResult<PlatformTenantFilterOptionsResponse>.Failure(AccessDenied);
        }

        var response = await _repository.GetFilterOptionsAsync(cancellationToken);
        return ApplicationResult<PlatformTenantFilterOptionsResponse>.Success(response);
    }

    public async Task<ApplicationResult<PlatformTenantDetailResponse>> GetTenantDetailAsync(
        Guid tenantId,
        Guid platformUserId,
        CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(platformUserId, PlatformPermissionCodes.TenantsView, cancellationToken))
        {
            return ApplicationResult<PlatformTenantDetailResponse>.Failure(AccessDenied);
        }

        return await LoadTenantDetailAsync(tenantId, platformUserId, cancellationToken);
    }

    public async Task<ApplicationResult<PlatformTenantDetailResponse>> CreateTenantAsync(
        CreatePlatformTenantRequest request,
        Guid platformUserId,
        CancellationToken cancellationToken)
    {
        return await CreateTenantInternalAsync(request, platformUserId, cancellationToken);
    }

    public async Task<ApplicationResult<PlatformTenantDetailResponse>> UpdateTenantAsync(
        Guid tenantId,
        UpdatePlatformTenantRequest request,
        Guid platformUserId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!await HasPermissionAsync(platformUserId, PlatformPermissionCodes.TenantsUpdate, cancellationToken))
        {
            return ApplicationResult<PlatformTenantDetailResponse>.Failure(AccessDenied);
        }

        var tenant = await _repository.GetTenantEntityByIdAsync(tenantId, cancellationToken);
        if (tenant is null)
        {
            return ApplicationResult<PlatformTenantDetailResponse>.Failure(NotFound);
        }

        var name = NormalizeRequiredText(request.Name);
        if (string.IsNullOrWhiteSpace(name))
        {
            return ApplicationResult<PlatformTenantDetailResponse>.Failure(
                ValidationFailed with { Message = "Tenant name is required." });
        }

        tenant.UpdateDetails(
            name,
            NormalizeOptionalText(request.DefaultTimezone) ?? tenant.DefaultTimezone,
            null, // dataRegion
            platformUserId,
            _dateTimeProvider.UtcNow);

        await _repository.UpdateTenantAsync(tenant, cancellationToken);
        return await LoadTenantDetailAsync(tenantId, platformUserId, cancellationToken);
    }

    public async Task<ApplicationResult<PlatformTenantDetailResponse>> ActivateTenantAsync(
        Guid tenantId,
        Guid platformUserId,
        CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(platformUserId, PlatformPermissionCodes.TenantsActivate, cancellationToken))
        {
            return ApplicationResult<PlatformTenantDetailResponse>.Failure(AccessDenied);
        }

        var tenant = await _repository.GetTenantEntityByIdAsync(tenantId, cancellationToken);
        if (tenant is null)
        {
            return ApplicationResult<PlatformTenantDetailResponse>.Failure(NotFound);
        }

        if (!TenantLifecycleRules.CanActivate(tenant.Status))
        {
            return ApplicationResult<PlatformTenantDetailResponse>.Failure(
                InvalidTransition with { Message = "Tenant cannot be activated from its current status." });
        }

        if (string.IsNullOrWhiteSpace(tenant.DisplayName) || string.IsNullOrWhiteSpace(tenant.TenantCode))
        {
            return ApplicationResult<PlatformTenantDetailResponse>.Failure(
                ValidationFailed with { Message = "Tenant is missing required activation fields." });
        }

        var subscription = await _repository.GetCurrentTenantSubscriptionEntityAsync(tenantId, cancellationToken);
        if (subscription is null)
        {
            return ApplicationResult<PlatformTenantDetailResponse>.Failure(
                ValidationFailed with { Message = "Tenant subscription is required before activation." });
        }

        var now = _dateTimeProvider.UtcNow;
        tenant.Activate(platformUserId, now);
        subscription.Activate(now);

        await _repository.UpdateTenantAsync(tenant, cancellationToken);
        await _repository.UpdateTenantSubscriptionAsync(subscription, cancellationToken);

        return await LoadTenantDetailAsync(tenantId, platformUserId, cancellationToken);
    }

    public async Task<ApplicationResult<PlatformTenantDetailResponse>> SuspendTenantAsync(
        Guid tenantId,
        Guid platformUserId,
        CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(platformUserId, PlatformPermissionCodes.TenantsSuspend, cancellationToken))
        {
            return ApplicationResult<PlatformTenantDetailResponse>.Failure(AccessDenied);
        }

        var tenant = await _repository.GetTenantEntityByIdAsync(tenantId, cancellationToken);
        if (tenant is null)
        {
            return ApplicationResult<PlatformTenantDetailResponse>.Failure(NotFound);
        }

        var subscription = await _repository.GetCurrentTenantSubscriptionEntityAsync(tenantId, cancellationToken);
        if (!TenantLifecycleRules.CanSuspend(tenant.Status, subscription?.SubscriptionStatus))
        {
            return ApplicationResult<PlatformTenantDetailResponse>.Failure(
                InvalidTransition with { Message = "Tenant cannot be suspended from its current status." });
        }

        tenant.Suspend(platformUserId, _dateTimeProvider.UtcNow);
        await _repository.UpdateTenantAsync(tenant, cancellationToken);

        return await LoadTenantDetailAsync(tenantId, platformUserId, cancellationToken);
    }

    public async Task<ApplicationResult<PlatformTenantDetailResponse>> UpdateEntitlementsAsync(
        Guid tenantId,
        UpdatePlatformTenantEntitlementsRequest request,
        Guid platformUserId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!await HasPermissionAsync(
                platformUserId,
                PlatformPermissionCodes.TenantsEntitlementsUpdate,
                cancellationToken))
        {
            return ApplicationResult<PlatformTenantDetailResponse>.Failure(AccessDenied);
        }

        var tenant = await _repository.GetTenantEntityByIdAsync(tenantId, cancellationToken);
        if (tenant is null)
        {
            return ApplicationResult<PlatformTenantDetailResponse>.Failure(NotFound);
        }

        var subscription = await _repository.GetCurrentTenantSubscriptionEntityAsync(tenantId, cancellationToken);
        if (subscription is null)
        {
            return ApplicationResult<PlatformTenantDetailResponse>.Failure(
                ValidationFailed with { Message = "Tenant subscription was not found." });
        }

        var planId = subscription.SubscriptionPlanId;
        if (request.SubscriptionPlanId is not null && request.SubscriptionPlanId != Guid.Empty)
        {
            var plan = await _subscriptionPlanRepository.GetPlanEntityByIdAsync(
                request.SubscriptionPlanId.Value,
                cancellationToken);

            if (plan is null || !IsActivePlan(plan.Status))
            {
                return ApplicationResult<PlatformTenantDetailResponse>.Failure(
                    ValidationFailed with { Message = "Subscription plan was not found or is not active." });
            }

            planId = plan.Id;
        }

        var featureResolution = await ResolveEnabledFeaturesForPlanAsync(
            planId,
            request.EnabledFeatureIds,
            request.EnabledFeatureCodes,
            cancellationToken);

        if (featureResolution.IsFailure)
        {
            return ApplicationResult<PlatformTenantDetailResponse>.Failure(featureResolution.Error);
        }

        var now = _dateTimeProvider.UtcNow;

        if (planId != subscription.SubscriptionPlanId)
        {
            subscription.ChangePlan(planId, now);
            await _repository.UpdateTenantSubscriptionAsync(subscription, cancellationToken);
        }

        await _repository.ReplaceTenantEntitlementsAsync(
            tenantId,
            featureResolution.Value!,
            now,
            cancellationToken);

        return await LoadTenantDetailAsync(tenantId, platformUserId, cancellationToken);
    }

    private async Task<ApplicationResult<PlatformTenantDetailResponse>> LoadTenantDetailAsync(
        Guid tenantId,
        Guid platformUserId,
        CancellationToken cancellationToken)
    {
        var detail = await _repository.GetTenantDetailAsync(tenantId, cancellationToken);
        if (detail is null)
        {
            return ApplicationResult<PlatformTenantDetailResponse>.Failure(NotFound);
        }

        var permissions = await _permissionRepository.GetActivePermissionCodesAsync(
            platformUserId,
            cancellationToken);

        return ApplicationResult<PlatformTenantDetailResponse>.Success(
            PlatformTenantDetailMapper.ApplyActionFlags(detail, permissions));
    }

    private async Task<ApplicationResult<IReadOnlyList<Guid>>> ResolveEnabledFeaturesForPlanAsync(
        Guid planId,
        IReadOnlyList<Guid>? featureIds,
        IReadOnlyList<string>? featureCodes,
        CancellationToken cancellationToken)
    {
        var hasFeatureIds = featureIds?.Any(id => id != Guid.Empty) == true;
        var hasFeatureCodes = featureCodes?.Any(code => !string.IsNullOrWhiteSpace(code)) == true;

        if (!hasFeatureIds && !hasFeatureCodes)
        {
            return ApplicationResult<IReadOnlyList<Guid>>.Success([]);
        }

        var resolvedFeatures = await _repository.ResolveActiveFeaturesAsync(
            featureIds,
            featureCodes,
            cancellationToken);

        var requestedFeatureIds = featureIds?.Where(id => id != Guid.Empty).Distinct().ToList() ?? [];
        var requestedFeatureCodes = featureCodes?
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Select(code => code.Trim().ToLowerInvariant())
            .Distinct(StringComparer.Ordinal)
            .ToList() ?? [];

        var unknownFeatureIds = requestedFeatureIds
            .Except(resolvedFeatures.Select(feature => feature.Id))
            .ToList();

        var unknownFeatureCodes = requestedFeatureCodes
            .Except(resolvedFeatures.Select(feature => feature.FeatureCode.ToLowerInvariant()), StringComparer.Ordinal)
            .ToList();

        if (unknownFeatureIds.Count > 0 || unknownFeatureCodes.Count > 0)
        {
            var unknownParts = new List<string>();
            if (unknownFeatureIds.Count > 0)
            {
                unknownParts.Add($"featureIds: {string.Join(", ", unknownFeatureIds)}");
            }

            if (unknownFeatureCodes.Count > 0)
            {
                unknownParts.Add($"featureCodes: {string.Join(", ", unknownFeatureCodes)}");
            }

            return ApplicationResult<IReadOnlyList<Guid>>.Failure(
                ValidationFailed with
                {
                    Message = $"Unknown platform features ({string.Join("; ", unknownParts)})."
                });
        }

        var planFeatureIds = await _repository.GetIncludedFeatureIdsForPlanAsync(planId, cancellationToken);
        var disallowedFeatures = resolvedFeatures
            .Where(feature => !planFeatureIds.Contains(feature.Id))
            .Select(feature => feature.FeatureCode)
            .ToList();

        if (disallowedFeatures.Count > 0)
        {
            return ApplicationResult<IReadOnlyList<Guid>>.Failure(
                ValidationFailed with
                {
                    Message =
                        $"Features are not included in the selected subscription plan: {string.Join(", ", disallowedFeatures)}."
                });
        }

        return ApplicationResult<IReadOnlyList<Guid>>.Success(
            resolvedFeatures.Select(feature => feature.Id).ToList());
    }

    private async Task<bool> HasPermissionAsync(
        Guid platformUserId,
        string permissionCode,
        CancellationToken cancellationToken)
    {
        return await _permissionChecker.HasPermissionAsync(platformUserId, permissionCode, cancellationToken);
    }

    private static bool IsActivePlan(string status) =>
        string.Equals(status, SubscriptionPlanConstants.Status.Active, StringComparison.OrdinalIgnoreCase);

    private static void NormalizeQuery(PlatformTenantListQuery query)
    {
        query.PageNumber = query.PageNumber < 1 ? 1 : query.PageNumber;
        query.PageSize = query.PageSize < 1 ? DefaultPageSize : Math.Min(query.PageSize, MaxPageSize);
        query.SortDirection = string.Equals(query.SortDirection, "desc", StringComparison.OrdinalIgnoreCase)
            ? "desc"
            : "asc";
    }

    private static string NormalizeRequiredText(string? value) => (value ?? string.Empty).Trim();

    private static string? NormalizeOptionalText(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static string NormalizeBillingStatus(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return TenantBillingStatusConstants.Pending;
        }

        return value.Trim().ToLowerInvariant();
    }
}


