using System.Text.Json;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.ExternalLookup;
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

public sealed class TenantAdminProductSetupHydrationRepositoryTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 13, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task GetSetupAsync_WithScanContext_HydratesTypedPrefill_PreservesLeadingZero()
    {
        var tenantId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        await using var db = CreateDbContext(tenantId);
        var repository = new TenantAdminProductRepository(db, new NoOpCodeSequenceRepository());

        SeedProduct(db, tenantId, productId, ProductStructureConstants.Simple, currentSetupStep: 2);
        var suggestion = new ExternalProductSuggestion(
            "Hydrated Name",
            null,
            "Acme",
            null,
            null,
            null,
            null,
            null,
            null,
            "012345678905",
            "GTIN12");
        db.ProductSetupScanContexts.Add(ProductSetupScanContext.Create(
            Guid.NewGuid(),
            tenantId,
            productId,
            "SCAN",
            "012345678905",
            "GTIN12",
            "UNKNOWN",
            null,
            "FOUND",
            "ext:abc",
            JsonSerializer.Serialize(suggestion),
            null,
            null,
            Now));
        await db.SaveChangesAsync();

        var setup = await repository.GetSetupAsync(tenantId, productId, CancellationToken.None);

        Assert.NotNull(setup);
        Assert.Equal(2, setup!.CurrentSetupStep);
        Assert.NotNull(setup.ScanContext);
        Assert.Equal("SCAN", setup.ScanContext!.AcquisitionMode);
        Assert.Equal("012345678905", setup.ScanContext.CandidateIdentifier);
        Assert.Equal("GTIN12", setup.ScanContext.IdentifierStandard);
        Assert.Equal("FOUND", setup.ScanContext.ExternalLookupStatus);
        Assert.Equal("ext:abc", setup.ScanContext.ExternalSourceReference);
        Assert.Equal("Hydrated Name", setup.ScanContext.NormalizedPrefill!.ProductName);
        Assert.Equal("012345678905", setup.ScanContext.NormalizedPrefill.PrimaryGtin);
    }

    [Fact]
    public async Task GetSetupAsync_NoBarcodeContext_RetainsReasonAndSkuCandidate()
    {
        var tenantId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        await using var db = CreateDbContext(tenantId);
        var repository = new TenantAdminProductRepository(db, new NoOpCodeSequenceRepository());

        SeedProduct(db, tenantId, productId, ProductStructureConstants.Simple, currentSetupStep: 2);
        db.ProductSetupScanContexts.Add(ProductSetupScanContext.Create(
            Guid.NewGuid(),
            tenantId,
            productId,
            "NO_BARCODE",
            null,
            null,
            null,
            "OWN_MADE",
            "NOT_STARTED",
            null,
            null,
            "SKU-NB-PREVIEW",
            null,
            Now));
        await db.SaveChangesAsync();

        var setup = await repository.GetSetupAsync(tenantId, productId, CancellationToken.None);

        Assert.Equal("NO_BARCODE", setup!.ScanContext!.AcquisitionMode);
        Assert.Null(setup.ScanContext.CandidateIdentifier);
        Assert.Equal("OWN_MADE", setup.ScanContext.NoBarcodeReason);
        Assert.Equal("SKU-NB-PREVIEW", setup.ScanContext.GeneratedSkuCandidate);
    }

    [Fact]
    public async Task GetSetupAsync_LegacyWithoutScanContext_ReturnsNullScanContext_DoesNotRewriteStep()
    {
        var tenantId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        await using var db = CreateDbContext(tenantId);
        var repository = new TenantAdminProductRepository(db, new NoOpCodeSequenceRepository());

        SeedProduct(db, tenantId, productId, ProductStructureConstants.Simple, currentSetupStep: 1);
        await db.SaveChangesAsync();
        var rowVersionBefore = (await db.Products.AsNoTracking().SingleAsync(x => x.Id == productId)).RowVersion;

        var setup = await repository.GetSetupAsync(tenantId, productId, CancellationToken.None);

        Assert.NotNull(setup);
        Assert.Equal(1, setup!.CurrentSetupStep);
        Assert.Null(setup.ScanContext);

        var productAfter = await db.Products.AsNoTracking().SingleAsync(x => x.Id == productId);
        Assert.Equal(1, productAfter.CurrentSetupStep);
        Assert.Equal(rowVersionBefore, productAfter.RowVersion);
    }

    [Fact]
    public async Task GetSetupAsync_LegacyOldStep5_PreservesSkuAndBarcodeAssignments()
    {
        var tenantId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        await using var db = CreateDbContext(tenantId);
        var repository = new TenantAdminProductRepository(db, new NoOpCodeSequenceRepository());

        SeedProduct(db, tenantId, productId, ProductStructureConstants.Simple, currentSetupStep: 5);
        var uomId = await db.UnitOfMeasures.Select(x => x.Id).FirstAsync();
        db.ProductVariants.Add(ProductVariant.Create(
            variantId,
            tenantId,
            productId,
            "DEFAULT",
            "Default",
            "SKU-LEGACY-5",
            uomId,
            uomId,
            true,
            true,
            false,
            ProductConstants.ActiveStatus,
            null,
            Now));
        db.ProductBarcodes.Add(ProductBarcode.Create(
            Guid.NewGuid(),
            tenantId,
            productId,
            variantId,
            "4006381333931",
            "EAN13",
            null,
            1m,
            true,
            ProductConstants.ActiveStatus,
            null,
            Now,
            "GTIN13"));
        await db.SaveChangesAsync();
        var barcodeCountBefore = await db.ProductBarcodes.CountAsync();

        var setup = await repository.GetSetupAsync(tenantId, productId, CancellationToken.None);
        var remapped = ScannerFirstSetupReadMapper.ApplyReadCompatibility(setup!, hasScanContextRow: false);

        Assert.Equal(3, remapped.CurrentSetupStep);
        Assert.Equal("LEGACY", remapped.ScanContext!.AcquisitionMode);
        Assert.NotNull(setup!.BarcodeSkuConfiguration);
        var assignment = Assert.Single(setup.BarcodeSkuConfiguration!.Assignments!);
        Assert.Equal(variantId, assignment.ProductVariantId);
        Assert.Equal("SKU-LEGACY-5", assignment.Sku);
        Assert.Equal("4006381333931", assignment.Barcode);
        Assert.Equal("GTIN13", assignment.IdentifierStandard);
        Assert.Equal(barcodeCountBefore, await db.ProductBarcodes.CountAsync());
        Assert.Equal(5, (await db.Products.AsNoTracking().SingleAsync(x => x.Id == productId)).CurrentSetupStep);
    }

    [Fact]
    public async Task GetSetupAsync_InitialTracking_RehydratesPersistedValues()
    {
        var tenantId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        await using var db = CreateDbContext(tenantId);
        var repository = new TenantAdminProductRepository(db, new NoOpCodeSequenceRepository());

        SeedProduct(db, tenantId, productId, ProductStructureConstants.Simple, currentSetupStep: 3);
        db.ProductSetupInitialTrackings.Add(ProductSetupInitialTracking.Create(
            Guid.NewGuid(),
            tenantId,
            productId,
            "BATCH-A",
            new DateOnly(2027, 1, 15),
            "SER-9",
            null,
            Now));
        await db.SaveChangesAsync();

        var setup = await repository.GetSetupAsync(tenantId, productId, CancellationToken.None);

        Assert.Equal("BATCH-A", setup!.InitialBatchNumber);
        Assert.Equal(new DateOnly(2027, 1, 15), setup.InitialExpiryDate);
        Assert.Equal("SER-9", setup.InitialSerialNumber);
    }

    [Fact]
    public async Task GetSetupAsync_WrongTenant_ReturnsNull()
    {
        var tenantId = Guid.NewGuid();
        var otherTenant = Guid.NewGuid();
        var productId = Guid.NewGuid();
        await using var db = CreateDbContext(tenantId);
        var repository = new TenantAdminProductRepository(db, new NoOpCodeSequenceRepository());

        SeedProduct(db, tenantId, productId, ProductStructureConstants.Simple, currentSetupStep: 2);
        db.ProductSetupScanContexts.Add(ProductSetupScanContext.Create(
            Guid.NewGuid(),
            tenantId,
            productId,
            "SCAN",
            "012345678905",
            "GTIN12",
            null,
            null,
            "NOT_STARTED",
            null,
            null,
            null,
            null,
            Now));
        await db.SaveChangesAsync();

        var setup = await repository.GetSetupAsync(otherTenant, productId, CancellationToken.None);

        Assert.Null(setup);
    }

    [Fact]
    public async Task GetSetupAsync_ServiceCompatibility_LegacyRemapDoesNotMutateDb()
    {
        var tenantId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        await using var db = CreateDbContext(tenantId);
        var repository = new TenantAdminProductRepository(db, new NoOpCodeSequenceRepository());

        SeedProduct(db, tenantId, productId, ProductStructureConstants.Simple, currentSetupStep: 3);
        await db.SaveChangesAsync();
        var before = await db.Products.AsNoTracking().SingleAsync(x => x.Id == productId);

        var setup = await repository.GetSetupAsync(tenantId, productId, CancellationToken.None);
        var compatible = ScannerFirstSetupReadMapper.ApplyReadCompatibility(setup!, hasScanContextRow: setup!.ScanContext is not null);

        Assert.Equal(3, compatible.CurrentSetupStep);
        Assert.Equal(3, compatible.TargetSetupStep);

        var after = await db.Products.AsNoTracking().SingleAsync(x => x.Id == productId);
        Assert.Equal(3, after.CurrentSetupStep);
        Assert.Equal(before.RowVersion, after.RowVersion);
    }

    private static void SeedProduct(
        EPosDbContext db,
        Guid tenantId,
        Guid productId,
        string structure,
        int currentSetupStep)
    {
        var product = Product.Create(
            productId,
            tenantId,
            $"P-{productId.ToString()[..8]}",
            "Setup Product",
            "setup-product",
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
        product.SetDraftSaved(currentSetupStep, Now);
        db.Products.Add(product);
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
