using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.TenantAdmin;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Contracts;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;
using E_POS.Infrastructure.Modules.Tenant.CatalogProduct.Repositories;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace E_POS.IntegrationTests.CatalogProduct;

/// <summary>
/// Live PostgreSQL verification for Chunk 6 wizard-create.
/// Soft-skips when DB/seed prerequisites are unavailable.
/// </summary>
public sealed class WizardProductCreatePostgreSqlTests
{
    private const string ConnectionString =
        "Host=localhost;Port=5434;Database=UnifiedCommerceDb;Username=postgres;Password=Nive@123";

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

        var result = await repo.CreateProductFromWizardAsync(
            ctx.TenantId, ctx.UserId, request, DateTimeOffset.UtcNow, CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error?.Message);
        Assert.NotNull(result.Response);

        var productId = result.Response!.ProductId;

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

        // Product List query must include the newly created ACTIVE product.
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
            PricingTax = new PricingTaxConfigurationDto(100m, 150m, 140m, ctx.TaxId, true),
        };

        var repo = new TenantAdminProductRepository(db, new NoOpCodeSequenceRepository());
        var result = await repo.CreateProductFromWizardAsync(
            ctx.TenantId, ctx.UserId, request, DateTimeOffset.UtcNow, CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error?.Message);
        var productId = result.Response!.ProductId;

        var product = await db.Products.AsNoTracking()
            .FirstAsync(p => p.TenantId == ctx.TenantId && p.Id == productId);
        Assert.Equal("VARIANT", product.ProductStructure);

        var created = await db.ProductVariants.AsNoTracking()
            .Where(v => v.TenantId == ctx.TenantId && v.ProductId == productId)
            .ToListAsync();
        Assert.Equal(4, created.Count);

        foreach (var (key, sku) in keys)
        {
            var match = created.SingleOrDefault(v =>
                string.Equals(v.OptionCombinationHash?.Trim(), key, StringComparison.Ordinal));
            Assert.NotNull(match);
            Assert.Equal(sku, match!.Sku);
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
                    new BarcodeSkuAssignmentDto(null, name, sku, barcode, null, "SIMPLE_DEFAULT")
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
