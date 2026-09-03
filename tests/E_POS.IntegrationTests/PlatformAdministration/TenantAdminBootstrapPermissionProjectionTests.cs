using E_POS.Domain.Modules.Platform.Subscription.Constants;
using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using E_POS.Domain.Modules.Tenant.AccessControl.Constants;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Constants;
using E_POS.Infrastructure.Modules.Platform.PlatformAdmin.Repositories;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace E_POS.IntegrationTests.PlatformAdministration;

public sealed class TenantAdminBootstrapPermissionProjectionTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 6, 14, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task GetActivePermissionIdMapByCodesAsync_ReturnsOnlyRequestedActiveCodes()
    {
        await using var db = CreateDbContext();
        SeedPermission(db, "tenant.dashboard.view");
        SeedPermission(db, "tenant.settings.manage");
        SeedPermission(db, "inventory.stock.view");
        SeedPermission(db, "platform.tenants.create", isActive: false);
        await db.SaveChangesAsync();

        var repository = new PlatformTenantRepository(db);
        var map = await repository.GetActivePermissionIdMapByCodesAsync(
            ["tenant.dashboard.view", "inventory.stock.view", "platform.tenants.create", "missing.code"],
            CancellationToken.None);

        Assert.True(map.ContainsKey("tenant.dashboard.view"));
        Assert.True(map.ContainsKey("inventory.stock.view"));
        Assert.False(map.ContainsKey("platform.tenants.create"));
        Assert.False(map.ContainsKey("missing.code"));
    }

    [Fact]
    public async Task GetActivePermissionIdMapByCodesAsync_ResolvesAllCashierBootstrapPermissionCodes()
    {
        await using var db = CreateDbContext();
        foreach (var code in TenantRoleSetupCatalog.CashierAllowedPermissionCodes)
        {
            SeedPermission(db, code);
        }
        await db.SaveChangesAsync();

        var repository = new PlatformTenantRepository(db);
        var map = await repository.GetActivePermissionIdMapByCodesAsync(
            TenantRoleSetupCatalog.CashierAllowedPermissionCodes.ToList(),
            CancellationToken.None);

        var missing = TenantRoleSetupCatalog.CashierAllowedPermissionCodes
            .Where(code => !map.ContainsKey(code))
            .ToList();

        Assert.Empty(missing);
    }

    [Fact]
    public void Resolve_OnlineOnlyTenant_ExcludesPosTillHardwareInventory()
    {
        var plan = TenantAdminBootstrapPermissionCatalog.Resolve(
        [
            PlatformTenantFeatureCodes.ProductCatalog,
            PlatformTenantFeatureCodes.OnlineStore,
            PlatformTenantFeatureCodes.SalesOrders
        ]);

        Assert.Contains("catalog.products.view", plan.PermissionCodes);
        Assert.Contains("fulfillment.orders.view", plan.PermissionCodes);
        Assert.DoesNotContain("pos.sale.create", plan.PermissionCodes);
        Assert.DoesNotContain("inventory.stock.view", plan.PermissionCodes);
        Assert.DoesNotContain(plan.PermissionCodes, code => code.StartsWith("tenant.tills.", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(plan.PermissionCodes, code => code.StartsWith("tenant.devices.", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(plan.PermissionCodes, code => code.StartsWith("platform.", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Resolve_PosProductOutletTill_GrantsCashierTemplateWithoutOnlineStoreOrInventory()
    {
        var plan = TenantAdminBootstrapPermissionCatalog.Resolve(
        [
            PlatformTenantFeatureCodes.PosCheckout,
            PlatformTenantFeatureCodes.ProductCatalog,
            PlatformTenantFeatureCodes.OutletManagement,
            PlatformTenantFeatureCodes.TillManagement
        ]);

        Assert.Contains("tenant.outlets.manage", plan.PermissionCodes);
        Assert.Contains("tenant.tills.manage", plan.PermissionCodes);
        Assert.Contains("catalog.products.create", plan.PermissionCodes);
        Assert.DoesNotContain("inventory.stock.view", plan.PermissionCodes);
        Assert.DoesNotContain("fulfillment.orders.manage", plan.PermissionCodes);
        Assert.Contains("sales.create", plan.PermissionCodes);
        Assert.Contains("payments.cash.accept", plan.PermissionCodes);
        Assert.DoesNotContain("payments.card.accept", plan.PermissionCodes);
    }

    [Fact]
    public void Resolve_DisabledConcept_MissingEntitlementGrantsNoModulePermissions()
    {
        var withInventory = TenantAdminBootstrapPermissionCatalog.Resolve(
        [
            PlatformTenantFeatureCodes.InventoryTracking
        ]);
        var withoutInventory = TenantAdminBootstrapPermissionCatalog.Resolve(
        [
            PlatformTenantFeatureCodes.OutletManagement
        ]);

        Assert.Contains("inventory.stock.view", withInventory.PermissionCodes);
        Assert.DoesNotContain("inventory.stock.view", withoutInventory.PermissionCodes);
    }

    private static void SeedPermission(EPosDbContext db, string code, bool isActive = true)
    {
        var moduleId = Guid.NewGuid();
        var featureId = Guid.NewGuid();
        db.PermissionDefinitions.Add(PermissionDefinition.Create(
            Guid.NewGuid(),
            code,
            moduleId,
            featureId,
            "manage",
            code,
            isSystem: true,
            isActive: isActive,
            now: Now));
    }

    private static EPosDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<EPosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new EPosDbContext(options);
    }
}
