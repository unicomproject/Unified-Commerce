using E_POS.Domain.Modules.Platform.Subscription.Entities;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace E_POS.Infrastructure.Persistence.Seed;

public static class SubscriptionBillingCatalogSeedApplicator
{
    public static async Task ApplyAsync(EPosDbContext dbContext, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        var module = await dbContext.PlatformModules
            .FirstOrDefaultAsync(x => x.ModuleCode == "core_commerce", cancellationToken);

        if (module is null)
        {
            module = PlatformModule.Create(
                SubscriptionBillingCatalogSeedConstants.CoreCommerceModuleId,
                "core_commerce",
                "Core Commerce",
                "Core TM-EPOS commercial capabilities.",
                "ACTIVE",
                1,
                now);

            dbContext.PlatformModules.Add(module);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        await UpsertFeatureAsync(
            dbContext,
            SubscriptionBillingCatalogSeedConstants.OnlineStoreFeatureId,
            module.Id,
            "online_store",
            "Online Store",
            "Enable tenant online store channel.",
            1,
            now,
            cancellationToken);

        await UpsertFeatureAsync(
            dbContext,
            SubscriptionBillingCatalogSeedConstants.ClickCollectFeatureId,
            module.Id,
            "click_collect",
            "Click & Collect",
            "Enable click and collect ordering.",
            2,
            now,
            cancellationToken);

        await UpsertFeatureAsync(
            dbContext,
            SubscriptionBillingCatalogSeedConstants.OfflineSyncFeatureId,
            module.Id,
            "offline_operation_sync",
            "Offline Operation Sync",
            "Enable offline operation synchronization.",
            3,
            now,
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task UpsertFeatureAsync(
        EPosDbContext dbContext,
        Guid featureId,
        Guid moduleId,
        string featureCode,
        string name,
        string description,
        int sortOrder,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var feature = await dbContext.PlatformFeatures
            .FirstOrDefaultAsync(x => x.FeatureCode == featureCode, cancellationToken);

        if (feature is null)
        {
            dbContext.PlatformFeatures.Add(PlatformFeature.Create(
                featureId,
                moduleId,
                featureCode,
                name,
                "ACTIVE",
                now,
                sortOrder));
        }
    }
}

