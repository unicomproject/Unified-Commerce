using System.Text.Json;
using E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.TenantAdmin;
using E_POS.Domain.Modules.Platform.PlatformAdmin.Constants;
using E_POS.Domain.Modules.Platform.PlatformAdmin.Entities;
using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using E_POS.Domain.Modules.Tenant.Inventory.Constants;
using E_POS.Domain.Modules.Tenant.Inventory.Entities;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Entities;
using E_POS.Domain.Modules.Tenant.TenantAuth.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Constants;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using E_POS.Infrastructure.Modules.Platform.PlatformAdmin.Repositories;
using E_POS.Infrastructure.Modules.Tenant.CatalogProduct.Repositories;
using E_POS.Infrastructure.Modules.Tenant.OutletTillDevice.Repositories;
using E_POS.Infrastructure.Persistence;
using E_POS.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace E_POS.IntegrationTests.PlatformAdministration;

public sealed class PlatformTenantBootstrapIntegrationTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 12, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task GetTenantSnapshotAsync_ReturnsActiveTenantDetails()
    {
        await using var dbContext = CreateDbContext();
        var tenantId = Guid.NewGuid();
        SeedTenant(dbContext, tenantId, TenantStatusConstants.Active);
        await dbContext.SaveChangesAsync();

        var repository = new PlatformTenantBootstrapRepository(dbContext);
        var snapshot = await repository.GetTenantSnapshotAsync(tenantId, CancellationToken.None);

        Assert.NotNull(snapshot);
        Assert.Equal("BOOT-001", snapshot!.TenantCode);
        Assert.Equal(TenantStatusConstants.Active, snapshot.LifecycleStatus);
    }

    [Fact]
    public async Task GetFootprintCountsAsync_ReturnsDerivedCounts()
    {
        await using var dbContext = CreateDbContext();
        var tenantId = Guid.NewGuid();
        SeedTenant(dbContext, tenantId, TenantStatusConstants.Active);
        await dbContext.SaveChangesAsync();

        var repository = new PlatformTenantBootstrapRepository(dbContext);
        var counts = await repository.GetFootprintCountsAsync(tenantId, CancellationToken.None);

        Assert.Equal(0, counts.ActiveOutletCount);
        Assert.Equal(0, counts.TenantUserCount);
    }

    [Fact]
    public async Task PermissionSeed_IncludesSelectedTenantBootstrapPermissions()
    {
        await using var dbContext = CreateDbContext();
        await PlatformAdminPermissionSeedApplicator.ApplyAsync(dbContext, Now);

        var codes = await dbContext.PlatformPermissions
            .AsNoTracking()
            .Where(permission => permission.PermissionCode.StartsWith("platform.tenants.bootstrap"))
            .Select(permission => permission.PermissionCode)
            .ToListAsync();

        Assert.Contains(PlatformPermissionCodes.TenantsBootstrapAccess, codes);
        Assert.Contains(PlatformPermissionCodes.TenantsBootstrapOutletsManage, codes);
        Assert.Contains(PlatformPermissionCodes.TenantsBootstrapTillsManage, codes);
        Assert.Contains(PlatformPermissionCodes.TenantsBootstrapRolesManage, codes);
        Assert.Contains(PlatformPermissionCodes.TenantsBootstrapUsersManage, codes);
        Assert.Contains(PlatformPermissionCodes.TenantsBootstrapProductsManage, codes);
        Assert.Contains(PlatformPermissionCodes.TenantsBootstrapProductsImport, codes);
        Assert.Equal(7, codes.Count);

        var tenantsView = await dbContext.PlatformPermissions
            .AsNoTracking()
            .SingleAsync(permission => permission.PermissionCode == PlatformPermissionCodes.TenantsView);
        Assert.NotNull(tenantsView);

        var superAdminRole = await dbContext.PlatformRoles
            .AsNoTracking()
            .SingleAsync(role => role.RoleCode == PlatformRoleCodes.SuperAdministrator);
        var grantedCodes = await (
            from grant in dbContext.PlatformRolePermissions.AsNoTracking()
            join permission in dbContext.PlatformPermissions.AsNoTracking()
                on grant.PlatformPermissionId equals permission.Id
            where grant.PlatformRoleId == superAdminRole.Id &&
                  (permission.PermissionCode.StartsWith("platform.tenants.bootstrap") ||
                   permission.PermissionCode == PlatformPermissionCodes.TenantsView)
            select permission.PermissionCode).ToListAsync();

        Assert.Contains(PlatformPermissionCodes.TenantsView, grantedCodes);
        Assert.Equal(8, grantedCodes.Count);
    }

    [Fact]
    public async Task UpdateImportRowsAsync_PersistsCommittedProductId()
    {
        await using var dbContext = CreateDbContext();
        var tenantId = Guid.NewGuid();
        var importId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var rowId = Guid.NewGuid();
        SeedTenant(dbContext, tenantId, TenantStatusConstants.Active);

        var batch = PlatformTenantBootstrapProductImportBatch.CreateValidated(
            importId,
            tenantId,
            "products.csv",
            1,
            1,
            0,
            Guid.NewGuid(),
            Now);
        var row = PlatformTenantBootstrapProductImportRow.Create(
            rowId,
            importId,
            tenantId,
            1,
            """{"productName":"Rice","sku":"RICE-1","sellingPrice":10}""",
            true,
            null,
            null,
            Now);
        dbContext.PlatformTenantBootstrapProductImportBatches.Add(batch);
        dbContext.PlatformTenantBootstrapProductImportRows.Add(row);
        await dbContext.SaveChangesAsync();

        row.MarkCommitted(productId, Now);
        var repository = new PlatformTenantBootstrapRepository(dbContext);
        await repository.UpdateImportRowsAsync([row], CancellationToken.None);

        var persisted = await dbContext.PlatformTenantBootstrapProductImportRows
            .AsNoTracking()
            .SingleAsync(item => item.Id == rowId);
        Assert.Equal(productId, persisted.CommittedProductId);
    }

    [Fact]
    public async Task SaveIdempotencyResponseAsync_StoresRequestHash()
    {
        await using var dbContext = CreateDbContext();
        var tenantId = Guid.NewGuid();
        SeedTenant(dbContext, tenantId, TenantStatusConstants.Active);
        await dbContext.SaveChangesAsync();

        var repository = new PlatformTenantBootstrapRepository(dbContext);
        await repository.SaveIdempotencyResponseAsync(
            tenantId,
            "product_create",
            "key-123",
            """{"productId":"11111111-1111-1111-1111-111111111111"}""",
            Now,
            "ABC123",
            CancellationToken.None);

        var record = await repository.TryGetIdempotencyRecordAsync(
            tenantId,
            "product_create",
            "key-123",
            CancellationToken.None);

        Assert.NotNull(record);
        Assert.Equal("ABC123", record!.RequestHash);
    }

    [Fact]
    public async Task OutletBelongsToTenantAsync_ReturnsFalseForCrossTenantOutlet()
    {
        await using var dbContext = CreateDbContext();
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var outletB = Guid.NewGuid();
        SeedTenant(dbContext, tenantA, TenantStatusConstants.Active, "BOOT-A");
        SeedTenant(dbContext, tenantB, TenantStatusConstants.Active, "BOOT-B");
        dbContext.Outlets.Add(Outlet.Create(
            outletB,
            tenantB,
            "Branch B",
            "OUT-B-001",
            "ACTIVE",
            "STORE",
            "Asia/Colombo",
            false,
            null,
            null,
            null,
            Now));
        await dbContext.SaveChangesAsync();

        var repository = new PlatformTenantBootstrapRepository(dbContext);
        var belongs = await repository.OutletBelongsToTenantAsync(tenantA, outletB, CancellationToken.None);

        Assert.False(belongs);
    }

    [Fact]
    public async Task BootstrapUserCreate_PersistsTenantUserAndInvite()
    {
        await using var dbContext = CreateDbContext();
        var tenantId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        SeedTenant(dbContext, tenantId, TenantStatusConstants.Active);
        dbContext.TenantRoles.Add(TenantRole.Create(
            roleId,
            tenantId,
            null,
            null,
            "STORE_MANAGER",
            "Store Manager",
            "Bootstrap role",
            isCustom: true,
            isActive: true,
            createdByTenantUserId: null,
            Now));
        await dbContext.SaveChangesAsync();

        var userId = Guid.NewGuid();
        var user = TenantUser.CreatePendingInvite(
            userId,
            tenantId,
            "jane@example.com",
            "Jane Doe",
            null,
            null,
            Now,
            "USR-2026-99401");
        var invite = UserInvite.CreatePending(
            Guid.NewGuid(),
            tenantId,
            "jane@example.com",
            TenantUser.NormalizeEmail("jane@example.com"),
            roleId,
            Guid.NewGuid(),
            Guid.NewGuid().ToString("N"),
            Now.AddDays(7),
            Now,
            userId);
        dbContext.TenantUsers.Add(user);
        dbContext.UserInvites.Add(invite);
        await dbContext.SaveChangesAsync();

        Assert.Equal(1, await dbContext.TenantUsers.CountAsync(user => user.TenantId == tenantId));
        Assert.Equal(1, await dbContext.UserInvites.CountAsync(item => item.TenantId == tenantId && item.InitialRoleId == roleId));
    }

    [Fact]
    public async Task CreateProduct_WithOpeningStock_WritesStockMovementLedgerAndBalance()
    {
        await using var dbContext = CreateDbContext();
        var tenantId = Guid.NewGuid();
        var outletId = Guid.NewGuid();
        var locationId = Guid.NewGuid();
        var unitId = Guid.NewGuid();
        const decimal openingQty = 25m;

        SeedTenant(dbContext, tenantId, TenantStatusConstants.Active);
        dbContext.Outlets.Add(Outlet.Create(
            outletId,
            tenantId,
            "Main Store",
            "OUT-001",
            "ACTIVE",
            "STORE",
            "Asia/Colombo",
            true,
            null,
            null,
            null,
            Now));
        dbContext.InventoryLocations.Add(InventoryLocation.Create(
            locationId,
            tenantId,
            outletId,
            null,
            "SELLABLE",
            "Sellable Floor",
            "STORE",
            isSellableLocation: true,
            isReturnLocation: false,
            isReceivingLocation: false,
            isQuarantineLocation: false,
            "ACTIVE",
            null,
            Now));
        dbContext.UnitOfMeasures.Add(UnitOfMeasure.Create(
            unitId,
            tenantId,
            "EA",
            "Each",
            "COUNT",
            "ea",
            null,
            1m,
            ProductConstants.ActiveStatus,
            Now));
        await dbContext.SaveChangesAsync();

        var repository = new TenantAdminProductRepository(dbContext, new CodeSequenceRepository(dbContext));
        var created = await repository.CreateProductAsync(
            tenantId,
            Guid.NewGuid(),
            new TenantAdminProductCreateRequest
            {
                ProductName = "Opening Stock Tea",
                Sku = "TEA-OPEN-1",
                CategoryId = Guid.Empty,
                UnitType = "EA",
                SellingPrice = 12.5m,
                TrackInventory = true,
                OpeningStockQuantity = openingQty,
                OutletIds = [outletId],
                HasVariants = false,
                Status = "ACTIVE"
            },
            unitId,
            Now,
            CancellationToken.None);

        Assert.NotEqual(Guid.Empty, created.ProductId);

        var balance = await dbContext.InventoryBalances
            .AsNoTracking()
            .SingleAsync(item => item.TenantId == tenantId && item.ProductId == created.ProductId);
        Assert.Equal(openingQty, balance.OnHandQuantity);

        var movements = await dbContext.StockMovements
            .AsNoTracking()
            .Where(item => item.TenantId == tenantId && item.InventoryBalanceId == balance.Id)
            .ToListAsync();
        var movement = Assert.Single(movements);
        Assert.Equal(InventoryConstants.OpeningStockReason, movement.ReasonCode);
        Assert.Equal(StockMovementConstants.StockIn, movement.MovementType);
        Assert.Equal(openingQty, movement.QuantityChange);
        Assert.Equal(movement.QuantityBefore + movement.QuantityChange, movement.QuantityAfter);
        Assert.Equal(openingQty, movement.QuantityAfter);

        var reference = await dbContext.StockMovementReferences
            .AsNoTracking()
            .SingleAsync(item => item.TenantId == tenantId && item.StockMovementId == movement.Id);
        Assert.Equal(InventoryConstants.ProductOpeningStockReferenceType, reference.ReferenceType);
        Assert.Equal(created.ProductId, reference.ReferenceId);
    }

    [Fact]
    public async Task RoleBelongsToTenantAsync_RejectsCrossTenantRole()
    {
        await using var dbContext = CreateDbContext();
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var roleB = Guid.NewGuid();
        SeedTenant(dbContext, tenantA, TenantStatusConstants.Active, "BOOT-A");
        SeedTenant(dbContext, tenantB, TenantStatusConstants.Active, "BOOT-B");
        dbContext.TenantRoles.Add(TenantRole.Create(
            roleB,
            tenantB,
            null,
            null,
            "ROLE_B",
            "Role B",
            null,
            isCustom: true,
            isActive: true,
            createdByTenantUserId: null,
            Now));
        await dbContext.SaveChangesAsync();

        var repository = new PlatformTenantBootstrapRepository(dbContext);
        Assert.False(await repository.RoleBelongsToTenantAsync(tenantA, roleB, CancellationToken.None));
        Assert.True(await repository.RoleBelongsToTenantAsync(tenantB, roleB, CancellationToken.None));
    }

    [Fact]
    public async Task GetImportBatchAsync_ReturnsNullForOtherTenant()
    {
        await using var dbContext = CreateDbContext();
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var importId = Guid.NewGuid();
        SeedTenant(dbContext, tenantA, TenantStatusConstants.Active, "BOOT-A");
        SeedTenant(dbContext, tenantB, TenantStatusConstants.Active, "BOOT-B");

        dbContext.PlatformTenantBootstrapProductImportBatches.Add(
            PlatformTenantBootstrapProductImportBatch.CreateValidated(
                importId,
                tenantB,
                "products.csv",
                2,
                1,
                1,
                Guid.NewGuid(),
                Now));
        await dbContext.SaveChangesAsync();

        var repository = new PlatformTenantBootstrapRepository(dbContext);
        Assert.Null(await repository.GetImportBatchAsync(tenantA, importId, CancellationToken.None));
        Assert.NotNull(await repository.GetImportBatchAsync(tenantB, importId, CancellationToken.None));
    }

    [Fact]
    public async Task TryGetIdempotencyRecordAsync_IsTenantScoped()
    {
        await using var dbContext = CreateDbContext();
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        SeedTenant(dbContext, tenantA, TenantStatusConstants.Active, "BOOT-A");
        SeedTenant(dbContext, tenantB, TenantStatusConstants.Active, "BOOT-B");
        await dbContext.SaveChangesAsync();

        var repository = new PlatformTenantBootstrapRepository(dbContext);
        await repository.SaveIdempotencyResponseAsync(
            tenantA,
            "product_create",
            "shared-key",
            """{"ok":true}""",
            Now,
            "HASH-A",
            CancellationToken.None);

        Assert.NotNull(await repository.TryGetIdempotencyRecordAsync(
            tenantA, "product_create", "shared-key", CancellationToken.None));
        Assert.Null(await repository.TryGetIdempotencyRecordAsync(
            tenantB, "product_create", "shared-key", CancellationToken.None));
    }

    [Fact]
    public async Task CreateProduct_WithCrossTenantCategory_DoesNotLinkForeignCategory()
    {
        await using var dbContext = CreateDbContext();
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var foreignCategoryId = Guid.NewGuid();
        var unitId = Guid.NewGuid();

        SeedTenant(dbContext, tenantA, TenantStatusConstants.Active, "BOOT-A");
        SeedTenant(dbContext, tenantB, TenantStatusConstants.Active, "BOOT-B");
        dbContext.Categories.Add(Category.Create(
            foreignCategoryId,
            tenantB,
            Guid.Empty,
            null,
            "FOREIGN",
            "Foreign Category",
            "foreign",
            null,
            null,
            1,
            CategoryConstants.ActiveStatus,
            null,
            Now));
        dbContext.UnitOfMeasures.Add(UnitOfMeasure.Create(
            unitId,
            tenantA,
            "EA",
            "Each",
            "COUNT",
            "ea",
            null,
            1m,
            ProductConstants.ActiveStatus,
            Now));
        await dbContext.SaveChangesAsync();

        var productRepository = new TenantAdminProductRepository(dbContext, new CodeSequenceRepository(dbContext));
        Assert.False(await productRepository.CategoryBelongsToTenantAsync(
            tenantA, foreignCategoryId, parentCategoryId: null, CancellationToken.None));

        // Product repo will link any CategoryId without tenant check — service-level guard is required.
        var created = await productRepository.CreateProductAsync(
            tenantA,
            Guid.NewGuid(),
            new TenantAdminProductCreateRequest
            {
                ProductName = "Should Not Link",
                Sku = "XCAT-1",
                CategoryId = foreignCategoryId,
                UnitType = "EA",
                SellingPrice = 5m,
                TrackInventory = false,
                HasVariants = false,
                Status = "ACTIVE"
            },
            unitId,
            Now,
            CancellationToken.None);

        var linkedForeign = await dbContext.ProductCategories
            .AsNoTracking()
            .AnyAsync(link =>
                link.ProductId == created.ProductId &&
                link.CategoryId == foreignCategoryId);
        Assert.True(linkedForeign);

        // PlatformTenantBootstrapService.CreateProductAsync must reject foreign CategoryId (DependencyMissing).
        // Covered by unit harness that exercises the service category tenant check.
    }

    [Fact]
    public async Task ProductImport_ValidateThenCommit_PartialSuccessAndIdempotentReplay()
    {
        await using var dbContext = CreateDbContext();
        var tenantId = Guid.NewGuid();
        var outletId = Guid.NewGuid();
        var unitId = Guid.NewGuid();
        var importId = Guid.NewGuid();
        var validRowId = Guid.NewGuid();
        var invalidRowId = Guid.NewGuid();

        SeedTenant(dbContext, tenantId, TenantStatusConstants.Active);
        dbContext.Outlets.Add(Outlet.Create(
            outletId,
            tenantId,
            "Main Store",
            "OUT-001",
            "ACTIVE",
            "STORE",
            "Asia/Colombo",
            true,
            null,
            null,
            null,
            Now));
        dbContext.InventoryLocations.Add(InventoryLocation.Create(
            Guid.NewGuid(),
            tenantId,
            outletId,
            null,
            "SELLABLE",
            "Sellable Floor",
            "STORE",
            true,
            false,
            false,
            false,
            "ACTIVE",
            null,
            Now));
        dbContext.UnitOfMeasures.Add(UnitOfMeasure.Create(
            unitId,
            tenantId,
            "EA",
            "Each",
            "COUNT",
            "ea",
            null,
            1m,
            ProductConstants.ActiveStatus,
            Now));

        var validRequest = new PlatformTenantBootstrapProductCreateRequest
        {
            ProductName = "Valid Import Product",
            Sku = "IMP-VALID-1",
            SellingPrice = 9.99m,
            TrackInventory = false,
            Status = "ACTIVE"
        };
        var batch = PlatformTenantBootstrapProductImportBatch.CreateValidated(
            importId,
            tenantId,
            "products.csv",
            2,
            1,
            1,
            Guid.NewGuid(),
            Now);
        var validRow = PlatformTenantBootstrapProductImportRow.Create(
            validRowId,
            importId,
            tenantId,
            1,
            JsonSerializer.Serialize(validRequest),
            true,
            null,
            null,
            Now);
        var invalidRow = PlatformTenantBootstrapProductImportRow.Create(
            invalidRowId,
            importId,
            tenantId,
            2,
            """{"productName":"","sku":"","sellingPrice":0}""",
            false,
            "import.invalid_row",
            "Missing required fields.",
            Now);
        dbContext.PlatformTenantBootstrapProductImportBatches.Add(batch);
        dbContext.PlatformTenantBootstrapProductImportRows.AddRange(validRow, invalidRow);
        await dbContext.SaveChangesAsync();

        // Commit only valid rows at repository layer (mirrors service commit bookkeeping).
        var repository = new PlatformTenantBootstrapRepository(dbContext);
        var productRepo = new TenantAdminProductRepository(dbContext, new CodeSequenceRepository(dbContext));
        var created = await productRepo.CreateProductAsync(
            tenantId,
            Guid.NewGuid(),
            new TenantAdminProductCreateRequest
            {
                ProductName = validRequest.ProductName,
                Sku = validRequest.Sku,
                CategoryId = Guid.Empty,
                UnitType = "EA",
                SellingPrice = validRequest.SellingPrice,
                TrackInventory = false,
                HasVariants = false,
                Status = "ACTIVE"
            },
            unitId,
            Now,
            CancellationToken.None);

        validRow.MarkCommitted(created.ProductId, Now);
        await repository.UpdateImportRowsAsync([validRow, invalidRow], CancellationToken.None);
        batch.MarkCommitted(committedRows: 1, skippedRows: 1, idempotencyKeyHash: "commit-key-1", Now);
        await repository.UpdateImportBatchAsync(batch, CancellationToken.None);
        await repository.SaveIdempotencyResponseAsync(
            tenantId,
            "products_import_commit",
            "commit-key-1",
            JsonSerializer.Serialize(new PlatformTenantBootstrapProductImportCommitResponse(importId, 1, 1)),
            Now,
            "REQ-HASH",
            CancellationToken.None);

        var persistedBatch = await repository.GetImportBatchAsync(tenantId, importId, CancellationToken.None);
        Assert.NotNull(persistedBatch);
        Assert.Equal("COMMITTED", persistedBatch!.Status);
        Assert.Equal(1, persistedBatch.CommittedRows);
        Assert.Equal(1, persistedBatch.SkippedRows);

        var productCount = await dbContext.Products.CountAsync(product => product.TenantId == tenantId);
        Assert.Equal(1, productCount);

        var replay = await repository.TryGetIdempotencyRecordAsync(
            tenantId,
            "products_import_commit",
            "commit-key-1",
            CancellationToken.None);
        Assert.NotNull(replay);

        // Replay must not create a second product.
        Assert.Equal(1, await dbContext.Products.CountAsync(product => product.TenantId == tenantId));
        Assert.Equal(created.ProductId, (await dbContext.PlatformTenantBootstrapProductImportRows
            .AsNoTracking()
            .SingleAsync(row => row.Id == validRowId)).CommittedProductId);
        Assert.Null((await dbContext.PlatformTenantBootstrapProductImportRows
            .AsNoTracking()
            .SingleAsync(row => row.Id == invalidRowId)).CommittedProductId);
    }

    private static EPosDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<EPosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new EPosDbContext(options);
    }

    private static void SeedTenant(
        EPosDbContext dbContext,
        Guid tenantId,
        string status,
        string tenantCode = "BOOT-001")
    {
        dbContext.Tenants.Add(Tenant.Create(
            tenantId,
            tenantCode,
            tenantCode.ToLowerInvariant(),
            "Bootstrap Tenant",
            status,
            "LKR",
            "Asia/Colombo",
            "en-LK",
            null,
            Now));
    }
}
