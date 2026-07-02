using E_POS.Application.Modules.PlatformAdministration.Dtos;
using E_POS.Domain.Modules.AccessControl.Entities;
using E_POS.Domain.Modules.SubscriptionBilling.Constants;
using E_POS.Domain.Modules.TenantFoundation.Constants;
using Microsoft.EntityFrameworkCore;

namespace E_POS.Infrastructure.Modules.PlatformAdministration.Repositories;

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
                on addonFeature.Id equals addonLimit.SubscriptionAddonFeatureId into limitJoin
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
                RelatedFeatureCode = feature != null ? feature.FeatureCode : null,
                LimitCode = limitDefinition != null ? limitDefinition.LimitCode : null,
                addonLimit.LimitValue
            })
            .ToListAsync(cancellationToken);

        var addons = addonRows
            .GroupBy(row => new
            {
                row.Id,
                row.AddonCode,
                row.Name,
                row.Description,
                row.PriceAmount
            })
            .Select(group =>
            {
                var increments = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                foreach (var row in group)
                {
                    var normalizedKey = NormalizeLimitKey(row.LimitCode);
                    if (normalizedKey is null || row.LimitValue is null)
                    {
                        continue;
                    }

                    increments[normalizedKey] = row.LimitValue.Value;
                }

                return new PlatformTenantCreateAddonOptionDto(
                    group.Key.Id,
                    group.Key.AddonCode,
                    group.Key.Name,
                    group.Key.Description,
                    group.Key.PriceAmount,
                    "LKR",
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
                $"{currency.CurrencyCode} - {currency.Name}"))
            .ToListAsync(cancellationToken);

        var businessTypes = await _dbContext.BusinessTypes
            .AsNoTracking()
            .Where(type => type.Status == "ACTIVE")
            .OrderBy(type => type.Name)
            .Select(type => new PlatformTenantCreateLookupOptionDto(type.BusinessTypeCode, type.Name))
            .ToListAsync(cancellationToken);

        var billingModes = new[]
        {
            TenantBillingStatusConstants.Pending,
            TenantBillingStatusConstants.Paid,
            TenantBillingStatusConstants.Overdue,
            TenantBillingStatusConstants.Failed,
            TenantBillingStatusConstants.Waived
        }
            .Select(value => new PlatformTenantCreateLookupOptionDto(value, ToLookupLabel(value)))
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

        return new PlatformTenantCreateOptionsResponse(
            plans,
            addons,
            catalogModules,
            billingModes,
            currencies,
            timezones,
            locales,
            businessTypes,
            operatingModes,
            subscriptionStatuses,
            billingCycles);
    }

    public Task<bool> TenantUserEmailExistsAsync(string email, CancellationToken cancellationToken)
    {
        var normalizedEmail = TenantUser.NormalizeEmail(email);
        return _dbContext.TenantUsers
            .AsNoTracking()
            .AnyAsync(user => user.NormalizedEmail == normalizedEmail, cancellationToken);
    }

    public async Task<IReadOnlyList<Guid>> GetTenantAdminBootstrapPermissionIdsAsync(CancellationToken cancellationToken)
    {
        var bootstrapCodes = TenantCreateWizardReferenceData.TenantAdminBootstrapPermissionCodes
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return await _dbContext.PermissionDefinitions
            .AsNoTracking()
            .Where(permission =>
                permission.Status == "ACTIVE" &&
                bootstrapCodes.Contains(permission.PermissionCode))
            .Select(permission => permission.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task CreateTenantWizardAsync(PlatformTenantCreateWriteModel model, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(model);

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            _dbContext.Tenants.Add(model.Tenant);
            if (model.Profile is not null)
            {
                _dbContext.TenantProfiles.Add(model.Profile);
            }

            if (model.Address is not null)
            {
                _dbContext.TenantAddresses.Add(model.Address);
            }

            _dbContext.TenantSubscriptions.Add(model.Subscription);

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
