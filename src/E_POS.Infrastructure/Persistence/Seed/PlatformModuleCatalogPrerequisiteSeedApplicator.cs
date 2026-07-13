using E_POS.Domain.Modules.Platform.PlatformAdmin.Constants;
using E_POS.Domain.Modules.Platform.Subscription.Entities;
using Microsoft.EntityFrameworkCore;

namespace E_POS.Infrastructure.Persistence.Seed;

public static class PlatformModuleCatalogPrerequisiteSeedApplicator
{
    public static async Task ApplyAsync(
        EPosDbContext dbContext,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        await UpsertModuleAsync(
            dbContext,
            PlatformModuleCatalogPrerequisiteSeedConstants.UserManagementModuleId,
            PlatformModuleCatalogPrerequisiteSeedConstants.UserManagementModuleCode,
            "User Management",
            sortOrder: 3,
            now,
            cancellationToken);

        await UpsertModuleAsync(
            dbContext,
            PlatformModuleCatalogPrerequisiteSeedConstants.OutletTillCoreModuleId,
            PlatformModuleCatalogPrerequisiteSeedConstants.OutletTillCoreModuleCode,
            "Outlet & Till Core",
            sortOrder: 9,
            now,
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        var userManagementModuleId = await GetModuleIdAsync(
            dbContext,
            PlatformModuleCatalogPrerequisiteSeedConstants.UserManagementModuleCode,
            cancellationToken);

        var outletTillCoreModuleId = await GetModuleIdAsync(
            dbContext,
            PlatformModuleCatalogPrerequisiteSeedConstants.OutletTillCoreModuleCode,
            cancellationToken);

        await UpsertFeatureAsync(
            dbContext,
            PlatformModuleCatalogPrerequisiteSeedConstants.UserAccountsFeatureId,
            userManagementModuleId,
            PlatformModuleCatalogPrerequisiteSeedConstants.UserAccountsFeatureCode,
            "User Accounts",
            sortOrder: 1,
            now,
            cancellationToken);

        await UpsertFeatureAsync(
            dbContext,
            PlatformModuleCatalogPrerequisiteSeedConstants.OutletManagementFeatureId,
            outletTillCoreModuleId,
            PlatformModuleCatalogPrerequisiteSeedConstants.OutletManagementFeatureCode,
            "Outlet Management",
            sortOrder: 1,
            now,
            cancellationToken);

        await UpsertFeatureAsync(
            dbContext,
            PlatformModuleCatalogPrerequisiteSeedConstants.TillManagementFeatureId,
            outletTillCoreModuleId,
            PlatformModuleCatalogPrerequisiteSeedConstants.TillManagementFeatureCode,
            "Till Management",
            sortOrder: 2,
            now,
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task UpsertModuleAsync(
        EPosDbContext dbContext,
        Guid moduleId,
        string moduleCode,
        string name,
        int sortOrder,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var module = await dbContext.PlatformModules
            .SingleOrDefaultAsync(item => item.ModuleCode == moduleCode, cancellationToken);

        if (module is null)
        {
            dbContext.PlatformModules.Add(PlatformModule.Create(
                moduleId,
                moduleCode,
                name,
                description: null,
                PlatformAuthConstants.ActiveStatus,
                sortOrder,
                now,
                isCoreModule: true));

            return;
        }

        dbContext.Entry(module).Property(nameof(PlatformModule.ModuleKey)).CurrentValue = moduleCode;
        dbContext.Entry(module).Property(nameof(PlatformModule.ModuleName)).CurrentValue = name;
        dbContext.Entry(module).Property(nameof(PlatformModule.IsCoreModule)).CurrentValue = true;
        dbContext.Entry(module).Property(nameof(PlatformModule.Status)).CurrentValue = PlatformAuthConstants.ActiveStatus;
        dbContext.Entry(module).Property(nameof(PlatformModule.SortOrder)).CurrentValue = sortOrder;
        dbContext.Entry(module).Property(nameof(PlatformModule.UpdatedAt)).CurrentValue = now;
    }

    private static async Task UpsertFeatureAsync(
        EPosDbContext dbContext,
        Guid featureId,
        Guid platformModuleId,
        string featureCode,
        string name,
        int sortOrder,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var feature = await dbContext.PlatformFeatures
            .SingleOrDefaultAsync(item => item.FeatureKey == featureCode, cancellationToken);

        if (feature is null)
        {
            dbContext.PlatformFeatures.Add(PlatformFeature.Create(
                featureId,
                platformModuleId,
                featureCode,
                name,
                PlatformAuthConstants.ActiveStatus,
                now,
                sortOrder,
                isCoreFeature: true));

            return;
        }

        dbContext.Entry(feature).Property(nameof(PlatformFeature.PlatformModuleId)).CurrentValue = platformModuleId;
        dbContext.Entry(feature).Property(nameof(PlatformFeature.FeatureCode)).CurrentValue = featureCode;
        dbContext.Entry(feature).Property(nameof(PlatformFeature.Name)).CurrentValue = name;
        dbContext.Entry(feature).Property(nameof(PlatformFeature.FeatureName)).CurrentValue = name;
        dbContext.Entry(feature).Property(nameof(PlatformFeature.IsCoreFeature)).CurrentValue = true;
        dbContext.Entry(feature).Property(nameof(PlatformFeature.Status)).CurrentValue = PlatformAuthConstants.ActiveStatus;
        dbContext.Entry(feature).Property(nameof(PlatformFeature.SortOrder)).CurrentValue = sortOrder;
        dbContext.Entry(feature).Property(nameof(PlatformFeature.UpdatedAt)).CurrentValue = now;
    }

    private static async Task<Guid> GetModuleIdAsync(
        EPosDbContext dbContext,
        string moduleCode,
        CancellationToken cancellationToken)
    {
        return await dbContext.PlatformModules
            .AsNoTracking()
            .Where(item => item.ModuleCode == moduleCode)
            .Select(item => item.Id)
            .SingleAsync(cancellationToken);
    }
}
