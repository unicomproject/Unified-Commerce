using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using E_POS.Domain.Modules.Tenant.Inventory.Entities;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using E_POS.Infrastructure.Persistence;
using E_POS.InventoryMaintenance;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace E_POS.UnitTests.Inventory;

public sealed class DevelopmentInventoryTopUpServiceTests
{
    [Fact]
    public async Task ExecuteAsync_AppliesEligibilityTrackingAuditAndIdempotencyRules()
    {
        await using var db = CreateDbContext();
        var now = DateTimeOffset.UtcNow;
        var tenant = Tenant.Create(
            Guid.NewGuid(), "DEV", "dev", "Development", "active",
            "LKR", "Asia/Colombo", null, null, now);
        var otherTenant = Tenant.Create(
            Guid.NewGuid(), "OTHER", "other", "Other", "active",
            "LKR", "Asia/Colombo", null, null, now);
        var actor = TenantUser.Create(
            Guid.NewGuid(), tenant.Id, "inventory@example.test", "Inventory User",
            null, null, "hash", "salt", "ACTIVE", "admin", "admin", "DEV", now,
            staffCode: "USR-2026-99301");
        var outlet = Outlet.Create(
            Guid.NewGuid(), tenant.Id, "Development Store", "DEV-01", "ACTIVE",
            "STORE", "Asia/Colombo", true, null, null, actor.Id, now);
        var location = InventoryLocation.Create(
            Guid.NewGuid(), tenant.Id, outlet.Id, null, "FLOOR", "Store Floor",
            "SALES", true, true, true, false, "ACTIVE", actor.Id, now);
        var wrongLocation = InventoryLocation.Create(
            Guid.NewGuid(), tenant.Id, outlet.Id, null, "OTHER", "Other",
            "STORE", true, false, false, false, "ACTIVE", actor.Id, now);

        db.AddRange(tenant, otherTenant, actor, outlet, location, wrongLocation);

        var zero = AddVariant(db, tenant.Id, actor.Id, now, "P0", "V0");
        var twentyFive = AddVariant(db, tenant.Id, actor.Id, now, "P25", "V25");
        var hundred = AddVariant(db, tenant.Id, actor.Id, now, "P100", "V100");
        var above = AddVariant(db, tenant.Id, actor.Id, now, "P125", "V125");
        var noTrack = AddVariant(db, tenant.Id, actor.Id, now, "PN", "VN");
        var batch = AddVariant(db, tenant.Id, actor.Id, now, "PB", "VB");
        var serial = AddVariant(db, tenant.Id, actor.Id, now, "PS", "VS");
        var inactiveVariant = AddVariant(
            db, tenant.Id, actor.Id, now, "PIV", "VIV", variantStatus: "INACTIVE");
        var inactiveProduct = AddVariant(
            db, tenant.Id, actor.Id, now, "PIP", "VIP", productStatus: "INACTIVE");
        var other = AddVariant(db, otherTenant.Id, null, now, "PO", "VO");

        var balance25 = AddBalance(
            db, tenant.Id, location.Id, twentyFive.Product.Id, twentyFive.Variant.Id,
            now, 25, reserved: 5, damaged: 2, quarantine: 1);
        AddBalance(db, tenant.Id, location.Id, hundred.Product.Id, hundred.Variant.Id, now, 100);
        AddBalance(db, tenant.Id, location.Id, above.Product.Id, above.Variant.Id, now, 125);
        var wrongLocationBalance = AddBalance(
            db, tenant.Id, wrongLocation.Id, zero.Product.Id, zero.Variant.Id, now, 40);
        var otherTenantBalance = AddBalance(
            db, otherTenant.Id, Guid.NewGuid(), other.Product.Id, other.Variant.Id, now, 10);

        AddSetting(db, tenant.Id, noTrack, now, isStockTracked: false);
        AddSetting(db, tenant.Id, batch, now, requiresBatch: true);
        AddSetting(db, tenant.Id, serial, now, requiresSerial: true);
        await db.SaveChangesAsync();

        var service = new DevelopmentInventoryTopUpService(db);
        var options = new DevelopmentInventoryTopUpOptions(
            "DEV", "DEV-01", "FLOOR", "inventory@example.test", 100);
        var first = await service.ExecuteAsync(options, CancellationToken.None);

        Assert.Equal(2, first.VariantsToppedUp);
        Assert.Equal(2, first.AlreadySufficient);
        Assert.Equal(1, first.SkippedNonStockTracked);
        Assert.Equal(1, first.SkippedBatchTracked);
        Assert.Equal(1, first.SkippedSerialTracked);
        Assert.Equal(1, first.MissingBalancesCreated);
        Assert.Equal(2, first.AdjustmentLinesCreated);
        Assert.Equal(2, first.StockMovementsCreated);
        Assert.Equal(100, balance25.OnHandQuantity);
        Assert.Equal(5, balance25.ReservedQuantity);
        Assert.Equal(2, balance25.DamagedQuantity);
        Assert.Equal(1, balance25.QuarantineQuantity);
        Assert.Equal(40, wrongLocationBalance.OnHandQuantity);
        Assert.Equal(10, otherTenantBalance.OnHandQuantity);
        Assert.DoesNotContain(first.Items, x =>
            x.ProductId == inactiveProduct.Product.Id ||
            x.ProductVariantId == inactiveVariant.Variant.Id);
        Assert.All(
            db.StockMovements.Where(x => x.TenantId == tenant.Id),
            x => Assert.Equal(DevelopmentInventoryTopUpService.ReasonCode, x.ReasonCode));
        Assert.Equal(2, await db.StockAdjustmentLines.CountAsync());
        Assert.Equal(2, await db.StockMovementReferences.CountAsync());

        var adjustmentCount = await db.StockAdjustments.CountAsync();
        var movementCount = await db.StockMovements.CountAsync();
        var second = await service.ExecuteAsync(options, CancellationToken.None);

        Assert.Equal(0, second.VariantsToppedUp);
        Assert.Null(second.StockAdjustmentId);
        Assert.Equal(adjustmentCount, await db.StockAdjustments.CountAsync());
        Assert.Equal(movementCount, await db.StockMovements.CountAsync());
    }

    private static EPosDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<EPosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new EPosDbContext(options);
    }

    private static (Product Product, ProductVariant Variant) AddVariant(
        EPosDbContext db,
        Guid tenantId,
        Guid? actorId,
        DateTimeOffset now,
        string productCode,
        string variantCode,
        string productStatus = "ACTIVE",
        string variantStatus = "ACTIVE")
    {
        var product = Product.Create(
            Guid.NewGuid(), tenantId, productCode, productCode, productCode.ToLowerInvariant(),
            "STANDARD", "SIMPLE", null, null, null, null, null,
            true, true, productStatus, actorId, now);
        var variant = ProductVariant.Create(
            Guid.NewGuid(), tenantId, product.Id, variantCode, variantCode, variantCode,
            Guid.NewGuid(), Guid.NewGuid(), true, true, false, variantStatus, actorId, now);
        db.AddRange(product, variant);
        return (product, variant);
    }

    private static InventoryBalance AddBalance(
        EPosDbContext db,
        Guid tenantId,
        Guid locationId,
        Guid productId,
        Guid variantId,
        DateTimeOffset now,
        decimal onHand,
        decimal reserved = 0,
        decimal damaged = 0,
        decimal quarantine = 0)
    {
        var balance = InventoryBalance.Create(
            Guid.NewGuid(), tenantId, locationId, productId, variantId, null, now);
        balance.AdjustQuantities(onHand, reserved, damaged, quarantine, now);
        db.Add(balance);
        return balance;
    }

    private static void AddSetting(
        EPosDbContext db,
        Guid tenantId,
        (Product Product, ProductVariant Variant) item,
        DateTimeOffset now,
        bool isStockTracked = true,
        bool requiresBatch = false,
        bool requiresSerial = false)
    {
        db.Add(ProductInventorySetting.Create(
            Guid.NewGuid(), tenantId, item.Product.Id, item.Variant.Id,
            Guid.NewGuid(), isStockTracked, false, requiresBatch, false,
            requiresSerial, "FIFO", "ACTIVE", null, now));
    }
}
