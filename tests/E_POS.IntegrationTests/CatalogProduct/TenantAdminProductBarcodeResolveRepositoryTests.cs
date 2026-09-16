using E_POS.Application.Modules.Tenant.OutletTillDevice.Contracts;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using E_POS.Infrastructure.Modules.Tenant.CatalogProduct.Repositories;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace E_POS.IntegrationTests.CatalogProduct;

public sealed class TenantAdminProductBarcodeResolveRepositoryTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 12, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task FindBarcodeResolveMatchAsync_SameTenantProductBarcode_ReturnsProductMatch()
    {
        var tenantId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        await using var db = CreateDbContext();
        SeedProductWithBarcode(
            db,
            tenantId,
            productId,
            variantId,
            barcode: "4006381333931",
            productVariantId: null,
            productStatus: ProductConstants.ActiveStatus,
            identifierStandard: "GTIN13");
        await db.SaveChangesAsync();

        var repository = new TenantAdminProductRepository(db, new NoOpCodeSequenceRepository());
        var match = await repository.FindBarcodeResolveMatchAsync(tenantId, "4006381333931", CancellationToken.None);

        Assert.NotNull(match);
        Assert.Equal(productId, match!.ProductId);
        Assert.Null(match.VariantId);
        Assert.Equal("PRODUCT", match.MatchedAt);
        Assert.Equal("Resolve Product", match.ProductName);
        Assert.Equal("SKU-DEFAULT", match.Sku);
        Assert.Equal(1, match.MatchCount);
    }

    [Fact]
    public async Task FindBarcodeResolveMatchAsync_VariantBarcode_ReturnsVariantOwnership()
    {
        var tenantId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        await using var db = CreateDbContext();
        SeedProductWithBarcode(
            db,
            tenantId,
            productId,
            variantId,
            barcode: "012345678905",
            productVariantId: variantId,
            productStatus: ProductConstants.ActiveStatus,
            identifierStandard: null);
        await db.SaveChangesAsync();

        var repository = new TenantAdminProductRepository(db, new NoOpCodeSequenceRepository());
        var match = await repository.FindBarcodeResolveMatchAsync(tenantId, "012345678905", CancellationToken.None);

        Assert.NotNull(match);
        Assert.Equal(productId, match!.ProductId);
        Assert.Equal(variantId, match.VariantId);
        Assert.Equal("VARIANT", match.MatchedAt);
        Assert.Equal("Red / M", match.VariantLabel);
        Assert.Equal("SKU-VARIANT", match.Sku);
    }

    [Fact]
    public async Task FindBarcodeResolveMatchAsync_OtherTenantOnly_ReturnsNull()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        await using var db = CreateDbContext();
        SeedProductWithBarcode(
            db,
            tenantB,
            productId,
            variantId,
            barcode: "4006381333931",
            productVariantId: null,
            productStatus: ProductConstants.ActiveStatus,
            identifierStandard: "GTIN13");
        await db.SaveChangesAsync();

        var repository = new TenantAdminProductRepository(db, new NoOpCodeSequenceRepository());
        var match = await repository.FindBarcodeResolveMatchAsync(tenantA, "4006381333931", CancellationToken.None);

        Assert.Null(match);
    }

    [Fact]
    public async Task FindBarcodeResolveMatchAsync_InactiveProduct_StillMatchesForUniqueness()
    {
        var tenantId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        await using var db = CreateDbContext();
        SeedProductWithBarcode(
            db,
            tenantId,
            productId,
            variantId,
            barcode: "96385074",
            productVariantId: null,
            productStatus: ProductConstants.InactiveStatus,
            identifierStandard: "GTIN8");
        await db.SaveChangesAsync();

        var repository = new TenantAdminProductRepository(db, new NoOpCodeSequenceRepository());
        var match = await repository.FindBarcodeResolveMatchAsync(tenantId, "96385074", CancellationToken.None);

        Assert.NotNull(match);
        Assert.Equal(ProductConstants.InactiveStatus, match!.ProductStatus);
        Assert.Equal(productId, match.ProductId);
    }

    [Fact]
    public async Task FindBarcodeResolveMatchAsync_CorruptDuplicateOwners_ReportsMatchCount()
    {
        var tenantId = Guid.NewGuid();
        var productIdA = Guid.NewGuid();
        var productIdB = Guid.NewGuid();
        var variantA = Guid.NewGuid();
        var variantB = Guid.NewGuid();
        await using var db = CreateDbContext();
        SeedProductWithBarcode(
            db,
            tenantId,
            productIdA,
            variantA,
            barcode: "4006381333931",
            productVariantId: null,
            productStatus: ProductConstants.ActiveStatus,
            identifierStandard: "GTIN13");
        SeedProductWithBarcode(
            db,
            tenantId,
            productIdB,
            variantB,
            barcode: "4006381333931",
            productVariantId: null,
            productStatus: ProductConstants.ActiveStatus,
            identifierStandard: "GTIN13",
            productCode: "P-B",
            productName: "Other");
        await db.SaveChangesAsync();

        var repository = new TenantAdminProductRepository(db, new NoOpCodeSequenceRepository());
        var match = await repository.FindBarcodeResolveMatchAsync(tenantId, "4006381333931", CancellationToken.None);

        Assert.NotNull(match);
        Assert.Equal(2, match!.MatchCount);
    }

    [Fact]
    public async Task FindBarcodeResolveMatchAsync_DoesNotMutateCatalogueOrScanContext()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        await using var db = CreateDbContext();
        SeedProductWithBarcode(
            db,
            tenantId,
            productId,
            variantId,
            barcode: "4006381333931",
            productVariantId: null,
            productStatus: ProductConstants.ActiveStatus,
            identifierStandard: "GTIN13");
        db.ProductSetupScanContexts.Add(ProductSetupScanContext.Create(
            Guid.NewGuid(),
            tenantId,
            productId,
            "SCAN",
            "4006381333931",
            "GTIN13",
            "EAN13",
            null,
            null,
            null,
            null,
            null,
            userId,
            Now));
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var productsBefore = await db.Products.CountAsync();
        var variantsBefore = await db.ProductVariants.CountAsync();
        var barcodesBefore = await db.ProductBarcodes.CountAsync();
        var scanBefore = await db.ProductSetupScanContexts.CountAsync();

        var repository = new TenantAdminProductRepository(db, new NoOpCodeSequenceRepository());
        _ = await repository.FindBarcodeResolveMatchAsync(tenantId, "4006381333931", CancellationToken.None);
        _ = await repository.FindBarcodeResolveMatchAsync(tenantId, "012345678905", CancellationToken.None);

        Assert.Equal(productsBefore, await db.Products.CountAsync());
        Assert.Equal(variantsBefore, await db.ProductVariants.CountAsync());
        Assert.Equal(barcodesBefore, await db.ProductBarcodes.CountAsync());
        Assert.Equal(scanBefore, await db.ProductSetupScanContexts.CountAsync());
        Assert.False(db.ChangeTracker.HasChanges());
    }

    private static void SeedProductWithBarcode(
        EPosDbContext db,
        Guid tenantId,
        Guid productId,
        Guid variantId,
        string barcode,
        Guid? productVariantId,
        string productStatus,
        string? identifierStandard,
        string productCode = "P-A",
        string productName = "Resolve Product")
    {
        db.Products.Add(Product.Create(
            productId,
            tenantId,
            productCode,
            productName,
            productCode.ToLowerInvariant(),
            "STANDARD",
            "SIMPLE",
            null,
            null,
            null,
            null,
            null,
            true,
            true,
            productStatus,
            null,
            Now));

        db.ProductVariants.Add(ProductVariant.Create(
            variantId,
            tenantId,
            productId,
            productVariantId.HasValue ? "RED-M" : "DEFAULT",
            productVariantId.HasValue ? "Red / M" : productName,
            productVariantId.HasValue ? "SKU-VARIANT" : "SKU-DEFAULT",
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
            productVariantId,
            barcode,
            "UNKNOWN",
            null,
            1m,
            true,
            ProductConstants.ActiveStatus,
            null,
            Now,
            identifierStandard));
    }

    private static EPosDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<EPosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new EPosDbContext(options);
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
