using E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;
using E_POS.Application.Modules.Tenant.TenantFoundation.Dtos;
using E_POS.Application.Modules.Tenant.TenantFoundation.Exceptions;
using E_POS.Application.Modules.Tenant.TenantFoundation.Services;
using E_POS.Domain.Modules.Platform.Subscription.Constants;
using E_POS.Domain.Modules.Platform.Subscription.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Constants;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using E_POS.Infrastructure.Modules.Platform.PlatformAdmin.Repositories;
using E_POS.Infrastructure.Modules.Tenant.TenantFoundation.Repositories;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace E_POS.IntegrationTests.PlatformAdministration;

public sealed class TenantFinalizeDefaultSettingsTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 7, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task CreateTenantWizardAsync_PersistsCoreTenantSettings()
    {
        await using var db = CreateDbContext();
        await SeedCatalogAsync(db);

        var provider = CreateProvider(db);
        var provision = await provider.BuildAsync(
            new DefaultTenantSettingsProvisionRequest(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Now,
                null,
                null,
                null,
                "LKR",
                []),
            CancellationToken.None);

        var tenantId = provision.SettingsToInsert[0].TenantId;
        await PersistTenantWithSettingsAsync(db, tenantId, provision.SettingsToInsert);

        var saved = await db.TenantSettings.AsNoTracking()
            .Where(x => x.TenantId == tenantId)
            .ToListAsync();

        Assert.Equal(TenantSettingKeys.CoreKeys.Count, saved.Count);
        Assert.All(saved, row => Assert.Equal(tenantId, row.TenantId));
    }

    [Fact]
    public async Task Provider_InventoryEntitled_PersistsInventorySetting()
    {
        await using var db = CreateDbContext();
        await SeedCatalogAsync(db);

        var provider = CreateProvider(db);
        var tenantId = Guid.NewGuid();
        var provision = await provider.BuildAsync(
            new DefaultTenantSettingsProvisionRequest(
                tenantId,
                Guid.NewGuid(),
                Now,
                "LKR",
                "Asia/Colombo",
                "en-LK",
                "LKR",
                [PlatformTenantFeatureCodes.InventoryTracking]),
            CancellationToken.None);

        await PersistTenantWithSettingsAsync(db, tenantId, provision.SettingsToInsert);

        var keys = await (
            from setting in db.TenantSettings.AsNoTracking()
            join definition in db.SettingDefinitions.AsNoTracking()
                on setting.SettingDefinitionId equals definition.Id
            where setting.TenantId == tenantId
            select definition.SettingKey).ToListAsync();

        Assert.Contains(TenantSettingKeys.InventoryStockBehaviour, keys);
        Assert.DoesNotContain(TenantSettingKeys.OnlineStoreDefaults, keys);
    }

    [Fact]
    public async Task Provider_OnlineStoreNotEntitled_SkipsOnlineStoreSetting()
    {
        await using var db = CreateDbContext();
        await SeedCatalogAsync(db);

        var provider = CreateProvider(db);
        var result = await provider.BuildAsync(
            new DefaultTenantSettingsProvisionRequest(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Now,
                "LKR",
                "Asia/Colombo",
                "en-LK",
                "LKR",
                [PlatformTenantFeatureCodes.InventoryTracking]),
            CancellationToken.None);

        Assert.DoesNotContain(TenantSettingKeys.OnlineStoreDefaults, result.ProvisionedSettingKeys);
        Assert.Contains(TenantSettingKeys.OnlineStoreDefaults, result.SkippedEntitlementSettingKeys);
    }

    [Fact]
    public async Task Provider_Retry_DoesNotDuplicateOrOverwriteCustomValue()
    {
        await using var db = CreateDbContext();
        await SeedCatalogAsync(db);

        var tenantId = Guid.NewGuid();
        var provider = CreateProvider(db);
        var first = await provider.BuildAsync(
            new DefaultTenantSettingsProvisionRequest(
                tenantId, Guid.NewGuid(), Now, "LKR", "Asia/Colombo", "en-LK", "LKR", []),
            CancellationToken.None);
        await PersistTenantWithSettingsAsync(db, tenantId, first.SettingsToInsert);

        var taxSetting = await db.TenantSettings.SingleAsync(x =>
            x.TenantId == tenantId &&
            x.SettingDefinitionId == TenantSettingDefinitionSeed.TaxPricingModeId);
        db.TenantSettings.Remove(taxSetting);
        db.TenantSettings.Add(TenantSetting.Create(
            taxSetting.Id,
            tenantId,
            TenantSettingDefinitionSeed.TaxPricingModeId,
            "\"TAX_INCLUSIVE\"",
            null,
            Now));
        await db.SaveChangesAsync();

        var second = await provider.BuildAsync(
            new DefaultTenantSettingsProvisionRequest(
                tenantId, Guid.NewGuid(), Now, "LKR", "Asia/Colombo", "en-LK", "LKR", []),
            CancellationToken.None);

        Assert.Empty(second.SettingsToInsert);

        var preserved = await db.TenantSettings.AsNoTracking().SingleAsync(x =>
            x.TenantId == tenantId &&
            x.SettingDefinitionId == TenantSettingDefinitionSeed.TaxPricingModeId);
        Assert.Equal("\"TAX_INCLUSIVE\"", preserved.SettingValue);
        Assert.Equal(TenantSettingKeys.CoreKeys.Count, await db.TenantSettings.CountAsync(x => x.TenantId == tenantId));
    }

    [Fact]
    public async Task Scenario11_MissingMandatoryDefinition_FailsBeforePersist()
    {
        await using var db = CreateDbContext();
        await SeedCatalogAsync(db);
        var tax = await db.SettingDefinitions.SingleAsync(x => x.SettingKey == TenantSettingKeys.TaxPricingMode);
        db.SettingDefinitions.Remove(tax);
        await db.SaveChangesAsync();

        var provider = CreateProvider(db);
        var tenantId = Guid.NewGuid();

        await Assert.ThrowsAsync<MissingMandatoryTenantSettingDefinitionException>(() =>
            provider.BuildAsync(
                new DefaultTenantSettingsProvisionRequest(
                    tenantId, Guid.NewGuid(), Now, "LKR", "Asia/Colombo", "en-LK", "LKR", []),
                CancellationToken.None));

        Assert.Empty(await db.Tenants.Where(x => x.Id == tenantId).ToListAsync());
        Assert.Empty(await db.TenantSettings.Where(x => x.TenantId == tenantId).ToListAsync());
    }

    [Fact]
    public async Task Scenario11_MissingPlatformCurrency_FailsClosed()
    {
        await using var db = CreateDbContext();
        foreach (var row in TenantSettingDefinitionSeed.All)
        {
            db.SettingDefinitions.Add(SettingDefinition.Create(
                row.Id,
                row.SettingKey,
                row.DisplayName,
                row.ValueType,
                row.DefaultValueJson,
                row.Description,
                row.IsTenantEditable,
                TenantSettingKeys.SettingDefinitionStatusActive,
                Now));
        }

        db.PlatformSettings.Add(Domain.Modules.Platform.PlatformAdmin.Entities.PlatformSetting.Create(
            Guid.Parse("c1000000-0000-4000-8000-000000000002"),
            "general.default_timezone",
            "Asia/Colombo",
            false,
            "test",
            Now));
        db.PlatformSettings.Add(Domain.Modules.Platform.PlatformAdmin.Entities.PlatformSetting.Create(
            Guid.Parse("c1000000-0000-4000-8000-000000000003"),
            "general.default_locale",
            "en-LK",
            false,
            "test",
            Now));
        await db.SaveChangesAsync();

        var provider = new DefaultTenantSettingsProvider(
            new PlatformSettingsRepository(db),
            new SettingDefinitionRepository(db),
            NullLogger<DefaultTenantSettingsProvider>.Instance);

        await Assert.ThrowsAsync<MissingPlatformGeneralDefaultException>(() =>
            provider.BuildAsync(
                new DefaultTenantSettingsProvisionRequest(
                    Guid.NewGuid(), Guid.NewGuid(), Now, null, "Asia/Colombo", "en-LK", null, []),
                CancellationToken.None));
    }

    [Fact]
    public async Task TenantIsolation_SettingsDoNotLeakAcrossTenants()
    {
        await using var db = CreateDbContext();
        await SeedCatalogAsync(db);
        var provider = CreateProvider(db);

        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var a = await provider.BuildAsync(
            new DefaultTenantSettingsProvisionRequest(
                tenantA, null, Now, "LKR", "Asia/Colombo", "en-LK", "LKR", []),
            CancellationToken.None);
        var b = await provider.BuildAsync(
            new DefaultTenantSettingsProvisionRequest(
                tenantB, null, Now, "USD", "UTC", "en-US", "USD", []),
            CancellationToken.None);

        await PersistTenantWithSettingsAsync(db, tenantA, a.SettingsToInsert);
        await PersistTenantWithSettingsAsync(db, tenantB, b.SettingsToInsert);

        Assert.Equal(
            TenantSettingKeys.CoreKeys.Count,
            await db.TenantSettings.CountAsync(x => x.TenantId == tenantA));
        Assert.Equal(
            TenantSettingKeys.CoreKeys.Count,
            await db.TenantSettings.CountAsync(x => x.TenantId == tenantB));
        Assert.DoesNotContain(
            await db.TenantSettings.Where(x => x.TenantId == tenantA).ToListAsync(),
            row => row.TenantId == tenantB);
    }

    private static DefaultTenantSettingsProvider CreateProvider(EPosDbContext db) =>
        new(
            new PlatformSettingsRepository(db),
            new SettingDefinitionRepository(db),
            NullLogger<DefaultTenantSettingsProvider>.Instance);

    private static async Task SeedCatalogAsync(EPosDbContext db)
    {
        foreach (var row in TenantSettingDefinitionSeed.All)
        {
            db.SettingDefinitions.Add(SettingDefinition.Create(
                row.Id,
                row.SettingKey,
                row.DisplayName,
                row.ValueType,
                row.DefaultValueJson,
                row.Description,
                row.IsTenantEditable,
                TenantSettingKeys.SettingDefinitionStatusActive,
                Now));
        }

        db.PlatformSettings.Add(Domain.Modules.Platform.PlatformAdmin.Entities.PlatformSetting.Create(
            Guid.Parse("c1000000-0000-4000-8000-000000000001"),
            "general.default_currency_code",
            "LKR",
            false,
            "test",
            Now));
        db.PlatformSettings.Add(Domain.Modules.Platform.PlatformAdmin.Entities.PlatformSetting.Create(
            Guid.Parse("c1000000-0000-4000-8000-000000000002"),
            "general.default_timezone",
            "Asia/Colombo",
            false,
            "test",
            Now));
        db.PlatformSettings.Add(Domain.Modules.Platform.PlatformAdmin.Entities.PlatformSetting.Create(
            Guid.Parse("c1000000-0000-4000-8000-000000000003"),
            "general.default_locale",
            "en-LK",
            false,
            "test",
            Now));

        await db.SaveChangesAsync();
    }

    private static async Task PersistTenantWithSettingsAsync(
        EPosDbContext db,
        Guid tenantId,
        IReadOnlyList<TenantSetting> settings)
    {
        var planId = Guid.NewGuid();
        db.SubscriptionPlans.Add(SubscriptionPlan.Create(
            planId,
            "PHASE4",
            "Phase4",
            SubscriptionPlanConstants.Status.Active,
            SubscriptionPlanConstants.BillingInterval.Monthly,
            10m,
            Now,
            baseCurrency: "LKR"));

        var tenant = Tenant.Create(
            tenantId,
            $"T-{tenantId:N}"[..12],
            $"t-{tenantId:N}"[..12],
            "Phase4 Tenant",
            TenantStatusConstants.Active,
            "LKR",
            "Asia/Colombo",
            null,
            null,
            Now,
            "en-LK",
            null);

        var subscription = TenantSubscription.Create(
            Guid.NewGuid(),
            tenantId,
            planId,
            TenantSubscriptionStatusConstants.Active,
            TenantSubscriptionBillingConstants.BillingCycleMonthly,
            null, null, Now, null, true, null, null, 0m, null, null, null,
            null, null, null, "LKR", 10m, Now, Now, null, null, Now);

        await new PlatformTenantRepository(db).CreateTenantWizardAsync(
            new PlatformTenantCreateWriteModel
            {
                Tenant = tenant,
                Subscription = subscription,
                TenantSettings = settings
            },
            CancellationToken.None);
    }

    private static EPosDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<EPosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new EPosDbContext(options);
    }
}
