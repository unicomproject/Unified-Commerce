using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.TenantAdmin;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Contracts;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Services;
using E_POS.Infrastructure.Modules.Tenant.CatalogProduct.Repositories;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

namespace E_POS.IntegrationTests.CatalogProduct;

/// <summary>
/// Live PostgreSQL verification for Chunk 6 wizard-create.
/// Soft-skips when DB/seed prerequisites are unavailable.
/// Implements IAsyncLifetime to auto-clean E2E test products after every run.
/// </summary>
public sealed class WizardProductCreatePostgreSqlTests : IAsyncLifetime
{
    private const string ConnectionString =
        "Host=localhost;Port=5434;Database=UnifiedCommerceDb;Username=postgres;Password=Nive@123";

    // Tracks every productId created during this test instance so DisposeAsync can clean them up.
    private readonly List<Guid> _createdProductIds = [];

    public Task InitializeAsync() => Task.CompletedTask;

    /// <summary>
    /// Runs after every [Fact] completes (pass or fail).
    /// Deletes all E2E products created during this test run from the dev DB.
    /// </summary>
    public async Task DisposeAsync()
    {
        if (_createdProductIds.Count == 0)
        {
            return;
        }

        try
        {
            await using var db = CreateDb();
            if (!await db.Database.CanConnectAsync())
            {
                return;
            }

            // Delete via raw SQL for reliability (avoids EF cascade issues).
            var ids = string.Join(",", _createdProductIds.Select(id => $"'{id}'::uuid"));
            await db.Database.ExecuteSqlRawAsync($"""
                DELETE FROM products WHERE id IN ({ids});
                """);
        }
        catch
        {
            // Best-effort cleanup — never fail the test run due to cleanup errors.
        }
    }

    [Fact]
    public async Task CreateProductFromWizard_Simple_SingleUnit_Persists_Complete_Graph()
    {
        if (!await CanConnectAsync())
        {
            return;
        }

        await using var db = CreateDb();
        var ctx = await LoadSeedContextAsync(db);
        if (ctx is null)
        {
            return;
        }

        var unique = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var sku = $"E2E-SIMPLE-{unique}";
        var barcode = $"89{unique % 10000000000:D10}";
        var name = $"E2E Simple Product {unique}";

        var beforeCount = await db.Products.CountAsync(p =>
            p.TenantId == ctx.TenantId && p.ProductName == name);

        var repo = new TenantAdminProductRepository(db, new NoOpCodeSequenceRepository());
        var request = BuildSimpleRequest(ctx, name, $"E2ES{unique}", sku, barcode, "SINGLE_UNIT", null, null);

        var balancesBefore = await db.InventoryBalances.AsNoTracking()
            .Where(b => b.TenantId == ctx.TenantId)
            .CountAsync();
            
        var movementsBefore = await db.StockMovements.AsNoTracking()
            .Where(m => m.TenantId == ctx.TenantId)
            .CountAsync();

        var result = await repo.CreateProductFromWizardAsync(
            ctx.TenantId, ctx.UserId, request, DateTimeOffset.UtcNow, CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error?.Message);
        Assert.NotNull(result.Response);
        
        var productId = result.Response!.ProductId;
        _createdProductIds.Add(productId);

        var afterCount = await db.Products.CountAsync(p =>
            p.TenantId == ctx.TenantId && p.ProductName == name);
        Assert.Equal(beforeCount + 1, afterCount);

        var product = await db.Products.AsNoTracking()
            .FirstAsync(p => p.TenantId == ctx.TenantId && p.Id == productId);
        Assert.Equal("SIMPLE", product.ProductStructure);
        Assert.Equal(ProductConstants.ActiveStatus, product.Status);

        var variants = await db.ProductVariants.AsNoTracking()
            .Where(v => v.TenantId == ctx.TenantId && v.ProductId == productId)
            .ToListAsync();
        Assert.Single(variants);
        Assert.True(variants[0].IsDefaultVariant);
        Assert.True(variants[0].IsSellable);
        Assert.Equal(sku, variants[0].Sku);

        var barcodes = await db.ProductBarcodes.AsNoTracking()
            .Where(b => b.TenantId == ctx.TenantId && b.ProductId == productId)
            .ToListAsync();
        Assert.Single(barcodes);
        Assert.Equal(barcode, barcodes[0].Barcode);
        Assert.Equal(variants[0].Id, barcodes[0].ProductVariantId);

        Assert.NotEmpty(await db.PriceListItems.AsNoTracking()
            .Where(p => p.TenantId == ctx.TenantId && p.ProductId == productId)
            .ToListAsync());
        Assert.NotEmpty(await db.ProductTaxAssignments.AsNoTracking()
            .Where(t => t.TenantId == ctx.TenantId && t.ProductId == productId)
            .ToListAsync());
        Assert.NotEmpty(await db.ProductChannelVisibilities.AsNoTracking()
            .Where(c => c.TenantId == ctx.TenantId && c.ProductId == productId)
            .ToListAsync());
        Assert.NotEmpty(await db.ProductUnitSettings.AsNoTracking()
            .Where(u => u.TenantId == ctx.TenantId && u.ProductId == productId)
            .ToListAsync());

        var list = await repo.GetPagedListAsync(
            ctx.TenantId,
            search: name,
            categoryId: null,
            brandId: null,
            productStatus: ProductConstants.ActiveStatus,
            stockStatus: null,
            pageNumber: 1,
            pageSize: 20,
            sortBy: null,
            sortDirection: null,
            canViewStock: false,
            cancellationToken: CancellationToken.None);
        Assert.Contains(list.Items, i => i.Id == productId && i.Name == name);

        var targetBalances = await db.InventoryBalances.AsNoTracking()
            .Where(b => b.TenantId == ctx.TenantId && b.ProductVariantId == variants[0].Id)
            .ToListAsync();
            
        var movementsAfter = await db.StockMovements.AsNoTracking()
            .Where(m => m.TenantId == ctx.TenantId)
            .CountAsync();
            
        Assert.Empty(targetBalances);
        Assert.Equal(0, movementsAfter - movementsBefore);
    }

    [Fact]
    public async Task CreateProductFromWizard_ScannerFirst_Simple_Reuses_PrimaryBarcode()
    {
        if (!await CanConnectAsync())
        {
            return;
        }

        await using var db = CreateDb();
        var ctx = await LoadSeedContextAsync(db);
        if (ctx is null)
        {
            return;
        }

        var unique = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + 99;
        var sku = $"E2E-SCAN-{unique}";
        var barcode = $"89{unique % 10000000000:D10}";
        var name = $"E2E Scan Product {unique}";
        var productId = Guid.NewGuid();

        // 1. Create scanner-first draft
        var product = Product.Create(
            productId, ctx.TenantId, $"P-{productId.ToString()[..8]}", name, $"scan-{unique}", "GOODS", 
            ProductStructureConstants.Simple, null, null, null, null, null, true, true, "DRAFT", ctx.UserId, DateTimeOffset.UtcNow, false);
        product.SetDraftSaved(2, DateTimeOffset.UtcNow); // Simulating draft state

        db.Products.Add(product);
        
        // 2. Persist Step 1 Primary Barcode (Scan Context)
        db.ProductSetupScanContexts.Add(ProductSetupScanContext.Create(
            Guid.NewGuid(), ctx.TenantId, productId, "SCAN", barcode, "GTIN12", 
            "UNKNOWN", null, "FOUND", null, null, null, null, DateTimeOffset.UtcNow));

        await db.SaveChangesAsync();

        var repo = new TenantAdminProductRepository(db, new NoOpCodeSequenceRepository());
        
        var request = new SaveProductDraftCommand(
            productId,
            name,
            $"E2ESCAN{unique}",
            $"scan-{unique}",
            ProductStructureConstants.Simple,
            null,
            null,
            null,
            null,
            "ACTIVE",
            true,
            false,
            false,
            false,
            false,
            true,
            4,
            4,
            product.RowVersion,
            Array.Empty<Guid>(),
            "SINGLE_UNIT",
            null,
            null,
            null,
            null,
            null,
            null,
            false,
            false,
            "PUBLISH",
            null,
            null,
            new BarcodeSkuConfigurationDto(
                Array.Empty<Step5IdentifierTargetDto>(),
                [new BarcodeSkuAssignmentDto(null, "BASE", sku, barcode, null, "BASE", "UPCA")]),
            null, // PricingTax
            null, // InitialBatchNumber
            null, // InitialExpiryDate
            null, // InitialSerialNumber
            false, // ConfirmClearIncompatibleInitialTracking
            null, // InitialTrackingAssignedVariantId
            true, // ApplyChannelMutation
            null, // ScanBootstrap
            true, // ApplyCompositeStep5Identifiers
            null, // AutoSkuBase
            $$"""
            {
                "units": [{"type": "BASE", "uomId": null}],
                "variants": [],
                "identifiers": [
                    {"clientCombinationKey": "BASE", "barcode": "{{barcode}}", "sku": "{{sku}}"}
                ]
            }
            """
        );

        var result = await repo.SaveProductDraftAsync(
            ctx.TenantId, ctx.UserId, request, DateTimeOffset.UtcNow, CancellationToken.None);

        var errorMessage = result.Error?.Message;
        if (result.Error?.FieldErrors != null)
        {
            errorMessage += " " + string.Join(", ", result.Error.FieldErrors.Select(f => $"{f.Field}: {f.Message}"));
        }

        Assert.True(result.IsSuccess, errorMessage);

        await db.SaveChangesAsync();

        // 4. Reload persisted identifier state
        var barcodes = await db.ProductBarcodes.AsNoTracking()
            .Where(b => b.TenantId == ctx.TenantId && b.ProductId == productId)
            .ToListAsync();

        // 5. Assert same barcode value
        // 6. Assert exactly one active/canonical primary barcode record
        Assert.Single(barcodes);
        Assert.Equal(barcode, barcodes[0].Barcode);
        
        // 7. Assert no duplicate barcode entity was created (Total barcodes for this product globally)
        var count = await db.ProductBarcodes.CountAsync(b => b.TenantId == ctx.TenantId && b.ProductId == productId);
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task CreateProductFromWizard_Simple_MultipleUnit_Persists_Conversions()
    {
        if (!await CanConnectAsync())
        {
            return;
        }

        await using var db = CreateDb();
        var ctx = await LoadSeedContextAsync(db);
        if (ctx is null)
        {
            return;
        }

        var uoms = await db.UnitOfMeasures.AsNoTracking()
            .Where(u => (u.TenantId == null || u.TenantId == ctx.TenantId) && u.Status == "ACTIVE")
            .Select(u => u.Id)
            .Take(2)
            .ToListAsync();
        if (uoms.Count < 2)
        {
            return;
        }

        var unique = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + 1;
        var sku = $"E2E-MULTI-{unique}";
        var name = $"E2E Simple Multi {unique}";
        var repo = new TenantAdminProductRepository(db, new NoOpCodeSequenceRepository());
        var request = BuildSimpleRequest(
            ctx, name, $"E2EM{unique}", sku, null, "MULTIPLE_UNITS", uoms[0], uoms[1]);
        request.ItemsPerPurchaseUnit = 12;
        request.AllowDecimalQuantity = false;

        var result = await repo.CreateProductFromWizardAsync(
            ctx.TenantId, ctx.UserId, request, DateTimeOffset.UtcNow, CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error?.Message);
        var productId = result.Response!.ProductId;
        _createdProductIds.Add(productId);

        var settings = await db.ProductUnitSettings.AsNoTracking()
            .Where(u => u.TenantId == ctx.TenantId && u.ProductId == productId)
            .ToListAsync();
        Assert.NotEmpty(settings);

        var variants = await db.ProductVariants.AsNoTracking()
            .Where(v => v.TenantId == ctx.TenantId && v.ProductId == productId)
            .ToListAsync();
        Assert.Single(variants);
        Assert.Equal(sku, variants[0].Sku);
    }

    [Fact]
    public async Task CreateProductFromWizard_Variant_Maps_ClientCombinationKey_To_Sku()
    {
        if (!await CanConnectAsync())
        {
            return;
        }

        await using var db = CreateDb();
        var ctx = await LoadSeedContextAsync(db);
        if (ctx is null)
        {
            return;
        }

        var unique = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + 2;
        var name = $"E2E Variant Product {unique}";
        var keys = new[]
        {
            ("Color:Red;Size:Small", "E2E-RS-" + unique),
            ("Color:Red;Size:Medium", "E2E-RM-" + unique),
            ("Color:Blue;Size:Small", "E2E-BS-" + unique),
            ("Color:Blue;Size:Medium", "E2E-BM-" + unique),
        };

        var options = new List<VariantConfigurationOptionDto>
        {
            new(
                null, null, "COLOR", "Color", "TEXT", "TEXT", 0,
                [
                    new VariantConfigurationOptionValueDto(null, null, "RED", "Red", "Red", null, 0, null),
                    new VariantConfigurationOptionValueDto(null, null, "BLUE", "Blue", "Blue", null, 1, null),
                ]),
            new(
                null, null, "SIZE", "Size", "TEXT", "TEXT", 1,
                [
                    new VariantConfigurationOptionValueDto(null, null, "S", "Small", "Small", null, 0, null),
                    new VariantConfigurationOptionValueDto(null, null, "M", "Medium", "Medium", null, 1, null),
                ]),
        };

        var variants = keys.Select((k, i) =>
        {
            var parts = k.Item1.Split(';');
            var color = parts[0].Split(':')[1];
            var size = parts[1].Split(':')[1];
            return new VariantConfigurationVariantDto(
                k.Item1,
                null,
                $"V{i + 1}",
                k.Item1,
                $"{color} / {size}",
                $"{name} - {color} / {size}",
                true,
                "ACTIVE",
                null,
                [
                    new VariantConfigurationSelectedValueDto(null, null, "Color", color),
                    new VariantConfigurationSelectedValueDto(null, null, "Size", size),
                ]);
        }).ToList();

        var request = new TenantAdminWizardProductCreateRequest
        {
            ProductName = name,
            ProductCode = $"E2EV{unique}",
            CategoryId = ctx.CategoryId,
            DesiredPublishActive = true,
            PosSellable = true,
            AllowOnlineSale = true,
            TrackInventory = true,
            ProductStructure = "VARIANT",
            VariantConfiguration = new VariantConfigurationDto(
                options,
                variants,
                Array.Empty<VariantConfigurationDeletedCombinationDto>()),
            BarcodeSkuConfiguration = new BarcodeSkuConfigurationDto(
                Array.Empty<Step5IdentifierTargetDto>(),
                keys.Select(k => new BarcodeSkuAssignmentDto(null, k.Item1, k.Item2, null, null, k.Item1)).ToList()),
            PricingTax = new PricingTaxConfigurationDto(
                100m,
                null,
                null,
                ctx.TaxId,
                true,
                keys.Select((k, i) => new VariantPriceConfigurationDto(
                    null,
                    k.Item1,
                    150m + (i * 10m))).ToList()),
        };

        var repo = new TenantAdminProductRepository(db, new NoOpCodeSequenceRepository());
        var result = await repo.CreateProductFromWizardAsync(
            ctx.TenantId, ctx.UserId, request, DateTimeOffset.UtcNow, CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error?.Message);
        var productId = result.Response!.ProductId;
        _createdProductIds.Add(productId);

        var product = await db.Products.AsNoTracking()
            .FirstAsync(p => p.TenantId == ctx.TenantId && p.Id == productId);
        Assert.Equal("VARIANT", product.ProductStructure);

        var created = await db.ProductVariants.AsNoTracking()
            .Where(v => v.TenantId == ctx.TenantId && v.ProductId == productId)
            .ToListAsync();
        Assert.Equal(4, created.Count);

        var priceItems = await db.PriceListItems.AsNoTracking()
            .Where(p => p.TenantId == ctx.TenantId &&
                        p.ProductId == productId &&
                        p.Status == "ACTIVE" &&
                        p.ProductVariantId != null)
            .ToListAsync();
        Assert.Equal(4, priceItems.Count);
        Assert.Equal(4, priceItems.Select(p => p.SellingPrice).Distinct().Count());
        Assert.DoesNotContain(priceItems, p => p.ProductVariantId is null);

        foreach (var (key, sku) in keys)
        {
            var match = created.SingleOrDefault(v => string.Equals(v.Sku, sku, StringComparison.Ordinal));
            Assert.NotNull(match);
            Assert.True(ProductVariantCombinationHashHelper.IsCanonicalSha256Hash(match!.OptionCombinationHash));
            Assert.Contains(priceItems, p => p.ProductVariantId == match.Id && p.SellingPrice > 0);
        }

        // VARIANT wizard must not require Step 3 units — unit settings may be absent.
        var unitSettings = await db.ProductUnitSettings.AsNoTracking()
            .CountAsync(u => u.TenantId == ctx.TenantId && u.ProductId == productId);
        Assert.Equal(0, unitSettings);
    }

    [Fact]
    public async Task CreateProductFromWizard_DuplicateSku_Does_Not_Leave_Partial_Product()
    {
        if (!await CanConnectAsync())
        {
            return;
        }

        await using var db = CreateDb();
        var ctx = await LoadSeedContextAsync(db);
        if (ctx is null)
        {
            return;
        }

        var unique = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + 3;
        var sku = $"E2E-DUP-{unique}";
        var name1 = $"E2E Dup First {unique}";
        var name2 = $"E2E Dup Second {unique}";

        var repo = new TenantAdminProductRepository(db, new NoOpCodeSequenceRepository());
        var first = await repo.CreateProductFromWizardAsync(
            ctx.TenantId,
            ctx.UserId,
            BuildSimpleRequest(ctx, name1, $"E2ED1{unique}", sku, null, "SINGLE_UNIT", null, null),
            DateTimeOffset.UtcNow,
            CancellationToken.None);
        Assert.True(first.IsSuccess, first.Error?.Message);

        var productCountBefore = await db.Products.CountAsync(p =>
            p.TenantId == ctx.TenantId && p.ProductName == name2);

        // Force collision by reusing SKU inside repository transaction (service-layer uniqueness bypassed).
        Exception? thrown = null;
        SaveProductDraftResult? second = null;
        try
        {
            second = await repo.CreateProductFromWizardAsync(
                ctx.TenantId,
                ctx.UserId,
                BuildSimpleRequest(ctx, name2, $"E2ED2{unique}", sku, null, "SINGLE_UNIT", null, null),
                DateTimeOffset.UtcNow,
                CancellationToken.None);
        }
        catch (Exception ex)
        {
            thrown = ex;
        }

        Assert.True(thrown is not null || (second is not null && !second.IsSuccess));
        var productCountAfter = await db.Products.CountAsync(p =>
            p.TenantId == ctx.TenantId && p.ProductName == name2);
        Assert.Equal(productCountBefore, productCountAfter);
    }

    private static TenantAdminWizardProductCreateRequest BuildSimpleRequest(
        SeedContext ctx,
        string name,
        string code,
        string sku,
        string? barcode,
        string unitModel,
        Guid? baseUnitOverride,
        Guid? purchaseUnitOverride)
    {
        var baseUnit = baseUnitOverride ?? ctx.UomId;
        return new TenantAdminWizardProductCreateRequest
        {
            ProductName = name,
            ProductCode = code,
            CategoryId = ctx.CategoryId,
            DesiredPublishActive = true,
            PosSellable = true,
            AllowOnlineSale = true,
            TrackInventory = true,
            ProductStructure = "SIMPLE",
            UnitModel = unitModel,
            ProductUnitId = baseUnit,
            BaseUnitId = baseUnit,
            SellingUnitId = baseUnit,
            PurchaseUnitId = purchaseUnitOverride ?? baseUnit,
            BarcodeSkuConfiguration = new BarcodeSkuConfigurationDto(
                Array.Empty<Step5IdentifierTargetDto>(),
                [
                    new BarcodeSkuAssignmentDto(
                        null,
                        name,
                        sku,
                        barcode,
                        null,
                        "SIMPLE_DEFAULT",
                        string.IsNullOrWhiteSpace(barcode) ? null : "CODE128")
                ]),
            PricingTax = new PricingTaxConfigurationDto(100m, 150m, 140m, ctx.TaxId, true),
        };
    }

    private sealed record SeedContext(Guid TenantId, Guid CategoryId, Guid TaxId, Guid UomId, Guid UserId);

    private static async Task<SeedContext?> LoadSeedContextAsync(EPosDbContext db)
    {
        // Prefer a tenant that already has ACTIVE category + tax + user (demo seed).
        var tenantId = await (
            from t in db.Tenants.AsNoTracking()
            where db.Categories.Any(c => c.TenantId == t.Id && c.Status == "ACTIVE")
                  && db.TaxClasses.Any(x => x.TenantId == t.Id && x.Status == "ACTIVE")
                  && db.TenantUsers.Any(u => u.TenantId == t.Id)
            select t.Id).FirstOrDefaultAsync();

        if (tenantId == Guid.Empty)
        {
            return null;
        }

        var categoryId = await db.Categories.AsNoTracking()
            .Where(c => c.TenantId == tenantId && c.Status == "ACTIVE")
            .Select(c => c.Id)
            .FirstAsync();
        var taxId = await db.TaxClasses.AsNoTracking()
            .Where(t => t.TenantId == tenantId && t.Status == "ACTIVE")
            .Select(t => t.Id)
            .FirstAsync();
        var uomId = await db.UnitOfMeasures.AsNoTracking()
            .Where(u => (u.TenantId == null || u.TenantId == tenantId) && u.Status == "ACTIVE")
            .Select(u => u.Id)
            .FirstOrDefaultAsync();
        var userId = await db.TenantUsers.AsNoTracking()
            .Where(u => u.TenantId == tenantId)
            .Select(u => u.Id)
            .FirstAsync();

        if (uomId == Guid.Empty)
        {
            return null;
        }

        return new SeedContext(tenantId, categoryId, taxId, uomId, userId);
    }

    private static EPosDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<EPosDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;
        return new EPosDbContext(options);
    }

    private static async Task<bool> CanConnectAsync()
    {
        try
        {
            await using var db = CreateDb();
            return await db.Database.CanConnectAsync();
        }
        catch
        {
            return false;
        }
    }

    private sealed class NoOpCodeSequenceRepository : ICodeSequenceRepository
    {
        public Task<string> GetNextCodeAsync(
            Guid tenantId,
            string sequenceKey,
            string prefix,
            int paddingLength,
            DateTimeOffset now,
            CancellationToken cancellationToken) =>
            Task.FromResult($"{prefix}{now.ToUnixTimeMilliseconds()}");
    }
}
