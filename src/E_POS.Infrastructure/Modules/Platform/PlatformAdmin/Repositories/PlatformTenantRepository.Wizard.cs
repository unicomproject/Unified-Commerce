using System.Text.Json;
using E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;
using E_POS.Domain.Modules.Platform.Subscription.Entities;
using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using E_POS.Domain.Modules.Platform.Subscription.Constants;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Constants;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using Microsoft.EntityFrameworkCore;
using E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;
using E_POS.Domain.Modules.Platform.PlatformAdmin.Entities;
using E_POS.Domain.Modules.Platform.PlatformAdmin.Constants;

namespace E_POS.Infrastructure.Modules.Platform.PlatformAdmin.Repositories;

public sealed partial class PlatformTenantRepository
{
    public async Task<PlatformTenantCreateOptionsResponse> GetCreateOptionsAsync(CancellationToken cancellationToken)
    {
        var activePlans = await _dbContext.SubscriptionPlans
            .AsNoTracking()
            .Where(plan => plan.Status == "active")
            .OrderBy(plan => plan.Name)
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
                plan.MaxTills,
                plan.MaxUsers
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

        var plans = activePlans
            .Select(plan =>
            {
                includedFeaturesByPlan.TryGetValue(plan.Id, out var planFeatures);
                return new PlatformTenantCreatePlanOptionDto(
                    plan.Id,
                    plan.PlanCode,
                    plan.Name,
                    plan.Description,
                    plan.Status,
                    plan.BillingInterval,
                    plan.BaseCurrency,
                    plan.PriceAmount,
                    plan.MaxOutlets,
                    plan.MaxTills,
                    plan.MaxUsers,
                    planFeatures?.FeatureIds ?? [],
                    planFeatures?.FeatureCodes ?? []);
            })
            .ToList();

        var addonRows = await (
            from addon in _dbContext.SubscriptionAddons.AsNoTracking()
            where addon.Status == "ACTIVE"
            join addonFeature in _dbContext.SubscriptionAddonFeatures.AsNoTracking()
                on addon.Id equals addonFeature.SubscriptionAddonId into addonFeatureJoin
            from addonFeature in addonFeatureJoin.DefaultIfEmpty()
            join feature in _dbContext.PlatformFeatures.AsNoTracking()
                on addonFeature.PlatformFeatureId equals feature.Id into featureJoin
            from feature in featureJoin.DefaultIfEmpty()
            join addonLimit in _dbContext.SubscriptionAddonLimits.AsNoTracking()
                on addon.Id equals addonLimit.SubscriptionAddonId into limitJoin
            from addonLimit in limitJoin.DefaultIfEmpty()
            join limitDefinition in _dbContext.FeatureLimitDefinitions.AsNoTracking()
                on addonLimit.FeatureLimitDefinitionId equals limitDefinition.Id into definitionJoin
            from limitDefinition in definitionJoin.DefaultIfEmpty()
            select new
            {
                addon.Id,
                addon.AddonCode,
                addon.Name,
                addon.Description,
                addon.PriceAmount,
                addon.BaseCurrencyCode,
                RelatedFeatureCode = feature != null ? feature.FeatureCode : null,
                LimitCode = limitDefinition != null ? limitDefinition.LimitCode : null,
                IncrementValue = addonLimit != null ? (decimal?)addonLimit.IncrementValue : null
            })
            .ToListAsync(cancellationToken);

        var addons = addonRows
            .GroupBy(row => new
            {
                row.Id,
                row.AddonCode,
                row.Name,
                row.Description,
                row.PriceAmount,
                row.BaseCurrencyCode
            })
            .Select(group =>
            {
                var increments = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                foreach (var row in group)
                {
                    var normalizedKey = NormalizeLimitKey(row.LimitCode);
                    if (normalizedKey is null ||
                        !row.IncrementValue.HasValue ||
                        row.IncrementValue.Value <= 0m)
                    {
                        continue;
                    }

                    increments[normalizedKey] = (int)row.IncrementValue.Value;
                }

                return new PlatformTenantCreateAddonOptionDto(
                    group.Key.Id,
                    group.Key.AddonCode,
                    group.Key.Name,
                    group.Key.Description,
                    group.Key.PriceAmount,
                    group.Key.BaseCurrencyCode,
                    group.Select(x => x.RelatedFeatureCode).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)),
                    increments);
            })
            .OrderBy(addon => addon.Name)
            .ToList();

        var catalogModules = await _dbContext.PlatformModules
            .AsNoTracking()
            .Where(module => module.Status == "ACTIVE")
            .OrderBy(module => module.SortOrder)
            .ThenBy(module => module.Name)
            .Select(module => new PlatformTenantCreateCatalogModuleDto(
                module.Id,
                module.ModuleCode,
                module.Name,
                module.Description,
                module.SortOrder,
                _dbContext.PlatformFeatures
                    .Where(feature => feature.PlatformModuleId == module.Id && feature.Status == "ACTIVE")
                    .OrderBy(feature => feature.SortOrder)
                    .ThenBy(feature => feature.Name)
                    .Select(feature => new PlatformTenantCreateCatalogFeatureDto(
                        feature.Id,
                        feature.FeatureCode,
                        feature.Name,
                        feature.Description,
                        feature.SortOrder))
                    .ToList()))
            .ToListAsync(cancellationToken);

        var currencies = await _dbContext.Currencies
            .AsNoTracking()
            .OrderBy(currency => currency.CurrencyCode)
            .Select(currency => new PlatformTenantCreateLookupOptionDto(
                currency.CurrencyCode,
                $"{currency.CurrencyCode} - {currency.CurrencyName}"))
            .ToListAsync(cancellationToken);

        var businessTypes = await _dbContext.BusinessTypes
            .AsNoTracking()
            .Where(type => type.Status == "ACTIVE")
            .OrderBy(type => type.BusinessName)
            .Select(type => new PlatformTenantCreateLookupOptionDto(type.BusinessCode, type.BusinessName))
            .ToListAsync(cancellationToken);

        var billingStatuses = new[]
        {
            TenantBillingStatusConstants.Pending,
            TenantBillingStatusConstants.Paid,
            TenantBillingStatusConstants.Overdue,
            TenantBillingStatusConstants.Failed,
            TenantBillingStatusConstants.Waived
        }
            .Select(value => new PlatformTenantCreateLookupOptionDto(value, ToLookupLabel(value)))
            .ToList();

        var paymentMethods = TenantSubscriptionBillingConstants.PaymentMethods
            .Select(item => new PlatformTenantCreateLookupOptionDto(item.Value, item.Label))
            .ToList();

        var countryCodes = TenantCreateWizardReferenceData.CountryCodes
            .Select(item => new PlatformTenantCreateCountryOptionDto(item.Code, item.Name))
            .ToList();

        var timezones = TenantCreateWizardReferenceData.Timezones
            .Select(item => new PlatformTenantCreateLookupOptionDto(item.Value, item.Label))
            .ToList();

        var locales = TenantCreateWizardReferenceData.Locales
            .Select(item => new PlatformTenantCreateLookupOptionDto(item.Value, item.Label))
            .ToList();

        var operatingModes = TenantOperatingModeConstants.All
            .Select(value => new PlatformTenantCreateLookupOptionDto(value, ToLookupLabel(value)))
            .ToList();

        var subscriptionStatuses = new[]
        {
            TenantSubscriptionStatusConstants.Trial,
            TenantSubscriptionStatusConstants.Active,
            TenantSubscriptionStatusConstants.PastDue,
            TenantSubscriptionStatusConstants.Cancelled,
            TenantSubscriptionStatusConstants.Expired
        }
            .Select(value => new PlatformTenantCreateLookupOptionDto(value, ToLookupLabel(value)))
            .ToList();

        var billingCycles = TenantSubscriptionBillingConstants.BillingCycles
            .Select(value => new PlatformTenantCreateLookupOptionDto(value, ToLookupLabel(value)))
            .ToList();

        var settingRows = await _dbContext.PlatformSettings.AsNoTracking()
            .Where(x => PlatformSettingKeys.GeneralSettings.Contains(x.SettingKey))
            .ToListAsync(cancellationToken);
        var settingValues = settingRows.ToDictionary(x => x.SettingKey, x => x.GetStringValue());
        string? Setting(string key) => settingValues.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value.Trim()
            : null;
        var defaultBillingCycle = plans.Select(x => x.BillingCycle)
            .Distinct(StringComparer.OrdinalIgnoreCase).Count() == 1 ? plans[0].BillingCycle : null;

        return new PlatformTenantCreateOptionsResponse(
            plans,
            addons,
            catalogModules,
            billingStatuses,
            paymentMethods,
            countryCodes,
            currencies,
            timezones,
            locales,
            businessTypes,
            operatingModes,
            subscriptionStatuses,
            billingCycles,
            new PlatformTenantCreateDefaultsDto(
                Setting(PlatformSettingKeys.DefaultCountryCode),
                Setting(PlatformSettingKeys.DefaultCurrencyCode),
                Setting(PlatformSettingKeys.DefaultTimezone),
                Setting(PlatformSettingKeys.DefaultLocale),
                defaultBillingCycle),
            new PlatformTenantCreateValidationDto(
                "^[A-Z0-9-]{3,60}$",
                "^[a-z0-9](?:[a-z0-9-]{1,98}[a-z0-9])?$",
                30,
                null));
    }

    public Task<bool> TenantUserEmailExistsAsync(string email, CancellationToken cancellationToken)
    {
        var normalizedEmail = TenantUser.NormalizeEmail(email);
        return _dbContext.TenantUsers
            .AsNoTracking()
            .AnyAsync(user => user.Email == email, cancellationToken);
    }

    public async Task<IReadOnlyDictionary<string, Guid>> GetActivePermissionIdMapByCodesAsync(
        IReadOnlyList<string> permissionCodes,
        CancellationToken cancellationToken)
    {
        if (permissionCodes.Count == 0)
        {
            return new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        }

        var codes = permissionCodes
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Select(code => code.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (codes.Count == 0)
        {
            return new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        }

        var rows = await _dbContext.PermissionDefinitions
            .AsNoTracking()
            .Where(permission =>
                permission.IsActive &&
                codes.Contains(permission.PermissionCode))
            .Select(permission => new { permission.PermissionCode, permission.Id })
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(
            row => row.PermissionCode,
            row => row.Id,
            StringComparer.OrdinalIgnoreCase);
    }

    public async Task<Guid?> GetActiveBusinessTypeIdByCodeAsync(string businessCode, CancellationToken cancellationToken)
    {
        var normalized = businessCode.Trim();
        return await _dbContext.BusinessTypes
            .AsNoTracking()
            .Where(type => type.Status == "ACTIVE" && type.BusinessCode == normalized)
            .Select(type => (Guid?)type.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<TenantProfile?> GetTenantProfileEntityByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        return _dbContext.TenantProfiles
            .FirstOrDefaultAsync(profile => profile.TenantId == tenantId, cancellationToken);
    }

    public async Task UpsertTenantProfileAsync(TenantProfile profile, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(profile);

        var exists = await _dbContext.TenantProfiles
            .AnyAsync(existing => existing.Id == profile.Id, cancellationToken);

        if (!exists)
        {
            _dbContext.TenantProfiles.Add(profile);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task CreateTenantWizardAsync(PlatformTenantCreateWriteModel model, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(model);

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            PlatformTenantOnboardingDraft? onboardingDraft = null;
            if (model.OnboardingFinalizeContext is { } onboarding)
            {
                onboardingDraft = await _dbContext.PlatformTenantOnboardingDrafts
                    .FromSqlInterpolated($"SELECT * FROM platform_tenant_onboarding_drafts WHERE id = {onboarding.DraftId} FOR UPDATE")
                    .SingleOrDefaultAsync(cancellationToken)
                    ?? throw new TenantOnboardingConcurrencyException(new InvalidOperationException("Onboarding draft no longer exists."));

                if (onboardingDraft.Status == "completed")
                {
                    var sameRequest = string.Equals(onboardingDraft.FinalizeIdempotencyKeyHash, onboarding.IdempotencyKeyHash, StringComparison.Ordinal) &&
                                      string.Equals(onboardingDraft.FinalizeRequestHash, onboarding.RequestHash, StringComparison.Ordinal);
                    throw new TenantOnboardingAlreadyFinalizedException(sameRequest);
                }

                if (onboardingDraft.Version != onboarding.ExpectedDraftVersion || onboardingDraft.Status != "in_progress")
                {
                    throw new TenantOnboardingConcurrencyException(new InvalidOperationException("Onboarding draft version or state changed."));
                }

                onboardingDraft.BeginFinalization(onboarding.IdempotencyKeyHash, onboarding.RequestHash,
                    onboarding.ActorPlatformUserId, onboarding.RequestedAt);
            }

            _dbContext.Tenants.Add(model.Tenant);
            if (model.Profile is not null)
            {
                _dbContext.TenantProfiles.Add(model.Profile);
            }

            if (model.Address is not null)
            {
                _dbContext.TenantAddresses.Add(model.Address);
            }

            if (model.Domain is not null)
            {
                _dbContext.TenantDomains.Add(model.Domain);
            }

            _dbContext.TenantSubscriptions.Add(model.Subscription);

            _dbContext.TenantSubscriptionHistory.Add(TenantSubscriptionHistory.CreateEvent(
                Guid.NewGuid(),
                model.Tenant.Id,
                model.Subscription.Id,
                sequenceNumber: 1,
                changeType: TenantSubscriptionHistoryChangeTypeConstants.Created,
                changedAt: model.Subscription.CreatedAt,
                newPlanId: model.Subscription.SubscriptionPlanId,
                newStatus: model.Subscription.SubscriptionStatus));

            if (model.Entitlements.Count > 0)
            {
                _dbContext.TenantFeatureEntitlements.AddRange(model.Entitlements);
            }

            if (model.SubscriptionAddons.Count > 0)
            {
                _dbContext.TenantSubscriptionAddons.AddRange(model.SubscriptionAddons);
            }

            if (model.TenantAdminRole is not null)
            {
                _dbContext.TenantRoles.Add(model.TenantAdminRole);
            }

            if (model.TenantAdminRolePermissions.Count > 0)
            {
                _dbContext.TenantRolePermissions.AddRange(model.TenantAdminRolePermissions);
            }

            if (model.TenantAdminUser is not null)
            {
                _dbContext.TenantUsers.Add(model.TenantAdminUser);
            }

            if (model.TenantAdminUserRole is not null)
            {
                _dbContext.TenantUserRoles.Add(model.TenantAdminUserRole);
            }

            if (model.TenantAdminInvite is not null)
            {
                _dbContext.UserInvites.Add(model.TenantAdminInvite);
            }

            if (model.DraftInvoice is not null)
            {
                _dbContext.SubscriptionInvoices.Add(model.DraftInvoice);
                if (model.DraftInvoiceLines.Count > 0)
                {
                    _dbContext.SubscriptionInvoiceLines.AddRange(model.DraftInvoiceLines);
                }
            }

            if (model.ManualPayment is not null)
            {
                _dbContext.SubscriptionPaymentTransactions.Add(model.ManualPayment);
            }

            if (model.ManualPaymentAccess is not null)
            {
                _dbContext.SubscriptionPaymentLinks.Add(model.ManualPaymentAccess);
            }

            if (model.ManualPaymentCreatedHistory is not null)
            {
                _dbContext.SubscriptionPaymentReviews.Add(model.ManualPaymentCreatedHistory);
            }

            if (model.OnboardingFinalizeContext is { } finalization && onboardingDraft is not null)
            {
                if (model.OnboardingContacts.Count > 0)
                {
                    _dbContext.TenantContacts.AddRange(model.OnboardingContacts);
                }
                if (model.OnboardingOperation is not null)
                {
                    _dbContext.PlatformTenantOnboardingOperations.Add(model.OnboardingOperation);
                }
                if (model.OnboardingOutboxMessages.Count > 0)
                {
                    _dbContext.IntegrationOutboxMessages.AddRange(model.OnboardingOutboxMessages);
                }

                var definitions = await _dbContext.FeatureLimitDefinitions
                    .Where(x => x.Status == SubscriptionCatalogConstants.RecordStatus.Active &&
                                (x.Id == SubscriptionCatalogLimitSeedConstants.MaxOutletsLimitDefinitionId ||
                                 x.Id == SubscriptionCatalogLimitSeedConstants.MaxUsersLimitDefinitionId ||
                                 x.Id == SubscriptionCatalogLimitSeedConstants.MaxTillsLimitDefinitionId))
                    .ToListAsync(cancellationToken);
                var limits = new Dictionary<Guid, int?>
                {
                    [SubscriptionCatalogLimitSeedConstants.MaxOutletsLimitDefinitionId] = model.Subscription.MaxOutletsOverride,
                    [SubscriptionCatalogLimitSeedConstants.MaxUsersLimitDefinitionId] = model.Subscription.MaxUsersOverride,
                    [SubscriptionCatalogLimitSeedConstants.MaxTillsLimitDefinitionId] = model.Subscription.MaxTillsOverride
                };
                if (definitions.Count != 3)
                {
                    throw new InvalidOperationException("Canonical capacity limit definitions are missing or inactive.");
                }
                _dbContext.TenantUsageCounters.AddRange(definitions.Select(definition => TenantUsageCounter.Create(
                    Guid.NewGuid(), model.Tenant.Id, definition.Id, definition.PlatformFeatureId,
                    TenantUsageCounterAlignmentConstants.UsageScope.Tenant, null, 0m,
                    limits[definition.Id], model.Subscription.CurrentPeriodStart, model.Subscription.CurrentPeriodEnd,
                    finalization.RequestedAt)));

                _dbContext.TenantSubscriptionHistory.Add(TenantSubscriptionHistory.CreateEvent(
                    Guid.NewGuid(), model.Tenant.Id, model.Subscription.Id, 2, "tenant.created",
                    finalization.RequestedAt, newPlanId: model.Subscription.SubscriptionPlanId,
                    newStatus: model.Subscription.SubscriptionStatus, reason: "Tenant finalized from onboarding draft.",
                    changeData: BuildOnboardingChangeData(finalization.DraftId, finalization.OperationId),
                    changedByPlatformUserId: finalization.ActorPlatformUserId));
                onboardingDraft.Complete(model.Tenant.Id, finalization.ActorPlatformUserId, finalization.RequestedAt);
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    internal static string BuildOnboardingChangeData(Guid draftId, Guid operationId) =>
        JsonSerializer.Serialize(new { draftId, operationId });

    private static string? NormalizeLimitKey(string? limitCode)
    {
        if (string.IsNullOrWhiteSpace(limitCode))
        {
            return null;
        }

        var normalized = limitCode.Trim().ToUpperInvariant();
        if (normalized.Contains("OUTLET", StringComparison.Ordinal))
        {
            return "max_outlets";
        }

        if (normalized.Contains("TILL", StringComparison.Ordinal))
        {
            return "max_tills";
        }

        if (normalized.Contains("USER", StringComparison.Ordinal))
        {
            return "max_users";
        }

        return normalized.ToLowerInvariant() switch
        {
            "max_outlets" => "max_outlets",
            "max_tills" => "max_tills",
            "max_users" => "max_users",
            _ => null
        };
    }

    private static string ToLookupLabel(string value)
    {
        var spaced = value.Replace("_", " ", StringComparison.Ordinal);
        var words = spaced.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return string.Join(" ", words.Select(word =>
            char.ToUpperInvariant(word[0]) + word[1..].ToLowerInvariant()));
    }
}



