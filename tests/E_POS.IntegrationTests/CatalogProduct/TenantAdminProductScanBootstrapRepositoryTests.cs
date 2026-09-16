using E_POS.Application.Modules.Tenant.CatalogProduct.Constants;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.TenantAdmin;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Contracts;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using E_POS.Infrastructure.Modules.Tenant.CatalogProduct.Repositories;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace E_POS.IntegrationTests.CatalogProduct;

public sealed class TenantAdminProductScanBootstrapRepositoryTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 13, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task SaveProductDraftAsync_ScanBootstrap_CreatesProductAndScanContext_AtStep2_NoBarcodeRow()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        await using var db = CreateDbContext();
        var repository = new TenantAdminProductRepository(db, new NoOpCodeSequenceRepository());

        var command = BuildScanCommand(
            acquisitionMode: "SCAN",
            candidate: "012345678905",
            identifierStandard: "GTIN12",
            symbology: "UNKNOWN",
            externalStatus: "NOT_STARTED");

        var result = await repository.SaveProductDraftAsync(
            tenantId, userId, command, Now, CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error?.Message);
        Assert.Equal(2, result.Response!.CurrentSetupStep);
        Assert.Equal(ProductConstants.DraftStatus, result.Response.Status);

        var productId = result.Response.ProductId;
        var product = await db.Products.AsNoTracking()
            .SingleAsync(x => x.TenantId == tenantId && x.Id == productId);
        Assert.Equal(2, product.CurrentSetupStep);

        var context = await db.ProductSetupScanContexts.AsNoTracking()
            .SingleAsync(x => x.TenantId == tenantId && x.ProductId == productId);
        Assert.Equal("SCAN", context.AcquisitionMode);
        Assert.Equal("012345678905", context.CandidateIdentifier);
        Assert.Equal("GTIN12", context.IdentifierStandard);
        Assert.Equal("UNKNOWN", context.SymbologyHint);
        Assert.Equal("NOT_STARTED", context.ExternalLookupStatus);

        Assert.Empty(await db.ProductBarcodes.AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.ProductId == productId)
            .ToListAsync());

        Assert.True(await repository.HasScanContextAsync(tenantId, productId, CancellationToken.None));
    }

    [Fact]
    public async Task SaveProductDraftAsync_NoBarcode_PersistsReasonAndSkuCandidate_NoFakeBarcode()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        await using var db = CreateDbContext();
        var repository = new TenantAdminProductRepository(db, new NoOpCodeSequenceRepository());

        var command = new SaveProductDraftCommand(
            null,
            "No Barcode Product",
            string.Empty,
            "no-barcode-product",
            ProductStructureConstants.Simple,
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
            ProductWizardStage.BasicDetails,
            TargetSetupStep: 2,
            ExpectedRowVersion: null,
            StagedMediaAssetIds: [],
            ScanBootstrap: new ProductSetupScanBootstrapPersistence(
                "NO_BARCODE",
                "CONTINUE_TO_BASIC_DETAILS",
                null,
                null,
                null,
                "OWN_MADE",
                "NOT_STARTED",
                null,
                null,
                "SKU-NB-PREVIEW",
                false));

        var result = await repository.SaveProductDraftAsync(
            tenantId, userId, command, Now, CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error?.Message);
        var productId = result.Response!.ProductId;
        var context = await db.ProductSetupScanContexts.SingleAsync(x => x.ProductId == productId);
        Assert.Equal("OWN_MADE", context.NoBarcodeReason);
        Assert.Equal("SKU-NB-PREVIEW", context.GeneratedSkuCandidate);
        Assert.Null(context.CandidateIdentifier);
        Assert.Empty(await db.ProductBarcodes.Where(x => x.ProductId == productId).ToListAsync());
        Assert.Empty(await db.ProductVariants.Where(x => x.ProductId == productId).ToListAsync());
    }

    [Fact]
    public async Task SaveProductDraftAsync_DuplicateBarcode_CreatesNeither()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var existingProductId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        await using var db = CreateDbContext();
        SeedOwnedBarcode(db, tenantId, existingProductId, variantId, "4006381333931");
        await db.SaveChangesAsync();

        var productsBefore = await db.Products.CountAsync();
        var scanBefore = await db.ProductSetupScanContexts.CountAsync();

        var repository = new TenantAdminProductRepository(db, new NoOpCodeSequenceRepository());
        var result = await repository.SaveProductDraftAsync(
            tenantId,
            userId,
            BuildScanCommand("SCAN", "4006381333931", "GTIN13", "EAN13", "NOT_STARTED"),
            Now,
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("product.duplicate_barcode", result.Error!.Code);
        Assert.Equal(productsBefore, await db.Products.CountAsync());
        Assert.Equal(scanBefore, await db.ProductSetupScanContexts.CountAsync());
    }

    [Fact]
    public async Task SaveProductDraftAsync_OtherTenantBarcode_DoesNotBlock()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        await using var db = CreateDbContext();
        SeedOwnedBarcode(db, tenantB, Guid.NewGuid(), Guid.NewGuid(), "4006381333931");
        await db.SaveChangesAsync();

        var repository = new TenantAdminProductRepository(db, new NoOpCodeSequenceRepository());
        var result = await repository.SaveProductDraftAsync(
            tenantA,
            Guid.NewGuid(),
            BuildScanCommand("SCAN", "4006381333931", "GTIN13", "EAN13", "NOT_STARTED"),
            Now,
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error?.Message);
        Assert.Equal(2, result.Response!.CurrentSetupStep);
    }

    private static SaveProductDraftCommand BuildScanCommand(
        string acquisitionMode,
        string candidate,
        string identifierStandard,
        string symbology,
        string externalStatus) =>
        new(
            null,
            "Scanner Product",
            string.Empty,
            "scanner-product",
            ProductStructureConstants.Simple,
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
            ProductWizardStage.BasicDetails,
            TargetSetupStep: 2,
            ExpectedRowVersion: null,
            StagedMediaAssetIds: [],
            ScanBootstrap: new ProductSetupScanBootstrapPersistence(
                acquisitionMode,
                "CONTINUE_WITH_BARCODE",
                candidate,
                identifierStandard,
                symbology,
                null,
                externalStatus,
                null,
                null,
                null,
                false));

    private static void SeedOwnedBarcode(
        EPosDbContext db,
        Guid tenantId,
        Guid productId,
        Guid variantId,
        string barcode)
    {
        db.Products.Add(Product.Create(
            productId,
            tenantId,
            "P-OWN",
            "Owned",
            "owned",
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

        db.ProductVariants.Add(ProductVariant.Create(
            variantId,
            tenantId,
            productId,
            "DEFAULT",
            "Owned",
            "SKU-OWN",
            Guid.NewGuid(),
            Guid.NewGuid(),
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
            null,
            barcode,
            "UNKNOWN",
            null,
            1m,
            true,
            ProductConstants.ActiveStatus,
            null,
            Now,
            "GTIN13"));
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
