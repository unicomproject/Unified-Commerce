using E_POS.Application.Modules.Tenant.CatalogProduct.Constants;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.TenantAdmin;
using E_POS.Application.Modules.Tenant.CatalogProduct.Services;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Contracts;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using E_POS.Domain.Modules.Tenant.Inventory.Entities;
using E_POS.Infrastructure.Modules.Tenant.CatalogProduct.Repositories;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace E_POS.IntegrationTests.CatalogProduct;

public sealed class TenantAdminProductScannerStep5RepositoryTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 13, 16, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task CompositeStep5_PersistsSkuAndBarcode_Idempotent_PreservesScanContext()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        await using var db = CreateDbContext(tenantId);
        var repository = new TenantAdminProductRepository(db, new NoOpCodeSequenceRepository());

        SeedScannerDraft(db, tenantId, productId, candidate: "012345678905");
        await db.SaveChangesAsync();
        var rowVersion = (await db.Products.SingleAsync(x => x.Id == productId)).RowVersion;
        var scanBefore = await db.ProductSetupScanContexts.AsNoTracking().SingleAsync(x => x.ProductId == productId);

        var command = BuildCompositeCommand(
            productId,
            rowVersion,
            targetStep: 5,
            assignments:
            [
                new BarcodeSkuAssignmentDto(
                    null,
                    "Default",
                    "SKU-FINAL",
                    "012345678905",
                    null,
                    null,
                    "UNKNOWN",
                    "GTIN12")
            ],
            isContinue: false);

        var first = await repository.SaveProductDraftAsync(tenantId, userId, command, Now, CancellationToken.None);
        Assert.True(first.IsSuccess, first.Error?.Message);
        Assert.Equal(ProductConstants.DraftStatus, first.Response!.Status);
        Assert.Equal(5, first.Response.CurrentSetupStep);

        var variant = await db.ProductVariants.SingleAsync(x => x.ProductId == productId);
        Assert.Equal("SKU-FINAL", variant.Sku);
        var barcode = await db.ProductBarcodes.SingleAsync(x => x.ProductId == productId && x.Status != ProductConstants.DeletedStatus);
        Assert.Equal("012345678905", barcode.Barcode);
        Assert.Equal("GTIN12", barcode.IdentifierStandard);
        Assert.Equal(variant.Id, barcode.ProductVariantId);

        var scanAfter = await db.ProductSetupScanContexts.AsNoTracking().SingleAsync(x => x.ProductId == productId);
        Assert.Equal(scanBefore.CandidateIdentifier, scanAfter.CandidateIdentifier);
        Assert.Equal(scanBefore.AcquisitionMode, scanAfter.AcquisitionMode);
        Assert.Equal(scanBefore.ExternalLookupStatus, scanAfter.ExternalLookupStatus);

        var second = await repository.SaveProductDraftAsync(
            tenantId,
            userId,
            BuildCompositeCommand(
                productId,
                first.Response.RowVersion,
                targetStep: 5,
                assignments:
                [
                    new BarcodeSkuAssignmentDto(
                        variant.Id,
                        "Default",
                        "SKU-FINAL",
                        "012345678905",
                        null,
                        variant.Id.ToString(),
                        "UNKNOWN",
                        "GTIN12")
                ],
                isContinue: false),
            Now.AddMinutes(1),
            CancellationToken.None);

        Assert.True(second.IsSuccess, second.Error?.Message);
        Assert.Equal(1, await db.ProductBarcodes.CountAsync(x =>
            x.ProductId == productId && x.Status != ProductConstants.DeletedStatus));
    }

    [Fact]
    public async Task CompositeStep5_DoesNotAutoCreateBarcodeFromScanCandidateAlone()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        await using var db = CreateDbContext(tenantId);
        var repository = new TenantAdminProductRepository(db, new NoOpCodeSequenceRepository());

        SeedScannerDraft(db, tenantId, productId, candidate: "4006381333931");
        await db.SaveChangesAsync();
        var rowVersion = (await db.Products.SingleAsync(x => x.Id == productId)).RowVersion;

        var result = await repository.SaveProductDraftAsync(
            tenantId,
            userId,
            BuildCompositeCommand(
                productId,
                rowVersion,
                targetStep: 5,
                assignments:
                [
                    new BarcodeSkuAssignmentDto(null, "Default", "SKU-ONLY", null, null, null)
                ],
                isContinue: false),
            Now,
            CancellationToken.None);

        Assert.True(result.IsSuccess, $"{result.Error?.Code}: {result.Error?.Message} | {string.Join("; ", result.Error?.FieldErrors?.Select(e => $"{e.Field}={e.Message}") ?? [])}");
        Assert.Equal("SKU-ONLY", (await db.ProductVariants.SingleAsync(x => x.ProductId == productId)).Sku);
        Assert.Empty(await db.ProductBarcodes
            .Where(x => x.ProductId == productId && x.Status != ProductConstants.DeletedStatus)
            .ToListAsync());
        Assert.Equal("4006381333931", (await db.ProductSetupScanContexts.SingleAsync(x => x.ProductId == productId)).CandidateIdentifier);
    }

    [Fact]
    public async Task CompositeStep5_SaveAndContinue_PersistsPublicStep6()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        await using var db = CreateDbContext(tenantId);
        var repository = new TenantAdminProductRepository(db, new NoOpCodeSequenceRepository());

        SeedScannerDraft(db, tenantId, productId, candidate: null);
        await db.SaveChangesAsync();
        var rowVersion = (await db.Products.SingleAsync(x => x.Id == productId)).RowVersion;

        var result = await repository.SaveProductDraftAsync(
            tenantId,
            userId,
            BuildCompositeCommand(
                productId,
                rowVersion,
                targetStep: 6,
                assignments:
                [
                    new BarcodeSkuAssignmentDto(null, "Default", "SKU-CONT", null, null, null)
                ],
                isContinue: true),
            Now,
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error?.Message);
        Assert.Equal(6, result.Response!.CurrentSetupStep);
        Assert.Equal(6, (await db.Products.AsNoTracking().SingleAsync(x => x.Id == productId)).CurrentSetupStep);
        Assert.Equal(ProductConstants.DraftStatus, result.Response.Status);
    }

    [Fact]
    public async Task CompositeStep5_ThenGetSetup_RoundTripsIdentifiers_ZeroWrite()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        await using var db = CreateDbContext(tenantId);
        var repository = new TenantAdminProductRepository(db, new NoOpCodeSequenceRepository());

        SeedScannerDraft(db, tenantId, productId, candidate: "012345678905");
        await db.SaveChangesAsync();
        var rowVersion = (await db.Products.SingleAsync(x => x.Id == productId)).RowVersion;

        var save = await repository.SaveProductDraftAsync(
            tenantId,
            userId,
            BuildCompositeCommand(
                productId,
                rowVersion,
                targetStep: 5,
                assignments:
                [
                    new BarcodeSkuAssignmentDto(
                        null,
                        "Default",
                        "SKU-RT",
                        "012345678905",
                        null,
                        null,
                        "UNKNOWN")
                ],
                isContinue: false),
            Now,
            CancellationToken.None);
        Assert.True(save.IsSuccess, save.Error?.Message);

        var barcodeCount = await db.ProductBarcodes.CountAsync();
        var productBefore = await db.Products.AsNoTracking().SingleAsync(x => x.Id == productId);

        var setup = await repository.GetSetupAsync(tenantId, productId, CancellationToken.None);
        var remapped = ScannerFirstSetupReadMapper.ApplyReadCompatibility(setup!, hasScanContextRow: true);

        Assert.Equal(5, remapped.CurrentSetupStep);
        Assert.Equal("SCAN", remapped.ScanContext!.AcquisitionMode);
        var assignment = Assert.Single(setup!.BarcodeSkuConfiguration!.Assignments!);
        Assert.Equal("SKU-RT", assignment.Sku);
        Assert.Equal("012345678905", assignment.Barcode);

        var productAfter = await db.Products.AsNoTracking().SingleAsync(x => x.Id == productId);
        Assert.Equal(productBefore.RowVersion, productAfter.RowVersion);
        Assert.Equal(productBefore.CurrentSetupStep, productAfter.CurrentSetupStep);
        Assert.Equal(barcodeCount, await db.ProductBarcodes.CountAsync());
    }

    [Fact]
    public async Task CompositeStep5_ClearsOptionalBarcode_WithoutTouchingScanContext()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        await using var db = CreateDbContext(tenantId);
        var repository = new TenantAdminProductRepository(db, new NoOpCodeSequenceRepository());

        SeedScannerDraft(db, tenantId, productId, candidate: "012345678905");
        await db.SaveChangesAsync();
        var rowVersion = (await db.Products.SingleAsync(x => x.Id == productId)).RowVersion;

        var first = await repository.SaveProductDraftAsync(
            tenantId,
            userId,
            BuildCompositeCommand(
                productId,
                rowVersion,
                targetStep: 5,
                assignments:
                [
                    new BarcodeSkuAssignmentDto(null, "Default", "SKU-CLR", "012345678905", null, null, "UNKNOWN")
                ],
                isContinue: false),
            Now,
            CancellationToken.None);
        Assert.True(first.IsSuccess, first.Error?.Message);
        var variantId = (await db.ProductVariants.SingleAsync(x => x.ProductId == productId)).Id;

        var second = await repository.SaveProductDraftAsync(
            tenantId,
            userId,
            BuildCompositeCommand(
                productId,
                first.Response!.RowVersion,
                targetStep: 5,
                assignments:
                [
                    new BarcodeSkuAssignmentDto(variantId, "Default", "SKU-CLR", null, null, variantId.ToString())
                ],
                isContinue: false),
            Now.AddMinutes(1),
            CancellationToken.None);

        Assert.True(second.IsSuccess, second.Error?.Message);
        Assert.Empty(await db.ProductBarcodes
            .Where(x => x.ProductId == productId && x.Status != ProductConstants.DeletedStatus)
            .ToListAsync());
        Assert.Equal("012345678905", (await db.ProductSetupScanContexts.SingleAsync(x => x.ProductId == productId)).CandidateIdentifier);
    }

    [Fact]
    public async Task CompositeStep5_AutoSkuSimple_PersistsBaseAndRehydratesAutoMode()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        await using var db = CreateDbContext(tenantId);
        var repository = new TenantAdminProductRepository(db, new NoOpCodeSequenceRepository());

        SeedScannerDraft(db, tenantId, productId, candidate: null, generatedSkuBase: "BEV-000128");
        SeedPrimaryCategory(db, tenantId, productId, "BEV");
        await db.SaveChangesAsync();
        var rowVersion = (await db.Products.SingleAsync(x => x.Id == productId)).RowVersion;

        var result = await repository.SaveProductDraftAsync(
            tenantId,
            userId,
            BuildCompositeCommand(
                productId,
                rowVersion,
                targetStep: 6,
                assignments: [],
                isContinue: true,
                autoSkuBase: "BEV-000128"),
            Now,
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error?.Message);
        Assert.Equal(
            "BEV-000128",
            (await db.ProductVariants.SingleAsync(x => x.ProductId == productId)).Sku);

        var setup = await repository.GetSetupAsync(tenantId, productId, CancellationToken.None);
        Assert.Equal("BEV-000128", setup!.ScanContext!.GeneratedSkuCandidate);
        Assert.Equal("AUTO", setup.BarcodeSkuConfiguration!.SkuMode);
        Assert.Equal("BEV-000128", Assert.Single(setup.BarcodeSkuConfiguration.Assignments!).Sku);
    }

    [Fact]
    public async Task CompositeStep5_AutoSkuVariant_UsesOneBaseAndCanonicalValueCodeOrder()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        await using var db = CreateDbContext(tenantId);
        var repository = new TenantAdminProductRepository(db, new NoOpCodeSequenceRepository());

        SeedScannerDraft(
            db,
            tenantId,
            productId,
            candidate: null,
            generatedSkuBase: "TSH-000125",
            structure: ProductStructureConstants.Variant);
        SeedPrimaryCategory(db, tenantId, productId, "TSH");
        await db.SaveChangesAsync();
        var rowVersion = (await db.Products.SingleAsync(x => x.Id == productId)).RowVersion;
        var variantConfiguration = BuildTShirtVariantConfiguration();

        var result = await repository.SaveProductDraftAsync(
            tenantId,
            userId,
            BuildCompositeCommand(
                productId,
                rowVersion,
                targetStep: 6,
                assignments: [],
                isContinue: true,
                autoSkuBase: "TSH-000125",
                structure: ProductStructureConstants.Variant,
                variantConfiguration: variantConfiguration),
            Now,
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error?.Message);
        var skus = await db.ProductVariants
            .Where(variant => variant.ProductId == productId && variant.IsSellable)
            .OrderBy(variant => variant.Sku)
            .Select(variant => variant.Sku)
            .ToListAsync();
        Assert.Equal(
            ["TSH-000125-BLK-M", "TSH-000125-BLK-S", "TSH-000125-WHT-M", "TSH-000125-WHT-S"],
            skus);
        Assert.All(skus, sku => Assert.StartsWith("TSH-000125-", sku, StringComparison.Ordinal));
    }

    [Fact]
    public async Task CompositeStep5_AutoSku_CategoryChanged_RequiresExplicitRegeneration()
    {
        var tenantId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        await using var db = CreateDbContext(tenantId);
        var repository = new TenantAdminProductRepository(db, new NoOpCodeSequenceRepository());

        SeedScannerDraft(
            db,
            tenantId,
            productId,
            candidate: null,
            generatedSkuBase: "TSH-000125");
        SeedPrimaryCategory(db, tenantId, productId, "BEV");
        await db.SaveChangesAsync();
        var rowVersion = (await db.Products.SingleAsync(x => x.Id == productId)).RowVersion;

        var result = await repository.SaveProductDraftAsync(
            tenantId,
            Guid.NewGuid(),
            BuildCompositeCommand(
                productId,
                rowVersion,
                targetStep: 5,
                assignments: [],
                isContinue: false,
                autoSkuBase: "TSH-000125"),
            Now,
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("product.auto_sku_category_changed", result.Error!.Code);
        Assert.Null((await db.ProductVariants.SingleAsync(x => x.ProductId == productId)).Sku);
    }

    private static SaveProductDraftCommand BuildCompositeCommand(
        Guid productId,
        long expectedRowVersion,
        int targetStep,
        IReadOnlyList<BarcodeSkuAssignmentDto> assignments,
        bool isContinue,
        string? autoSkuBase = null,
        string structure = ProductStructureConstants.Simple,
        VariantConfigurationDto? variantConfiguration = null) =>
        new(
            productId,
            "Scanner Step5 Product",
            "SCN-S5",
            "scanner-step5-product",
            structure,
            null,
            null,
            null,
            null,
            ProductConstants.DesiredPublishActive,
            true,
            false,
            false,
            false,
            false,
            false,
            ProductWizardStage.ProductConfiguration,
            targetStep,
            expectedRowVersion,
            [],
            IsExplicitDraftSave: !isContinue,
            WizardAction: isContinue ? "SAVE_AND_CONTINUE" : "SAVE_DRAFT",
            VariantConfiguration: variantConfiguration,
            BarcodeSkuConfiguration: new BarcodeSkuConfigurationDto(null, assignments),
            ApplyCompositeStep3Identifiers: true,
            AutoSkuBase: autoSkuBase);

    private static VariantConfigurationDto BuildTShirtVariantConfiguration()
    {
        var options = new[]
        {
            new VariantConfigurationOptionDto(
                null,
                null,
                "COLOUR",
                "Colour",
                "TEXT",
                "SELECT",
                0,
                [
                    new(null, null, "BLK", "Black", null, null, 0, null),
                    new(null, null, "WHT", "White", null, null, 1, null),
                ]),
            new VariantConfigurationOptionDto(
                null,
                null,
                "SIZE",
                "Size",
                "TEXT",
                "SELECT",
                1,
                [
                    new(null, null, "S", "Small", null, null, 0, null),
                    new(null, null, "M", "Medium", null, null, 1, null),
                ]),
        };

        var combinations = new[]
        {
            ("BLK-S", "Black / Small", "Black", "Small"),
            ("BLK-M", "Black / Medium", "Black", "Medium"),
            ("WHT-S", "White / Small", "White", "Small"),
            ("WHT-M", "White / Medium", "White", "Medium"),
        };
        var variants = combinations
            .Select(combination => new VariantConfigurationVariantDto(
                combination.Item1,
                null,
                null,
                null,
                combination.Item2,
                combination.Item2,
                true,
                ProductConstants.ActiveStatus,
                null,
                [
                    new(null, null, "Colour", combination.Item3),
                    new(null, null, "Size", combination.Item4),
                ]))
            .ToList();

        return new VariantConfigurationDto(options, variants, []);
    }

    private static void SeedPrimaryCategory(
        EPosDbContext db,
        Guid tenantId,
        Guid productId,
        string categoryCode)
    {
        var categoryId = Guid.NewGuid();
        db.Categories.Add(Category.Create(
            categoryId,
            tenantId,
            null,
            categoryCode,
            $"{categoryCode} Category",
            $"{categoryCode.ToLowerInvariant()}-category",
            null,
            0,
            ProductConstants.ActiveStatus,
            null,
            Now));
        db.ProductCategories.Add(ProductCategory.Create(
            Guid.NewGuid(),
            tenantId,
            productId,
            categoryId,
            true,
            0,
            null,
            Now));
    }

    private static void SeedScannerDraft(
        EPosDbContext db,
        Guid tenantId,
        Guid productId,
        string? candidate,
        string? generatedSkuBase = null,
        string structure = ProductStructureConstants.Simple)
    {
        var product = Product.Create(
            productId,
            tenantId,
            "SCN-S5",
            "Scanner Step5 Product",
            "scanner-step5-product",
            "GOODS",
            structure,
            null,
            null,
            null,
            null,
            null,
            true,
            true,
            ProductConstants.DraftStatus,
            null,
            Now);
        product.SetDraftSaved(5, Now);
        db.Products.Add(product);

        var uomId = db.UnitOfMeasures.Select(x => x.Id).First();
        if (structure == ProductStructureConstants.Simple)
        {
            db.ProductVariants.Add(ProductVariant.Create(
                Guid.NewGuid(),
                tenantId,
                productId,
                "DEFAULT",
                "Scanner Step5 Product",
                null,
                uomId,
                uomId,
                isDefaultVariant: true,
                isSellable: true,
                allowFractionalQuantity: false,
                ProductConstants.DraftStatus,
                null,
                Now));
        }

        db.ProductSetupScanContexts.Add(ProductSetupScanContext.Create(
            Guid.NewGuid(),
            tenantId,
            productId,
            candidate is null ? "NO_BARCODE" : "SCAN",
            candidate,
            candidate is null ? null : "GTIN12",
            candidate is null ? null : "UNKNOWN",
            candidate is null ? "OWN_MADE" : null,
            "NOT_STARTED",
            null,
            null,
            candidate is null ? generatedSkuBase ?? "SKU-NB-PREVIEW" : null,
            null,
            Now));
    }

    private static EPosDbContext CreateDbContext(Guid? seedTenantId = null)
    {
        var options = new DbContextOptionsBuilder<EPosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        var db = new EPosDbContext(options);
        db.UnitOfMeasures.Add(UnitOfMeasure.Create(
            Guid.NewGuid(),
            seedTenantId,
            "PIECE",
            "Piece",
            "COUNT",
            "pc",
            null,
            1m,
            ProductConstants.ActiveStatus,
            Now));
        db.SaveChanges();
        return db;
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
            Task.FromResult($"{prefix}1");
    }
}
