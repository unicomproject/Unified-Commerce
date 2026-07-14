using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.Inventory.Contracts;
using E_POS.Application.Modules.Tenant.Inventory.Dtos.TenantAdmin;
using E_POS.Application.Modules.Tenant.Inventory.Services;
using E_POS.Application.Modules.Tenant.Inventory.Validators;
using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using E_POS.Domain.Modules.Tenant.Inventory.Constants;
using E_POS.Domain.Modules.Tenant.Inventory.Entities;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Constants;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Constants;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using E_POS.Infrastructure.Modules.Tenant.Inventory.Repositories;
using E_POS.Infrastructure.Modules.Tenant.OutletTillDevice.Repositories;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace E_POS.IntegrationTests.Inventory;

public sealed class TenantAdminInventoryIntegrationTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 12, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task StockInAsync_UpdatesBalanceAndCreatesMovement()
    {
        await using var dbContext = CreateDbContext();
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var outletId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        var locationId = Guid.NewGuid();

        await SeedTenantAsync(dbContext, tenantId);
        await SeedOutletAsync(dbContext, tenantId, outletId);
        await SeedInventoryLocationAsync(dbContext, tenantId, outletId, locationId);
        await SeedProductWithVariantAsync(dbContext, tenantId, productId, variantId);

        var service = CreateService(dbContext);
        var request = new TenantAdminStockInRequest
        {
            OutletId = outletId,
            ReferenceNumber = "GRN-1001",
            Notes = "Opening stock",
            Items =
            [
                new TenantAdminStockInLineRequest
                {
                    ProductVariantId = variantId,
                    Quantity = 25,
                    UnitCost = 100,
                },
            ],
        };

        var result = await service.StockInAsync(
            CreateContext(tenantId, userId, [TenantAdminStockPermissions.StockIn]),
            request,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("COMPLETED", result.Value!.Status);
        Assert.Equal("Main Outlet", result.Value.OutletName);
        Assert.Single(result.Value.Items);
        Assert.Equal(25, result.Value.Items[0].OnHandAfter);
        Assert.Equal(25, result.Value.Items[0].AvailableAfter);
        Assert.Equal(1, await dbContext.StockMovements.CountAsync());
        Assert.Equal(25, await dbContext.InventoryBalances.Select(x => x.OnHandQuantity).SingleAsync());
    }

    [Fact]
    public async Task StockInAsync_WithoutPermission_ReturnsPermissionDenied()
    {
        await using var dbContext = CreateDbContext();
        var tenantId = Guid.NewGuid();
        await SeedTenantAsync(dbContext, tenantId);
        var service = CreateService(dbContext);

        var result = await service.StockInAsync(
            CreateContext(tenantId, Guid.NewGuid(), [TenantAdminStockPermissions.View]),
            new TenantAdminStockInRequest
            {
                OutletId = Guid.NewGuid(),
                Items = [new TenantAdminStockInLineRequest { ProductVariantId = Guid.NewGuid(), Quantity = 1 }],
            },
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("inventory.permission_denied", result.Error.Code);
    }

    [Fact]
    public async Task GetCurrentStockAsync_WithoutPermission_ReturnsPermissionDenied()
    {
        await using var dbContext = CreateDbContext();
        var tenantId = Guid.NewGuid();
        await SeedTenantAsync(dbContext, tenantId);
        var service = CreateService(dbContext);

        var result = await service.GetCurrentStockAsync(
            CreateContext(tenantId, Guid.NewGuid(), []),
            new TenantAdminCurrentStockQuery(null, null, null, null, null, null, 1, 50, null, null),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("inventory.permission_denied", result.Error.Code);
    }

    [Fact]
    public async Task StockInAsync_WithDifferentTenantOutlet_ReturnsOutletNotFound()
    {
        await using var dbContext = CreateDbContext();
        var tenantId = Guid.NewGuid();
        var otherTenantId = Guid.NewGuid();
        var outletId = Guid.NewGuid();
        var variantId = Guid.NewGuid();

        await SeedTenantAsync(dbContext, tenantId);
        await SeedTenantAsync(dbContext, otherTenantId);
        await SeedOutletAsync(dbContext, otherTenantId, outletId);
        await SeedInventoryLocationAsync(dbContext, otherTenantId, outletId, Guid.NewGuid());
        await SeedProductWithVariantAsync(dbContext, tenantId, Guid.NewGuid(), variantId);

        var service = CreateService(dbContext);
        var result = await service.StockInAsync(
            CreateContext(tenantId, Guid.NewGuid(), [TenantAdminStockPermissions.StockIn]),
            new TenantAdminStockInRequest
            {
                OutletId = outletId,
                Items = [new TenantAdminStockInLineRequest { ProductVariantId = variantId, Quantity = 5 }],
            },
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("inventory.outlet_not_found", result.Error.Code);
    }

    [Fact]
    public async Task StockInAsync_WithRestrictedOutletAccess_ReturnsOutletAccessDenied()
    {
        await using var dbContext = CreateDbContext();
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var allowedOutletId = Guid.NewGuid();
        var restrictedOutletId = Guid.NewGuid();
        var variantId = Guid.NewGuid();

        await SeedTenantAsync(dbContext, tenantId);
        await SeedOutletAsync(dbContext, tenantId, allowedOutletId, "Allowed Outlet");
        await SeedOutletAsync(dbContext, tenantId, restrictedOutletId, "Restricted Outlet");
        await SeedInventoryLocationAsync(dbContext, tenantId, restrictedOutletId, Guid.NewGuid());
        await SeedProductWithVariantAsync(dbContext, tenantId, Guid.NewGuid(), variantId);
        await SeedOutletUserRoleAsync(dbContext, tenantId, userId, allowedOutletId);

        var service = CreateService(dbContext);
        var result = await service.StockInAsync(
            CreateContext(tenantId, userId, [TenantAdminStockPermissions.StockIn]),
            new TenantAdminStockInRequest
            {
                OutletId = restrictedOutletId,
                Items = [new TenantAdminStockInLineRequest { ProductVariantId = variantId, Quantity = 5 }],
            },
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("inventory.outlet_access_denied", result.Error.Code);
    }

    [Fact]
    public async Task StockInAsync_WithBatchTrackedVariant_RequiresBatchNumber()
    {
        await using var dbContext = CreateDbContext();
        var tenantId = Guid.NewGuid();
        var outletId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var variantId = Guid.NewGuid();

        await SeedTenantAsync(dbContext, tenantId);
        await SeedOutletAsync(dbContext, tenantId, outletId);
        await SeedInventoryLocationAsync(dbContext, tenantId, outletId, Guid.NewGuid());
        await SeedProductWithVariantAsync(dbContext, tenantId, productId, variantId);
        await SeedBatchTrackedProductAsync(dbContext, tenantId, productId, variantId);

        var service = CreateService(dbContext);
        var result = await service.StockInAsync(
            CreateContext(tenantId, Guid.NewGuid(), [TenantAdminStockPermissions.StockIn]),
            new TenantAdminStockInRequest
            {
                OutletId = outletId,
                Items = [new TenantAdminStockInLineRequest { ProductVariantId = variantId, Quantity = 10 }],
            },
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("inventory.validation_failed", result.Error.Code);
        Assert.Contains(result.Error.FieldErrors!, x => x.Field == "items[0].batchNumber");
    }

    [Fact]
    public async Task StockInAsync_WithDuplicateVariantBatchLines_ReturnsValidationFailed()
    {
        await using var dbContext = CreateDbContext();
        var tenantId = Guid.NewGuid();
        var outletId = Guid.NewGuid();
        var variantId = Guid.NewGuid();

        await SeedTenantAsync(dbContext, tenantId);
        await SeedOutletAsync(dbContext, tenantId, outletId);
        await SeedInventoryLocationAsync(dbContext, tenantId, outletId, Guid.NewGuid());
        await SeedProductWithVariantAsync(dbContext, tenantId, Guid.NewGuid(), variantId);

        var service = CreateService(dbContext);
        var result = await service.StockInAsync(
            CreateContext(tenantId, Guid.NewGuid(), [TenantAdminStockPermissions.StockIn]),
            new TenantAdminStockInRequest
            {
                OutletId = outletId,
                Items =
                [
                    new TenantAdminStockInLineRequest { ProductVariantId = variantId, Quantity = 5 },
                    new TenantAdminStockInLineRequest { ProductVariantId = variantId, Quantity = 3 },
                ],
            },
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("inventory.validation_failed", result.Error.Code);
        Assert.Contains(result.Error.FieldErrors!, x => x.Field == "items[1].productVariantId");
    }

    [Fact]
    public async Task GetCurrentStockAsync_ReturnsStockAfterStockIn()
    {
        await using var dbContext = CreateDbContext();
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var outletId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        var locationId = Guid.NewGuid();

        await SeedTenantAsync(dbContext, tenantId);
        await SeedOutletAsync(dbContext, tenantId, outletId);
        await SeedInventoryLocationAsync(dbContext, tenantId, outletId, locationId);
        await SeedProductWithVariantAsync(dbContext, tenantId, productId, variantId);

        var service = CreateService(dbContext);
        await service.StockInAsync(
            CreateContext(tenantId, userId, [TenantAdminStockPermissions.StockIn]),
            new TenantAdminStockInRequest
            {
                OutletId = outletId,
                Items = [new TenantAdminStockInLineRequest { ProductVariantId = variantId, Quantity = 12 }],
            },
            CancellationToken.None);

        var result = await service.GetCurrentStockAsync(
            CreateContext(tenantId, userId, [TenantAdminStockPermissions.View]),
            new TenantAdminCurrentStockQuery(outletId, null, null, null, null, null, 1, 50, null, null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Items);
        Assert.Equal(12, result.Value.Items[0].OnHandQuantity);
        Assert.Equal("IN_STOCK", result.Value.Items[0].StockStatus);
        Assert.Equal("NOT_APPLICABLE", result.Value.Items[0].ExpiryStatus);
    }

    [Fact]
    public async Task GetCurrentStockSummaryAsync_UsesFullFilteredScope()
    {
        await using var dbContext = CreateDbContext();
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var outletId = Guid.NewGuid();
        var variantA = Guid.NewGuid();
        var variantB = Guid.NewGuid();

        await SeedTenantAsync(dbContext, tenantId);
        await SeedOutletAsync(dbContext, tenantId, outletId);
        await SeedInventoryLocationAsync(dbContext, tenantId, outletId, Guid.NewGuid());
        await SeedProductWithVariantAsync(dbContext, tenantId, Guid.NewGuid(), variantA, "Product A", "SKU-A");
        await SeedProductWithVariantAsync(dbContext, tenantId, Guid.NewGuid(), variantB, "Product B", "SKU-B");

        var service = CreateService(dbContext);
        var context = CreateContext(tenantId, userId, [TenantAdminStockPermissions.StockIn, TenantAdminStockPermissions.View]);
        await service.StockInAsync(
            context,
            new TenantAdminStockInRequest
            {
                OutletId = outletId,
                Items =
                [
                    new TenantAdminStockInLineRequest { ProductVariantId = variantA, Quantity = 4 },
                    new TenantAdminStockInLineRequest { ProductVariantId = variantB, Quantity = 6 },
                ],
            },
            CancellationToken.None);

        var summary = await service.GetCurrentStockSummaryAsync(context, outletId, CancellationToken.None);

        Assert.True(summary.IsSuccess);
        Assert.Equal(2, summary.Value!.TotalProducts);
        Assert.Equal(2, summary.Value.TotalVariants);
        Assert.Equal(10, summary.Value.TotalUnits);
    }

    [Fact]
    public async Task GetProductVariantsForStockInAsync_ReturnsTrackingFlags()
    {
        await using var dbContext = CreateDbContext();
        var tenantId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var variantId = Guid.NewGuid();

        await SeedTenantAsync(dbContext, tenantId);
        await SeedProductWithVariantAsync(dbContext, tenantId, productId, variantId);
        await SeedBatchTrackedProductAsync(dbContext, tenantId, productId, variantId);

        var service = CreateService(dbContext);
        var result = await service.GetProductVariantsForStockInAsync(
            CreateContext(tenantId, Guid.NewGuid(), [TenantAdminStockPermissions.StockIn]),
            productId,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Variants);
        Assert.True(result.Value.IsBatchTracked);
        Assert.True(result.Value.Variants[0].IsBatchTracked);
        Assert.True(result.Value.Variants[0].IsExpiryTracked);
    }

    [Fact]
    public async Task StockInAsync_WithDuplicateIdempotencyKey_ReturnsConflict()
    {
        await using var dbContext = CreateDbContext();
        var tenantId = Guid.NewGuid();
        var outletId = Guid.NewGuid();
        var variantId = Guid.NewGuid();

        await SeedTenantAsync(dbContext, tenantId);
        await SeedOutletAsync(dbContext, tenantId, outletId);
        await SeedInventoryLocationAsync(dbContext, tenantId, outletId, Guid.NewGuid());
        await SeedProductWithVariantAsync(dbContext, tenantId, Guid.NewGuid(), variantId);

        var service = CreateService(dbContext);
        var context = CreateContext(tenantId, Guid.NewGuid(), [TenantAdminStockPermissions.StockIn]);
        var request = new TenantAdminStockInRequest
        {
            OutletId = outletId,
            IdempotencyKey = "stock-in-001",
            Items = [new TenantAdminStockInLineRequest { ProductVariantId = variantId, Quantity = 3 }],
        };

        var first = await service.StockInAsync(context, request, CancellationToken.None);
        var second = await service.StockInAsync(context, request, CancellationToken.None);

        Assert.True(first.IsSuccess);
        Assert.False(second.IsSuccess);
        Assert.Equal("inventory.duplicate_idempotency_key", second.Error.Code);
    }

    private static TenantAdminInventoryService CreateService(EPosDbContext dbContext) =>
        new(
            new TenantAdminInventoryRepository(dbContext, new CodeSequenceRepository(dbContext)),
            new TenantAdminInventoryRequestValidator(),
            new FakeInventoryAuditLogger(),
            new FakeDateTimeProvider(Now));

    private static TenantRequestContext CreateContext(
        Guid tenantId,
        Guid userId,
        IReadOnlyCollection<string> permissions) =>
        new(tenantId, userId, permissions);

    private static EPosDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<EPosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new EPosDbContext(options);
    }

    private static async Task SeedTenantAsync(EPosDbContext dbContext, Guid tenantId)
    {
        dbContext.Tenants.Add(Tenant.Create(
            tenantId,
            $"TEN-{tenantId.ToString()[..8]}",
            $"tenant-{tenantId.ToString()[..8]}",
            "Test Tenant",
            TenantStatusConstants.Active,
            "LKR",
            "UTC",
            null,
            null,
            Now));

        await dbContext.SaveChangesAsync();
    }

    private static async Task SeedOutletAsync(
        EPosDbContext dbContext,
        Guid tenantId,
        Guid outletId,
        string outletName = "Main Outlet")
    {
        dbContext.Outlets.Add(Outlet.Create(
            outletId,
            tenantId,
            outletName,
            "OUT001",
            OutletConstants.ActiveStatus,
            "STORE",
            "UTC",
            true,
            null,
            null,
            null,
            Now));

        await dbContext.SaveChangesAsync();
    }

    private static async Task SeedInventoryLocationAsync(
        EPosDbContext dbContext,
        Guid tenantId,
        Guid outletId,
        Guid locationId)
    {
        dbContext.InventoryLocations.Add(InventoryLocation.Create(
            locationId,
            tenantId,
            outletId,
            null,
            "MAIN",
            "Main Store Stock",
            TenantAdminInventoryConstants.DefaultLocationType,
            isSellableLocation: true,
            isReturnLocation: true,
            isReceivingLocation: true,
            isQuarantineLocation: false,
            TenantAdminInventoryConstants.ActiveStatus,
            null,
            Now));

        await dbContext.SaveChangesAsync();
    }

    private static async Task SeedProductWithVariantAsync(
        EPosDbContext dbContext,
        Guid tenantId,
        Guid productId,
        Guid variantId,
        string productName = "Test Product",
        string sku = "SKU-001")
    {
        dbContext.Products.Add(Product.Create(
            productId,
            tenantId,
            "PROD-001",
            productName,
            "test-product",
            "STANDARD",
            "SIMPLE",
            null,
            null,
            null,
            null,
            null,
            true,
            true,
            ProductConstants.ActiveStatus,
            null,
            Now));

        dbContext.ProductVariants.Add(ProductVariant.Create(
            variantId,
            tenantId,
            productId,
            "DEFAULT",
            productName,
            sku,
            Guid.NewGuid(),
            Guid.NewGuid(),
            true,
            true,
            false,
            ProductConstants.ActiveStatus,
            null,
            Now));

        await dbContext.SaveChangesAsync();
    }

    private static async Task SeedBatchTrackedProductAsync(
        EPosDbContext dbContext,
        Guid tenantId,
        Guid productId,
        Guid variantId)
    {
        dbContext.ProductInventorySettings.Add(ProductInventorySetting.Create(
            Guid.NewGuid(),
            tenantId,
            productId,
            variantId,
            Guid.NewGuid(),
            isStockTracked: true,
            allowNegativeStock: false,
            requiresBatchTracking: true,
            requiresExpiryTracking: true,
            requiresSerialTracking: false,
            costingMethod: "FIFO",
            TenantAdminInventoryConstants.ActiveStatus,
            null,
            Now));

        await dbContext.SaveChangesAsync();
    }

    private static async Task SeedOutletUserRoleAsync(
        EPosDbContext dbContext,
        Guid tenantId,
        Guid userId,
        Guid outletId)
    {
        dbContext.OutletUserRoles.Add(OutletUserRole.Create(
            Guid.NewGuid(),
            tenantId,
            outletId,
            userId,
            Guid.NewGuid(),
            null,
            Now));

        await dbContext.SaveChangesAsync();
    }

    private sealed class FakeDateTimeProvider : IDateTimeProvider
    {
        public FakeDateTimeProvider(DateTimeOffset now) => UtcNow = now;

        public DateTimeOffset UtcNow { get; }
    }

    private sealed class FakeInventoryAuditLogger : ITenantAdminInventoryAuditLogger
    {
        public int LoggedCount { get; private set; }

        public void LogStockInCompleted(
            Guid tenantId,
            Guid userId,
            Guid operationId,
            Guid outletId,
            string? referenceNumber,
            int itemCount,
            decimal totalQuantity,
            IReadOnlyCollection<Guid> productVariantIds)
        {
            LoggedCount++;
        }
    }
}
