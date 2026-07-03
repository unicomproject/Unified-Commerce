using E_POS.Application.Common.Models;
using E_POS.Application.Modules.PlatformAdministration.Dtos;
using E_POS.Application.Modules.PlatformAdministration.Validators;
using E_POS.Domain.Modules.AccessControl.Constants;
using E_POS.Domain.Modules.AccessControl.Entities;
using E_POS.Domain.Modules.AuthSecurity.Entities;
using E_POS.Domain.Modules.PlatformAdministration.Constants;
using E_POS.Domain.Modules.SubscriptionBilling.Constants;
using E_POS.Domain.Modules.SubscriptionBilling.Entities;
using E_POS.Domain.Modules.TenantFoundation.Constants;
using E_POS.Domain.Modules.TenantFoundation.Entities;

namespace E_POS.Application.Modules.PlatformAdministration.Services;

public sealed partial class PlatformTenantService
{
    public async Task<ApplicationResult<PlatformTenantCreateOptionsResponse>> GetCreateOptionsAsync(
        Guid platformUserId,
        CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(platformUserId, PlatformPermissionCodes.TenantsCreate, cancellationToken))
        {
            return ApplicationResult<PlatformTenantCreateOptionsResponse>.Failure(AccessDenied);
        }

        var response = await _repository.GetCreateOptionsAsync(cancellationToken);
        return ApplicationResult<PlatformTenantCreateOptionsResponse>.Success(response);
    }

    private async Task<ApplicationResult<PlatformTenantDetailResponse>> CreateTenantInternalAsync(
        CreatePlatformTenantRequest request,
        Guid platformUserId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!await HasPermissionAsync(platformUserId, PlatformPermissionCodes.TenantsCreate, cancellationToken))
        {
            return ApplicationResult<PlatformTenantDetailResponse>.Failure(AccessDenied);
        }

        if (!IsWizardRequest(request))
        {
            return await CreateLegacyTenantAsync(request, platformUserId, cancellationToken);
        }

        var wizardValidationError = PlatformTenantCreateRequestValidator.ValidateWizard(request);
        if (wizardValidationError is not null)
        {
            return ApplicationResult<PlatformTenantDetailResponse>.Failure(wizardValidationError);
        }

        var code = NormalizeRequiredText(request.Code);
        var name = NormalizeRequiredText(request.Name);

        if (string.IsNullOrWhiteSpace(code))
        {
            return ApplicationResult<PlatformTenantDetailResponse>.Failure(
                ValidationFailed with { Message = "Tenant code is required." });
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return ApplicationResult<PlatformTenantDetailResponse>.Failure(
                ValidationFailed with { Message = "Tenant name is required." });
        }

        if (request.SubscriptionPlanId is null || request.SubscriptionPlanId == Guid.Empty)
        {
            return ApplicationResult<PlatformTenantDetailResponse>.Failure(
                ValidationFailed with { Message = "Subscription plan is required." });
        }

        if (await _repository.TenantCodeExistsAsync(code, cancellationToken))
        {
            return ApplicationResult<PlatformTenantDetailResponse>.Failure(
                Conflict with { Message = "A tenant with this code already exists." });
        }

        var plan = await _subscriptionPlanRepository.GetPlanEntityByIdAsync(
            request.SubscriptionPlanId.Value,
            cancellationToken);

        if (plan is null || !IsActivePlan(plan.Status))
        {
            return ApplicationResult<PlatformTenantDetailResponse>.Failure(
                ValidationFailed with { Message = "Subscription plan was not found or is not active." });
        }

        var billingStatus = NormalizeBillingStatus(request.BillingStatus);
        if (!AllowedBillingStatuses.Contains(billingStatus))
        {
            return ApplicationResult<PlatformTenantDetailResponse>.Failure(
                ValidationFailed with { Message = "Invalid tenant billing status." });
        }

        var tenantAdmin = request.TenantAdmin;
        if (tenantAdmin is null)
        {
            return ApplicationResult<PlatformTenantDetailResponse>.Failure(
                ValidationFailed with { Message = "Tenant admin details are required for wizard create." });
        }

        var adminFirstName = NormalizeRequiredText(tenantAdmin.FirstName);
        var adminEmail = NormalizeRequiredText(tenantAdmin.Email);
        if (string.IsNullOrWhiteSpace(adminFirstName) || string.IsNullOrWhiteSpace(adminEmail))
        {
            return ApplicationResult<PlatformTenantDetailResponse>.Failure(
                ValidationFailed with { Message = "Tenant admin first name and email are required." });
        }

        if (await _repository.TenantUserEmailExistsAsync(adminEmail, cancellationToken))
        {
            return ApplicationResult<PlatformTenantDetailResponse>.Failure(
                Conflict with { Message = "A tenant user with this email already exists." });
        }

        var createOptions = await _repository.GetCreateOptionsAsync(cancellationToken);
        var addonMap = createOptions.Addons.ToDictionary(addon => addon.Id);

        var addonSelections = (request.Addons ?? [])
            .Where(x => x.AddonId != Guid.Empty && x.Quantity > 0)
            .GroupBy(x => x.AddonId)
            .Select(group => new ResolvedAddonSelection(group.Key, group.Sum(x => x.Quantity)))
            .ToList();

        var unknownAddonIds = addonSelections
            .Where(selection => !addonMap.ContainsKey(selection.AddonId))
            .Select(selection => selection.AddonId)
            .ToList();
        if (unknownAddonIds.Count > 0)
        {
            return ApplicationResult<PlatformTenantDetailResponse>.Failure(
                ValidationFailed with
                {
                    Message = $"Unknown addon ids: {string.Join(", ", unknownAddonIds)}."
                });
        }

        var requestedFeatureIds = request.EnabledFeatureIds?.Where(id => id != Guid.Empty).ToList() ?? [];
        var requestedFeatureCodes = request.EnabledFeatureCodes?
            .Where(codeValue => !string.IsNullOrWhiteSpace(codeValue))
            .ToList() ?? [];

        IReadOnlyList<Guid> resolvedFeatureIds;
        if (requestedFeatureIds.Count == 0 && requestedFeatureCodes.Count == 0)
        {
            resolvedFeatureIds = (await _repository.GetIncludedFeatureIdsForPlanAsync(
                request.SubscriptionPlanId.Value,
                cancellationToken)).ToList();
        }
        else
        {
            var featureResolution = await ResolveEnabledFeaturesForPlanAsync(
                request.SubscriptionPlanId.Value,
                request.EnabledFeatureIds,
                request.EnabledFeatureCodes,
                cancellationToken);

            if (featureResolution.IsFailure)
            {
                return ApplicationResult<PlatformTenantDetailResponse>.Failure(featureResolution.Error);
            }

            resolvedFeatureIds = featureResolution.Value ?? [];
        }

        var computedMaxOutlets = CalculateEffectiveLimit(plan.MaxOutlets, addonSelections, addonMap, "max_outlets");
        var computedMaxTills = CalculateEffectiveLimit(plan.MaxTills, addonSelections, addonMap, "max_tills");
        var computedMaxUsers = CalculateEffectiveLimit(plan.MaxUsers, addonSelections, addonMap, "max_users");

        var requestedMaxOutlets = request.Limits?.MaxOutlets;
        var requestedMaxTills = request.Limits?.MaxTills;
        var requestedMaxUsers = request.Limits?.MaxUsers;

        if (!IsLimitOverrideValid(requestedMaxOutlets, computedMaxOutlets) ||
            !IsLimitOverrideValid(requestedMaxTills, computedMaxTills) ||
            !IsLimitOverrideValid(requestedMaxUsers, computedMaxUsers))
        {
            return ApplicationResult<PlatformTenantDetailResponse>.Failure(
                ValidationFailed with
                {
                    Message = "Requested tenant limits exceed the selected plan and addons."
                });
        }

        var maxOutlets = requestedMaxOutlets ?? computedMaxOutlets;
        var maxTills = requestedMaxTills ?? computedMaxTills;
        var maxUsers = requestedMaxUsers ?? computedMaxUsers;

        var now = _dateTimeProvider.UtcNow;
        var tenantId = Guid.NewGuid();
        var tenant = Tenant.CreateDraft(
            tenantId,
            code,
            name,
            billingStatus,
            NormalizeOptionalText(request.BaseCurrency) ?? DefaultBaseCurrency,
            NormalizeOptionalText(request.DefaultTimezone) ?? DefaultTimezone,
            NormalizeOptionalText(request.DefaultLocale) ?? DefaultLocale,
            NormalizeOptionalText(request.OperatingMode) ?? DefaultOperatingMode,
            request.BusinessType,
            businessTypeId: null,
            now);

        var profile = CreateTenantProfileOrNull(tenantId, request, now);
        var address = CreateTenantAddressOrNull(tenantId, request, now);

        var subscriptionRequest = request.Subscription;
        var billingCycle = NormalizeBillingCycle(subscriptionRequest?.BillingCycle);
        var subscriptionStatus = NormalizeSubscriptionStatus(subscriptionRequest?.SubscriptionStatus);

        var subscriptionId = Guid.NewGuid();
        var subscription = TenantSubscription.Create(
            subscriptionId,
            tenantId,
            plan.Id,
            subscriptionStatus,
            billingCycle,
            subscriptionRequest?.TrialStartAt,
            subscriptionRequest?.TrialEndAt,
            subscriptionRequest?.BillingStartAt,
            subscriptionRequest?.NextBillingAt,
            subscriptionRequest?.AutoRenew ?? true,
            subscriptionRequest?.DiscountType,
            subscriptionRequest?.DiscountValue,
            subscriptionRequest?.TaxPercentage ?? 0m,
            NormalizeOptionalText(subscriptionRequest?.InvoiceEmail) ?? NormalizeOptionalText(adminEmail),
            subscriptionRequest?.PaymentMethod,
            subscriptionRequest?.Notes,
            maxOutlets,
            maxTills,
            maxUsers,
            now);

        var entitlements = resolvedFeatureIds
            .Distinct()
            .Select(featureId => TenantFeatureEntitlement.Create(
                Guid.NewGuid(),
                tenantId,
                featureId,
                TenantEntitlementStatusConstants.Enabled,
                now))
            .ToList();

        var subscriptionAddons = addonSelections
            .Select(selection => TenantSubscriptionAddon.Create(
                Guid.NewGuid(),
                subscriptionId,
                selection.AddonId,
                selection.Quantity,
                status: "ACTIVE",
                now))
            .ToList();

        var roleId = Guid.NewGuid();
        var tenantAdminRole = TenantRole.Create(
            roleId,
            tenantId,
            TenantUserConstants.DefaultTenantAdminRoleCode,
            "Tenant Administrator",
            "Bootstrap tenant admin role",
            status: "ACTIVE",
            TenantBootstrapConstants.DefaultRoleTemplateVersionId,
            now);

        var bootstrapPermissionIds = await _repository.GetTenantAdminBootstrapPermissionIdsAsync(cancellationToken);
        var rolePermissions = bootstrapPermissionIds
            .Distinct()
            .Select(permissionId => TenantRolePermission.Create(
                Guid.NewGuid(),
                roleId,
                permissionId,
                description: "Wizard bootstrap permission",
                now))
            .ToList();

        if (!tenantAdmin.SendInvite)
        {
            return ApplicationResult<PlatformTenantDetailResponse>.Failure(
                ValidationFailed with
                {
                    Message = "Temporary password provisioning is deferred. Create a pending tenant admin invite instead."
                });
        }

        var normalizedAdminPhone = NormalizeOptionalText(tenantAdmin.Phone);
        var tenantAdminUser = TenantUser.CreatePendingInvite(
            Guid.NewGuid(),
            tenantId,
            adminEmail,
            adminFirstName,
            tenantAdmin.LastName,
            normalizedAdminPhone,
            now);

        var tenantAdminUserRole = TenantUserRole.Create(
            Guid.NewGuid(),
            tenantAdminUser.Id,
            roleId,
            "Bootstrap tenant admin role assignment",
            now);

        var invite = UserInvite.CreatePending(
            Guid.NewGuid(),
            tenantId,
            tenantAdminUser.Id,
            now);

        var shouldCreateDraftInvoice = subscriptionRequest?.CreateDraftInvoice == true ||
                                       string.Equals(billingStatus, TenantBillingStatusConstants.Pending, StringComparison.OrdinalIgnoreCase);

        SubscriptionInvoice? draftInvoice = null;
        if (shouldCreateDraftInvoice)
        {
            var addonsTotal = addonSelections.Sum(selection => addonMap[selection.AddonId].UnitPrice * selection.Quantity);
            var invoiceAmount = Math.Max(0m, plan.PriceAmount + addonsTotal);
            draftInvoice = SubscriptionInvoice.CreateDraft(
                Guid.NewGuid(),
                tenantId,
                subscriptionId,
                GenerateDraftInvoiceNumber(now),
                invoiceAmount,
                billingCycle,
                subscriptionRequest?.NextBillingAt ?? subscriptionRequest?.BillingStartAt ?? now.AddDays(7),
                now);
        }

        var writeModel = new PlatformTenantCreateWriteModel
        {
            Tenant = tenant,
            Profile = profile,
            Address = address,
            Subscription = subscription,
            Entitlements = entitlements,
            SubscriptionAddons = subscriptionAddons,
            TenantAdminRole = tenantAdminRole,
            TenantAdminRolePermissions = rolePermissions,
            TenantAdminUser = tenantAdminUser,
            TenantAdminUserRole = tenantAdminUserRole,
            TenantAdminInvite = invite,
            DraftInvoice = draftInvoice
        };

        await _repository.CreateTenantWizardAsync(writeModel, cancellationToken);
        return await LoadTenantDetailAsync(tenantId, platformUserId, cancellationToken);
    }

    private async Task<ApplicationResult<PlatformTenantDetailResponse>> CreateLegacyTenantAsync(
        CreatePlatformTenantRequest request,
        Guid platformUserId,
        CancellationToken cancellationToken)
    {
        var code = NormalizeRequiredText(request.Code);
        var name = NormalizeRequiredText(request.Name);

        if (string.IsNullOrWhiteSpace(code))
        {
            return ApplicationResult<PlatformTenantDetailResponse>.Failure(
                ValidationFailed with { Message = "Tenant code is required." });
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return ApplicationResult<PlatformTenantDetailResponse>.Failure(
                ValidationFailed with { Message = "Tenant name is required." });
        }

        if (request.SubscriptionPlanId is null || request.SubscriptionPlanId == Guid.Empty)
        {
            return ApplicationResult<PlatformTenantDetailResponse>.Failure(
                ValidationFailed with { Message = "Subscription plan is required." });
        }

        if (await _repository.TenantCodeExistsAsync(code, cancellationToken))
        {
            return ApplicationResult<PlatformTenantDetailResponse>.Failure(
                Conflict with { Message = "A tenant with this code already exists." });
        }

        var plan = await _subscriptionPlanRepository.GetPlanEntityByIdAsync(
            request.SubscriptionPlanId.Value,
            cancellationToken);

        if (plan is null || !IsActivePlan(plan.Status))
        {
            return ApplicationResult<PlatformTenantDetailResponse>.Failure(
                ValidationFailed with { Message = "Subscription plan was not found or is not active." });
        }

        var featureResolution = await ResolveEnabledFeaturesForPlanAsync(
            request.SubscriptionPlanId.Value,
            request.EnabledFeatureIds,
            request.EnabledFeatureCodes,
            cancellationToken);

        if (featureResolution.IsFailure)
        {
            return ApplicationResult<PlatformTenantDetailResponse>.Failure(featureResolution.Error);
        }

        var billingStatus = NormalizeBillingStatus(request.BillingStatus);
        if (!AllowedBillingStatuses.Contains(billingStatus))
        {
            return ApplicationResult<PlatformTenantDetailResponse>.Failure(
                ValidationFailed with { Message = "Invalid tenant billing status." });
        }

        var now = _dateTimeProvider.UtcNow;
        var tenantId = Guid.NewGuid();
        var tenant = Tenant.CreateDraft(
            tenantId,
            code,
            name,
            billingStatus,
            NormalizeOptionalText(request.BaseCurrency) ?? DefaultBaseCurrency,
            NormalizeOptionalText(request.DefaultTimezone) ?? DefaultTimezone,
            NormalizeOptionalText(request.DefaultLocale) ?? DefaultLocale,
            NormalizeOptionalText(request.OperatingMode) ?? DefaultOperatingMode,
            request.BusinessType,
            businessTypeId: null,
            now);

        var subscription = TenantSubscription.Create(
            Guid.NewGuid(),
            tenantId,
            request.SubscriptionPlanId.Value,
            TenantSubscriptionStatusConstants.Trial,
            now);

        await _repository.AddTenantWithSubscriptionAndEntitlementsAsync(
            tenant,
            subscription,
            featureResolution.Value!,
            now,
            cancellationToken);

        return await LoadTenantDetailAsync(tenantId, platformUserId, cancellationToken);
    }

    private static int? CalculateEffectiveLimit(
        int? planLimit,
        IReadOnlyList<ResolvedAddonSelection> selections,
        IReadOnlyDictionary<Guid, PlatformTenantCreateAddonOptionDto> addonMap,
        string limitKey)
    {
        if (!planLimit.HasValue)
        {
            return null;
        }

        var increment = selections.Sum(selection =>
        {
            if (!addonMap.TryGetValue(selection.AddonId, out var addon))
            {
                return 0;
            }

            if (!addon.LimitIncrementByKey.TryGetValue(limitKey, out var addonIncrement))
            {
                return 0;
            }

            return addonIncrement * selection.Quantity;
        });

        return planLimit.Value + increment;
    }

    private static bool IsLimitOverrideValid(int? requested, int? computed)
    {
        if (requested is null)
        {
            return true;
        }

        if (requested < 0)
        {
            return false;
        }

        return computed is null || requested <= computed;
    }

    private static bool IsWizardRequest(CreatePlatformTenantRequest request)
    {
        return request.TenantAdmin is not null ||
               request.Subscription is not null ||
               (request.Addons?.Count > 0) ||
               request.Address is not null ||
               request.PrimaryContact is not null ||
               !string.IsNullOrWhiteSpace(request.LegalName) ||
               !string.IsNullOrWhiteSpace(request.RegistrationNumber) ||
               !string.IsNullOrWhiteSpace(request.TaxNumber);
    }

    private static TenantProfile? CreateTenantProfileOrNull(
        Guid tenantId,
        CreatePlatformTenantRequest request,
        DateTimeOffset now)
    {
        var legalName = NormalizeOptionalText(request.LegalName);
        var registrationNumber = NormalizeOptionalText(request.RegistrationNumber);
        var taxNumber = NormalizeOptionalText(request.TaxNumber);
        var contactName = NormalizeOptionalText(request.PrimaryContact?.Name);
        var contactEmail = NormalizeOptionalText(request.PrimaryContact?.Email);
        var contactPhone = NormalizeOptionalText(request.PrimaryContact?.Phone);
        var countryCode = NormalizeOptionalText(request.CountryCode);

        var hasProfileData = !string.IsNullOrWhiteSpace(legalName) ||
                             !string.IsNullOrWhiteSpace(registrationNumber) ||
                             !string.IsNullOrWhiteSpace(taxNumber) ||
                             !string.IsNullOrWhiteSpace(contactName) ||
                             !string.IsNullOrWhiteSpace(contactEmail) ||
                             !string.IsNullOrWhiteSpace(contactPhone) ||
                             !string.IsNullOrWhiteSpace(countryCode);

        if (!hasProfileData)
        {
            return null;
        }

        return TenantProfile.Create(
            Guid.NewGuid(),
            tenantId,
            legalName,
            registrationNumber,
            taxNumber,
            contactName,
            contactEmail,
            contactPhone,
            websiteUrl: null,
            countryCode,
            now);
    }

    private static TenantAddress? CreateTenantAddressOrNull(
        Guid tenantId,
        CreatePlatformTenantRequest request,
        DateTimeOffset now)
    {
        var address = request.Address;
        if (address is null)
        {
            return null;
        }

        var hasAddressData =
            !string.IsNullOrWhiteSpace(address.Line1) ||
            !string.IsNullOrWhiteSpace(address.Line2) ||
            !string.IsNullOrWhiteSpace(address.City) ||
            !string.IsNullOrWhiteSpace(address.State) ||
            !string.IsNullOrWhiteSpace(address.PostalCode) ||
            !string.IsNullOrWhiteSpace(address.CountryCode);

        if (!hasAddressData)
        {
            return null;
        }

        return TenantAddress.CreateRegistered(
            Guid.NewGuid(),
            tenantId,
            address.Line1,
            address.Line2,
            address.City,
            address.State,
            address.PostalCode,
            address.CountryCode,
            now);
    }

    private static string NormalizeSubscriptionStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return TenantSubscriptionStatusConstants.Trial;
        }

        var normalized = status.Trim().ToUpperInvariant();
        return normalized switch
        {
            "TRIAL" => TenantSubscriptionStatusConstants.Trial,
            "ACTIVE" => TenantSubscriptionStatusConstants.Active,
            "PAST_DUE" => TenantSubscriptionStatusConstants.PastDue,
            "CANCELLED" => TenantSubscriptionStatusConstants.Cancelled,
            "EXPIRED" => TenantSubscriptionStatusConstants.Expired,
            _ => TenantSubscriptionStatusConstants.Trial
        };
    }

    private static string NormalizeBillingCycle(string? billingCycle)
    {
        if (string.IsNullOrWhiteSpace(billingCycle))
        {
            return TenantSubscriptionBillingConstants.BillingCycleMonthly;
        }

        var normalized = billingCycle.Trim().ToLowerInvariant();
        return normalized switch
        {
            TenantSubscriptionBillingConstants.BillingCycleMonthly => TenantSubscriptionBillingConstants.BillingCycleMonthly,
            TenantSubscriptionBillingConstants.BillingCycleYearly => TenantSubscriptionBillingConstants.BillingCycleYearly,
            _ => TenantSubscriptionBillingConstants.BillingCycleMonthly
        };
    }

    private static string GenerateDraftInvoiceNumber(DateTimeOffset now) =>
        $"INV-{now:yyyyMMddHHmmss}-{Guid.NewGuid():N}"[..32];

    private sealed record ResolvedAddonSelection(Guid AddonId, int Quantity);
}
