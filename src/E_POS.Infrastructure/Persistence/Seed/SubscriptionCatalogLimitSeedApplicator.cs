using E_POS.Domain.Modules.Platform.Subscription.Constants;
using E_POS.Domain.Modules.Platform.Subscription.Entities;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace E_POS.Infrastructure.Persistence.Seed;

public static class SubscriptionCatalogLimitSeedApplicator
{
    public sealed class MissingRequiredFeatureException : Exception
    {
        public MissingRequiredFeatureException(string featureCode)
            : base($"Required platform feature '{featureCode}' was not found. Seed platform module catalog before limit definitions.")
        {
            FeatureCode = featureCode;
        }

        public string FeatureCode { get; }
    }

    public static async Task ApplyAsync(
        EPosDbContext dbContext,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        var outletFeature = await RequireFeatureAsync(
            dbContext,
            SubscriptionCatalogLimitSeedConstants.OutletManagementFeatureCode,
            cancellationToken);

        var userFeature = await RequireFeatureAsync(
            dbContext,
            SubscriptionCatalogLimitSeedConstants.UserAccountsFeatureCode,
            cancellationToken);

        var tillFeature = await RequireFeatureAsync(
            dbContext,
            SubscriptionCatalogLimitSeedConstants.TillManagementFeatureCode,
            cancellationToken);

        await UpsertLimitDefinitionAsync(
            dbContext,
            SubscriptionCatalogLimitSeedConstants.MaxOutletsLimitDefinitionId,
            outletFeature.Id,
            SubscriptionCatalogLimitSeedConstants.MaxOutletsLimitKey,
            "Maximum Outlets",
            "outlet",
            now,
            cancellationToken);

        await UpsertLimitDefinitionAsync(
            dbContext,
            SubscriptionCatalogLimitSeedConstants.MaxUsersLimitDefinitionId,
            userFeature.Id,
            SubscriptionCatalogLimitSeedConstants.MaxUsersLimitKey,
            "Maximum Users",
            "user",
            now,
            cancellationToken);

        await UpsertLimitDefinitionAsync(
            dbContext,
            SubscriptionCatalogLimitSeedConstants.MaxTillsLimitDefinitionId,
            tillFeature.Id,
            SubscriptionCatalogLimitSeedConstants.MaxTillsLimitKey,
            "Maximum Tills",
            "till",
            now,
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        await SeedAddonIncrementsIfAddonsExistAsync(dbContext, now, cancellationToken);
    }

    private static async Task<PlatformFeature> RequireFeatureAsync(
        EPosDbContext dbContext,
        string featureCode,
        CancellationToken cancellationToken)
    {
        var feature = await dbContext.PlatformFeatures
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.FeatureCode == featureCode, cancellationToken);

        if (feature is null)
        {
            throw new MissingRequiredFeatureException(featureCode);
        }

        return feature;
    }

    private static async Task UpsertLimitDefinitionAsync(
        EPosDbContext dbContext,
        Guid id,
        Guid platformFeatureId,
        string limitKey,
        string limitName,
        string unitCode,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var existing = await dbContext.FeatureLimitDefinitions
            .FirstOrDefaultAsync(
                item =>
                    item.Id == id ||
                    (item.PlatformFeatureId == platformFeatureId && item.LimitKey == limitKey),
                cancellationToken);

        if (existing is null)
        {
            dbContext.FeatureLimitDefinitions.Add(FeatureLimitDefinition.Create(
                id,
                platformFeatureId,
                limitKey,
                limitName,
                defaultLimitValue: null,
                now,
                unitCode: unitCode));
        }
    }

    private static async Task SeedAddonIncrementsIfAddonsExistAsync(
        EPosDbContext dbContext,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var addonLimitMappings = new (string AddonCode, Guid LimitDefinitionId, Guid SeedId)[]
        {
            (
                SubscriptionCatalogLimitSeedConstants.ExtraOutletAddonCode,
                SubscriptionCatalogLimitSeedConstants.MaxOutletsLimitDefinitionId,
                Guid.Parse("74000000-0000-0000-0000-000000000001")),
            (
                SubscriptionCatalogLimitSeedConstants.ExtraUserAddonCode,
                SubscriptionCatalogLimitSeedConstants.MaxUsersLimitDefinitionId,
                Guid.Parse("74000000-0000-0000-0000-000000000002")),
            (
                SubscriptionCatalogLimitSeedConstants.ExtraTillAddonCode,
                SubscriptionCatalogLimitSeedConstants.MaxTillsLimitDefinitionId,
                Guid.Parse("74000000-0000-0000-0000-000000000003"))
        };

        foreach (var mapping in addonLimitMappings)
        {
            var addon = await dbContext.SubscriptionAddons
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.AddonCode == mapping.AddonCode, cancellationToken);

            if (addon is null)
            {
                continue;
            }

            var existing = await dbContext.SubscriptionAddonLimits
                .FirstOrDefaultAsync(
                    item =>
                        item.SubscriptionAddonId == addon.Id &&
                        item.FeatureLimitDefinitionId == mapping.LimitDefinitionId,
                    cancellationToken);

            if (existing is null)
            {
                dbContext.SubscriptionAddonLimits.Add(SubscriptionAddonLimit.Create(
                    mapping.SeedId,
                    addon.Id,
                    mapping.LimitDefinitionId,
                    incrementValue: 1m,
                    now));
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
