using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Common.Security;
using E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;
using E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;
using E_POS.Application.Modules.Platform.PlatformAdmin.Mappers;
using E_POS.Application.Modules.Platform.PlatformAdmin.Validators;
using E_POS.Application.Modules.Platform.Subscription.Contracts;
using E_POS.Application.Modules.Tenant.TenantFoundation.Contracts;
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
    private readonly ITenantUsageCounterService _tenantUsageCounterService;
    private readonly IDefaultTenantSettingsProvider _defaultTenantSettingsProvider;
    private readonly IInvitationTokenService? _invitationTokenService;
    private readonly ITenantAdminInvitationDeliveryService? _invitationDeliveryService;
    private readonly Microsoft.Extensions.Logging.ILogger<PlatformTenantService>? _logger;

    public PlatformTenantService(
        IPlatformTenantRepository repository,
        IPlatformSubscriptionPlanRepository subscriptionPlanRepository,
        IPlatformPermissionChecker permissionChecker,
        IPlatformPermissionRepository permissionRepository,
        IDateTimeProvider dateTimeProvider,
        IPasswordHashService passwordHashService,
        ITenantUsageCounterService tenantUsageCounterService,
        IDefaultTenantSettingsProvider defaultTenantSettingsProvider,
        IInvitationTokenService? invitationTokenService = null,
        ITenantAdminInvitationDeliveryService? invitationDeliveryService = null,
        Microsoft.Extensions.Logging.ILogger<PlatformTenantService>? logger = null)
    {
        _repository = repository;
        _subscriptionPlanRepository = subscriptionPlanRepository;
        _permissionChecker = permissionChecker;
        _permissionRepository = permissionRepository;
        _dateTimeProvider = dateTimeProvider;
        _passwordHashService = passwordHashService;
        _tenantUsageCounterService = tenantUsageCounterService;
        _defaultTenantSettingsProvider = defaultTenantSettingsProvider;
        _invitationTokenService = invitationTokenService;
        _invitationDeliveryService = invitationDeliveryService;
        _logger = logger;
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

        if (!await HasPermissionAsync(platformUserId, PlatformPermissionCodes.TenantSubscriptionsView, cancellationToken))
        {
            var redactedItems = response.Items.Select(item => item with { Subscription = null }).ToList();
            response = response with { Items = redactedItems };
        }

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

        if (!string.IsNullOrWhiteSpace(request.ConcurrencyVersion) &&
            !string.Equals(request.ConcurrencyVersion, (tenant.UpdatedAt ?? tenant.CreatedAt).Ticks.ToString(), StringComparison.Ordinal))
        {
            return ApplicationResult<PlatformTenantDetailResponse>.Failure(
                Conflict with { Message = "The tenant record was updated by another user. Please reload and try again." });
        }

        var name = NormalizeRequiredText(request.Name);
        if (string.IsNullOrWhiteSpace(name))
        {
            return ApplicationResult<PlatformTenantDetailResponse>.Failure(
                ValidationFailed with { Message = "Tenant name is required." });
        }

        var updateValidationError = PlatformTenantCreateRequestValidator.ValidateUpdate(request);
        if (updateValidationError is not null)
        {
            return ApplicationResult<PlatformTenantDetailResponse>.Failure(updateValidationError);
        }

        var now = _dateTimeProvider.UtcNow;
        tenant.UpdateDetails(
            name,
            NormalizeOptionalText(request.DefaultTimezone) ?? tenant.DefaultTimezone,
            null, // dataRegion
            platformUserId,
            now,
            request.DefaultLocale,
            request.OperatingMode,
            updateLocale: request.DefaultLocale is not null,
            updateOperatingMode: request.OperatingMode is not null);

        await _repository.UpdateTenantAsync(tenant, cancellationToken);

        if (request.BusinessType is not null)
        {
            var businessTypeResolution = await ResolveBusinessTypeIdAsync(request.BusinessType, cancellationToken);
            if (businessTypeResolution.IsFailure)
            {
                return ApplicationResult<PlatformTenantDetailResponse>.Failure(businessTypeResolution.Error);
            }

            var profile = await _repository.GetTenantProfileEntityByTenantIdAsync(tenantId, cancellationToken);
            if (profile is null && businessTypeResolution.Value.HasValue)
            {
                profile = TenantProfile.Create(
                    Guid.NewGuid(),
                    tenantId,
                    businessTypeResolution.Value,
                    name,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    platformUserId,
                    now);
                await _repository.UpsertTenantProfileAsync(profile, cancellationToken);
            }
            else if (profile is not null)
            {
                profile.UpdateBusinessType(businessTypeResolution.Value, platformUserId, now);
                await _repository.UpsertTenantProfileAsync(profile, cancellationToken);
            }
        }

        var subscription = await _repository.GetCurrentTenantSubscriptionEntityAsync(tenantId, cancellationToken);
        if (subscription is not null && request.BillingStatus is not null && !string.Equals(request.BillingStatus, subscription.SubscriptionStatus, StringComparison.OrdinalIgnoreCase))
        {
            await _repository.AddAuditLogAsync(tenantId, platformUserId, "tenant.billing_state_changed", $"Tenant billing status updated to {request.BillingStatus}.", null, now, cancellationToken);
        }

        await _repository.AddAuditLogAsync(tenantId, platformUserId, "tenant.profile_updated", "Tenant profile updated by platform admin.", null, now, cancellationToken);

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

        if (string.Equals(tenant.Status, TenantStatusConstants.Active, StringComparison.OrdinalIgnoreCase))
        {
            return await LoadTenantDetailAsync(tenantId, platformUserId, cancellationToken);
        }

        if (string.Equals(tenant.Status, TenantStatusConstants.Suspended, StringComparison.OrdinalIgnoreCase))
        {
            return ApplicationResult<PlatformTenantDetailResponse>.Failure(
                InvalidTransition with { Message = "Suspended tenants must be reactivated using the reactivate action." });
        }

        if (string.Equals(tenant.Status, TenantStatusConstants.Cancelled, StringComparison.OrdinalIgnoreCase))
        {
            return ApplicationResult<PlatformTenantDetailResponse>.Failure(
                InvalidTransition with { Message = "Cancelled tenants cannot be activated." });
        }

        if (string.Equals(tenant.Status, TenantStatusConstants.PendingPayment, StringComparison.OrdinalIgnoreCase))
        {
            return ApplicationResult<PlatformTenantDetailResponse>.Failure(
                InvalidTransition with
                {
                    Message = "Paid tenants cannot be activated before payment verification."
                });
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

        // Paid path: PENDING_ACTIVATION requires authoritative PAID invoice evidence (Mark Paid).
        // Draft remains activatable without payment for legacy/data-repair recovery only.
        if (string.Equals(tenant.Status, TenantStatusConstants.PendingActivation, StringComparison.OrdinalIgnoreCase))
        {
            var hasVerifiedPayment = await _repository.HasVerifiedPaidInvoiceAsync(tenantId, cancellationToken);
            if (!hasVerifiedPayment)
            {
                return ApplicationResult<PlatformTenantDetailResponse>.Failure(
                    InvalidTransition with
                    {
                        Message = "Paid tenants cannot be activated before payment verification."
                    });
            }
        }

        var activation = await _repository.ActivateTenantRuntimeAsync(
            tenantId, platformUserId, _dateTimeProvider.UtcNow, cancellationToken);
        if (activation.Outcome is PlatformTenantActivationRuntimeOutcome.Success or PlatformTenantActivationRuntimeOutcome.Replay)
            return await LoadTenantDetailAsync(tenantId, platformUserId, cancellationToken);
        return activation.Outcome switch
        {
            PlatformTenantActivationRuntimeOutcome.NotFound => ApplicationResult<PlatformTenantDetailResponse>.Failure(NotFound),
            PlatformTenantActivationRuntimeOutcome.PaymentNotVerified => ApplicationResult<PlatformTenantDetailResponse>.Failure(
                InvalidTransition with { Message = "Paid tenants cannot be activated before payment verification." }),
            PlatformTenantActivationRuntimeOutcome.SubscriptionMissing => ApplicationResult<PlatformTenantDetailResponse>.Failure(
                ValidationFailed with { Message = "Tenant subscription is required before activation." }),
            PlatformTenantActivationRuntimeOutcome.MembershipMissing => ApplicationResult<PlatformTenantDetailResponse>.Failure(
                ValidationFailed with { Message = "Tenant Admin membership and role are required before activation." }),
            PlatformTenantActivationRuntimeOutcome.EntitlementsNotReady => ApplicationResult<PlatformTenantDetailResponse>.Failure(
                ValidationFailed with { Message = "Tenant entitlements are not ready for activation." }),
            PlatformTenantActivationRuntimeOutcome.ConcurrencyConflict => ApplicationResult<PlatformTenantDetailResponse>.Failure(
                Conflict with { Message = "Tenant activation was changed by another request." }),
            _ => ApplicationResult<PlatformTenantDetailResponse>.Failure(
                InvalidTransition with { Message = "Tenant cannot be activated from its current status." })
        };
    }

    public async Task<ApplicationResult<PlatformTenantDetailResponse>> ReactivateTenantAsync(
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

        if (!string.Equals(tenant.Status, TenantStatusConstants.Suspended, StringComparison.OrdinalIgnoreCase))
        {
            return ApplicationResult<PlatformTenantDetailResponse>.Failure(
                InvalidTransition with { Message = "Only suspended tenants can be reactivated." });
        }

        var subscription = await _repository.GetCurrentTenantSubscriptionEntityAsync(tenantId, cancellationToken);
        var now = _dateTimeProvider.UtcNow;
        tenant.Activate(platformUserId, now);
        if (subscription is not null)
        {
            subscription.Activate(now);
            await _repository.UpdateTenantSubscriptionAsync(subscription, cancellationToken);
        }

        await _repository.UpdateTenantAsync(tenant, cancellationToken);
        await _repository.AddAuditLogAsync(tenantId, platformUserId, "tenant.reactivated", "Tenant reactivated by platform admin.", null, now, cancellationToken);

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

        var now = _dateTimeProvider.UtcNow;
        tenant.Suspend(platformUserId, now);
        await _repository.UpdateTenantAsync(tenant, cancellationToken);
        await _repository.AddAuditLogAsync(tenantId, platformUserId, "tenant.suspended", "Tenant suspended by platform admin.", null, now, cancellationToken);

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

        if (!string.IsNullOrWhiteSpace(request.ConcurrencyVersion) &&
            !string.Equals(request.ConcurrencyVersion, (tenant.UpdatedAt ?? tenant.CreatedAt).Ticks.ToString(), StringComparison.Ordinal))
        {
            return ApplicationResult<PlatformTenantDetailResponse>.Failure(
                Conflict with { Message = "The tenant record was updated by another user. Please reload and try again." });
        }

        var subscription = await _repository.GetCurrentTenantSubscriptionEntityAsync(tenantId, cancellationToken);
        if (subscription is null)
        {
            return ApplicationResult<PlatformTenantDetailResponse>.Failure(
                ValidationFailed with { Message = "Tenant subscription was not found." });
        }

        var sourceTypeInput = request.SourceType?.Trim().ToUpperInvariant();
        if (!string.IsNullOrWhiteSpace(sourceTypeInput) &&
            sourceTypeInput != TenantEntitlementSourceTypeConstants.Override &&
            sourceTypeInput != TenantEntitlementSourceTypeConstants.Manual)
        {
            return ApplicationResult<PlatformTenantDetailResponse>.Failure(
                ValidationFailed with { Message = "SourceType must be OVERRIDE or MANUAL." });
        }

        var resolvedSourceType = !string.IsNullOrWhiteSpace(sourceTypeInput)
            ? sourceTypeInput
            : (!string.IsNullOrWhiteSpace(request.OverrideReason)
                ? TenantEntitlementSourceTypeConstants.Override
                : TenantEntitlementSourceTypeConstants.Manual);

        if (string.Equals(resolvedSourceType, TenantEntitlementSourceTypeConstants.Override, StringComparison.Ordinal))
        {
            if (string.IsNullOrWhiteSpace(request.OverrideReason))
            {
                return ApplicationResult<PlatformTenantDetailResponse>.Failure(
                    ValidationFailed with { Message = "OverrideReason is required when sourceType is OVERRIDE." });
            }

            if (request.OverrideReason.Trim().Length > 500)
            {
                return ApplicationResult<PlatformTenantDetailResponse>.Failure(
                    ValidationFailed with { Message = "OverrideReason cannot exceed 500 characters." });
            }
        }

        if (request.EffectiveFrom.HasValue && request.EffectiveUntil.HasValue &&
            request.EffectiveUntil.Value <= request.EffectiveFrom.Value)
        {
            return ApplicationResult<PlatformTenantDetailResponse>.Failure(
                ValidationFailed with { Message = "EffectiveUntil must be greater than EffectiveFrom." });
        }

        var planId = subscription.SubscriptionPlanId;
        SubscriptionPlan? selectedPlan = null;
        if (request.SubscriptionPlanId is not null && request.SubscriptionPlanId != Guid.Empty)
        {
            selectedPlan = await _subscriptionPlanRepository.GetPlanEntityByIdAsync(
                request.SubscriptionPlanId.Value,
                cancellationToken);

            if (selectedPlan is null || !IsActivePlan(selectedPlan.Status))
            {
                return ApplicationResult<PlatformTenantDetailResponse>.Failure(
                    ValidationFailed with { Message = "Subscription plan was not found or is not active." });
            }

            planId = selectedPlan.Id;
        }

        var featureResolution = await ResolveEnabledFeaturesForPlanAsync(
            planId,
            request.EnabledFeatureIds,
            request.EnabledFeatureCodes,
            cancellationToken,
            allowCustomOverrides: true);

        if (featureResolution.IsFailure)
        {
            return ApplicationResult<PlatformTenantDetailResponse>.Failure(featureResolution.Error);
        }

        var now = _dateTimeProvider.UtcNow;

        if (planId != subscription.SubscriptionPlanId)
        {
            subscription.ChangePlan(
                planId,
                selectedPlan?.BaseCurrency,
                selectedPlan?.PriceAmount,
                now);
            await _repository.UpdateTenantSubscriptionAsync(subscription, cancellationToken);
            await _repository.AddAuditLogAsync(tenantId, platformUserId, "tenant.subscription_changed", $"Tenant subscription plan updated to {selectedPlan?.PlanName ?? planId.ToString()}.", null, now, cancellationToken);
        }

        var revokedReason = string.Equals(resolvedSourceType, TenantEntitlementSourceTypeConstants.Override, StringComparison.Ordinal)
            ? request.OverrideReason!.Trim()
            : "Removed by platform admin entitlement update.";

        await _repository.ReplaceTenantEntitlementsAsync(
            tenantId,
            featureResolution.Value!,
            now,
            platformUserId,
            revokedReason,
            resolvedSourceType,
            request.OverrideReason?.Trim(),
            request.EffectiveFrom,
            request.EffectiveUntil,
            cancellationToken);

        var auditDetail = string.Equals(resolvedSourceType, TenantEntitlementSourceTypeConstants.Override, StringComparison.Ordinal)
            ? $"Tenant entitlements updated with OVERRIDE: {request.OverrideReason!.Trim()}"
            : "Tenant entitlements updated by platform admin.";

        await _repository.AddAuditLogAsync(tenantId, platformUserId, "tenant.entitlements_updated", auditDetail, null, now, cancellationToken);

        return await LoadTenantDetailAsync(tenantId, platformUserId, cancellationToken);
    }

    public async Task<ApplicationResult<PlatformTenantDetailResponse>> RestoreEntitlementsToPlanAsync(
        Guid tenantId,
        Guid platformUserId,
        CancellationToken cancellationToken)
    {
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
                ValidationFailed with { Message = "Tenant subscription was not found. Cannot restore plan baseline without a valid subscription." });
        }

        var now = _dateTimeProvider.UtcNow;

        await _repository.RestoreTenantPlanEntitlementsAsync(
            tenantId,
            subscription.SubscriptionPlanId,
            now,
            platformUserId,
            cancellationToken);

        await _repository.AddAuditLogAsync(
            tenantId,
            platformUserId,
            "tenant.entitlements_restored_to_plan",
            "Tenant entitlements restored to subscription plan baseline.",
            null,
            now,
            cancellationToken);

        return await LoadTenantDetailAsync(tenantId, platformUserId, cancellationToken);
    }

    public async Task<ApplicationResult<PlatformTenantAuditLogListResponse>> GetTenantAuditLogsAsync(
        Guid tenantId,
        Guid platformUserId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(platformUserId, PlatformPermissionCodes.AuditView, cancellationToken))
        {
            return ApplicationResult<PlatformTenantAuditLogListResponse>.Failure(AccessDenied);
        }

        var tenant = await _repository.GetTenantEntityByIdAsync(tenantId, cancellationToken);
        if (tenant is null)
        {
            return ApplicationResult<PlatformTenantAuditLogListResponse>.Failure(NotFound);
        }

        var logs = await _repository.GetTenantAuditLogsAsync(tenantId, pageNumber, pageSize, cancellationToken);
        return ApplicationResult<PlatformTenantAuditLogListResponse>.Success(logs);
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

        var resultDetail = PlatformTenantDetailMapper.ApplyActionFlags(detail, permissions);
        if (!permissions.Contains(PlatformPermissionCodes.TenantSubscriptionsView))
        {
            resultDetail = resultDetail with { Subscription = null };
        }

        return ApplicationResult<PlatformTenantDetailResponse>.Success(resultDetail);
    }

    private async Task<ApplicationResult<IReadOnlyList<Guid>>> ResolveEnabledFeaturesForPlanAsync(
        Guid planId,
        IReadOnlyList<Guid>? featureIds,
        IReadOnlyList<string>? featureCodes,
        CancellationToken cancellationToken,
        bool allowCustomOverrides = false)
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

        if (!allowCustomOverrides)
        {
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


