using E_POS.Application.Common.Contracts;
using E_POS.Application.Modules.Platform.Subscription.Contracts;
using E_POS.Domain.Modules.Platform.Subscription.Constants;
using E_POS.Domain.Modules.Platform.Subscription.Entities;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace E_POS.Infrastructure.Modules.Platform.Subscription.Services;

/// <summary>
/// Resolves effective subscription limits.
/// Hierarchy: explicit finite Max*Override → plan Max* + active add-on increments →
/// plan Max* null means unlimited. Null Max*Override means "no override" (not unlimited).
/// </summary>
public sealed class TenantSubscriptionLimitResolver : ITenantSubscriptionLimitResolver
{
    private readonly EPosDbContext _dbContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<TenantSubscriptionLimitResolver> _logger;

    public TenantSubscriptionLimitResolver(
        EPosDbContext dbContext,
        IDateTimeProvider dateTimeProvider,
        ILogger<TenantSubscriptionLimitResolver> logger)
    {
        _dbContext = dbContext;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task<TenantSubscriptionLimitResolution> ResolveAsync(
        Guid tenantId,
        string limitKey,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!TenantSubscriptionLimitKeys.TryGet(limitKey, out var definition))
            {
                return TenantSubscriptionLimitResolution.ConfigurationFailure(
                    limitKey?.Trim() ?? string.Empty,
                    "unknown",
                    SubscriptionLimitErrorCodes.UnknownKey,
                    $"Unknown subscription limit key '{limitKey}'.");
            }

            if (definition.Status != RuntimeEnforcementStatus.Enforced ||
                !definition.FeatureLimitDefinitionId.HasValue)
            {
                return TenantSubscriptionLimitResolution.ConfigurationFailure(
                    definition.LimitKey,
                    definition.Resource,
                    SubscriptionLimitErrorCodes.NotEnforced,
                    $"Subscription limit '{definition.LimitKey}' is not enabled for runtime enforcement.");
            }

            var subscription = await _dbContext.TenantSubscriptions
                .AsNoTracking()
                .Where(item =>
                    item.TenantId == tenantId &&
                    (item.SubscriptionStatus == TenantSubscriptionStatusConstants.Active ||
                     item.Status == TenantSubscriptionStatusConstants.Active))
                .OrderByDescending(item => item.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (subscription is null)
            {
                _logger.LogWarning(
                    "Subscription limit configuration missing: no active subscription. TenantId={TenantId} LimitKey={LimitKey}",
                    tenantId,
                    definition.LimitKey);

                return TenantSubscriptionLimitResolution.ConfigurationFailure(
                    definition.LimitKey,
                    definition.Resource,
                    SubscriptionLimitErrorCodes.ConfigurationMissing,
                    "Active tenant subscription is required to evaluate capacity limits.");
            }

            var plan = await _dbContext.SubscriptionPlans
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == subscription.SubscriptionPlanId, cancellationToken);

            if (plan is null)
            {
                return TenantSubscriptionLimitResolution.ConfigurationFailure(
                    definition.LimitKey,
                    definition.Resource,
                    SubscriptionLimitErrorCodes.ConfigurationMissing,
                    "Subscription plan is missing for the active tenant subscription.");
            }

            var planBaseline = await ResolvePlanBaselineAsync(
                plan,
                subscription.Id,
                definition.LimitKey,
                cancellationToken);

            if (planBaseline is < 0)
            {
                return TenantSubscriptionLimitResolution.ConfigurationFailure(
                    definition.LimitKey,
                    definition.Resource,
                    SubscriptionLimitErrorCodes.Invalid,
                    $"Plan limit for '{definition.LimitKey}' is invalid.");
            }

            var (overrideLimit, overrideConfigured) = ReadTenantOverride(subscription, definition.LimitKey);
            if (overrideConfigured && overrideLimit is < 0)
            {
                return TenantSubscriptionLimitResolution.ConfigurationFailure(
                    definition.LimitKey,
                    definition.Resource,
                    SubscriptionLimitErrorCodes.Invalid,
                    $"Tenant override for '{definition.LimitKey}' is invalid.");
            }

            // Canonical: valid explicit override → override; else plan + add-ons.
            // Null Max*Override = no override (fall back). Plan Max* null = unlimited baseline.
            int? effective;
            var overrideApplied = false;
            if (overrideConfigured)
            {
                effective = overrideLimit;
                overrideApplied = true;
                _logger.LogInformation(
                    "Subscription limit override applied. TenantId={TenantId} LimitKey={LimitKey} Override={Override} PlanBaseline={PlanBaseline}",
                    tenantId,
                    definition.LimitKey,
                    overrideLimit,
                    planBaseline);
            }
            else
            {
                effective = planBaseline;
                if (planBaseline.HasValue)
                {
                    _logger.LogInformation(
                        "Subscription limit plan fallback used. TenantId={TenantId} LimitKey={LimitKey} PlanBaseline={PlanBaseline}",
                        tenantId,
                        definition.LimitKey,
                        planBaseline);
                }
                else
                {
                    _logger.LogInformation(
                        "Subscription limit explicit unlimited plan used. TenantId={TenantId} LimitKey={LimitKey}",
                        tenantId,
                        definition.LimitKey);
                }
            }

            var isUnlimited = !effective.HasValue;

            return new TenantSubscriptionLimitResolution(
                definition.LimitKey,
                definition.Resource,
                planBaseline,
                overrideConfigured ? overrideLimit : null,
                effective,
                isUnlimited,
                overrideApplied,
                true,
                null,
                null);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Subscription limit evaluation failed. TenantId={TenantId} LimitKey={LimitKey}",
                tenantId,
                limitKey);

            return TenantSubscriptionLimitResolution.ConfigurationFailure(
                limitKey?.Trim() ?? string.Empty,
                "unknown",
                SubscriptionLimitErrorCodes.EvaluationFailed,
                "Subscription limit evaluation failed.");
        }
    }

    private async Task<int?> ResolvePlanBaselineAsync(
        SubscriptionPlan plan,
        Guid tenantSubscriptionId,
        string limitKey,
        CancellationToken cancellationToken)
    {
        var planLimit = limitKey switch
        {
            TenantSubscriptionLimitKeys.MaxOutlets => plan.MaxOutlets,
            TenantSubscriptionLimitKeys.MaxTills => plan.MaxTills,
            TenantSubscriptionLimitKeys.MaxUsers => plan.MaxUsers,
            _ => null
        };

        // null plan Max* = explicit unlimited baseline (approved representation).
        // Schema cannot distinguish "missing Max*" from "unlimited Max*"; null is the approved unlimited sentinel.
        if (!planLimit.HasValue)
        {
            return null;
        }

        var definitionId = limitKey switch
        {
            TenantSubscriptionLimitKeys.MaxOutlets => SubscriptionCatalogLimitSeedConstants.MaxOutletsLimitDefinitionId,
            TenantSubscriptionLimitKeys.MaxTills => SubscriptionCatalogLimitSeedConstants.MaxTillsLimitDefinitionId,
            TenantSubscriptionLimitKeys.MaxUsers => SubscriptionCatalogLimitSeedConstants.MaxUsersLimitDefinitionId,
            _ => Guid.Empty
        };

        var now = _dateTimeProvider.UtcNow;
        var activeStatus = SubscriptionCatalogConstants.RecordStatus.Active;
        var addonIncrement = await (
            from tenantAddon in _dbContext.TenantSubscriptionAddons.AsNoTracking()
            join addonLimit in _dbContext.SubscriptionAddonLimits.AsNoTracking()
                on tenantAddon.SubscriptionAddonId equals addonLimit.SubscriptionAddonId
            where tenantAddon.TenantSubscriptionId == tenantSubscriptionId &&
                  addonLimit.FeatureLimitDefinitionId == definitionId &&
                  tenantAddon.Status == activeStatus &&
                  tenantAddon.StartsAt <= now &&
                  (tenantAddon.EndsAt == null || tenantAddon.EndsAt > now)
            select addonLimit.IncrementValue * (tenantAddon.Quantity <= 0 ? 1 : tenantAddon.Quantity)
        ).SumAsync(cancellationToken);

        var increment = (int)Math.Truncate(addonIncrement);
        if (increment != 0)
        {
            _logger.LogInformation(
                "Subscription limit add-on capacity applied. TenantSubscriptionId={TenantSubscriptionId} LimitKey={LimitKey} PlanLimit={PlanLimit} AddonIncrement={AddonIncrement}",
                tenantSubscriptionId,
                limitKey,
                planLimit.Value,
                increment);
        }

        return planLimit.Value + increment;
    }

    /// <summary>
    /// Null Max*Override means no tenant-specific override (fall back to plan).
    /// A HasValue override is an explicit finite capacity (including zero).
    /// Schema v1 cannot express an explicit unlimited override distinct from null;
    /// unlimited comes from a null plan Max* baseline only.
    /// </summary>
    private static (int? Value, bool Configured) ReadTenantOverride(TenantSubscription subscription, string limitKey)
    {
        return limitKey switch
        {
            TenantSubscriptionLimitKeys.MaxOutlets =>
                (subscription.MaxOutletsOverride, subscription.MaxOutletsOverride.HasValue),
            TenantSubscriptionLimitKeys.MaxTills =>
                (subscription.MaxTillsOverride, subscription.MaxTillsOverride.HasValue),
            TenantSubscriptionLimitKeys.MaxUsers =>
                (subscription.MaxUsersOverride, subscription.MaxUsersOverride.HasValue),
            _ => (null, false)
        };
    }
}
