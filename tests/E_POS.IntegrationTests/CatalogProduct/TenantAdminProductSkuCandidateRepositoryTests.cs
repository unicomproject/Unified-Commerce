using E_POS.Application.Modules.Tenant.OutletTillDevice.Contracts;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using E_POS.Infrastructure.Modules.Tenant.CatalogProduct.Repositories;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace E_POS.IntegrationTests.CatalogProduct;

/// <summary>
/// B5 uniqueness owner: product_variants.sku via SkuExistsAsync (read-only, tenant-scoped).
/// </summary>
public sealed class TenantAdminProductSkuCandidateRepositoryTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 12, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task SkuExistsAsync_SameTenantActiveSku_ReturnsTrue()
    {
        var tenantId = Guid.NewGuid();
        await using var db = CreateDbContext();
        SeedVariant(db, tenantId, Guid.NewGuid(), Guid.NewGuid(), "SKU-HOUSELEMONJUICE");
        await db.SaveChangesAsync();

        var repository = new TenantAdminProductRepository(db, new NoOpCodeSequenceRepository());
        Assert.True(await repository.SkuExistsAsync(tenantId, "SKU-HOUSELEMONJUICE", null, CancellationToken.None));
    }

    [Fact]
    public async Task SkuExistsAsync_OtherTenantSameSku_ReturnsFalse()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        await using var db = CreateDbContext();
        SeedVariant(db, tenantB, Guid.NewGuid(), Guid.NewGuid(), "SKU-SHARED");
        await db.SaveChangesAsync();

        var repository = new TenantAdminProductRepository(db, new NoOpCodeSequenceRepository());
        Assert.False(await repository.SkuExistsAsync(tenantA, "SKU-SHARED", null, CancellationToken.None));
    }

    [Fact]
    public async Task SkuExistsAsync_CaseSensitive_MatchesOrdinalEquality()
    {
        var tenantId = Guid.NewGuid();
        await using var db = CreateDbContext();
        SeedVariant(db, tenantId, Guid.NewGuid(), Guid.NewGuid(), "sku-lower");
        await db.SaveChangesAsync();

        var repository = new TenantAdminProductRepository(db, new NoOpCodeSequenceRepository());
        Assert.True(await repository.SkuExistsAsync(tenantId, "sku-lower", null, CancellationToken.None));
        Assert.False(await repository.SkuExistsAsync(tenantId, "SKU-LOWER", null, CancellationToken.None));
    }

    [Fact]
    public async Task SkuExistsAsync_DoesNotMutateProductsVariantsOrScanContexts()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        await using var db = CreateDbContext();
        SeedVariant(db, tenantId, productId, variantId, "SKU-NB");
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
        var scanBefore = await db.ProductSetupScanContexts.CountAsync();

        var repository = new TenantAdminProductRepository(db, new NoOpCodeSequenceRepository());
        _ = await repository.SkuExistsAsync(tenantId, "SKU-NB", null, CancellationToken.None);
        _ = await repository.SkuExistsAsync(tenantId, "SKU-NB-2", null, CancellationToken.None);

        Assert.Equal(productsBefore, await db.Products.CountAsync());
        Assert.Equal(variantsBefore, await db.ProductVariants.CountAsync());
        Assert.Equal(scanBefore, await db.ProductSetupScanContexts.CountAsync());
        Assert.False(db.ChangeTracker.HasChanges());
    }

    private static void SeedVariant(
        EPosDbContext db,
        Guid tenantId,
        Guid productId,
        Guid variantId,
        string sku)
    {
        db.Products.Add(Product.Create(
            productId,
            tenantId,
            "P-SKU",
            "SKU Product",
            "p-sku",
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
            "SKU Product",
            sku,
            Guid.NewGuid(),
            Guid.NewGuid(),
            true,
            true,
            false,
            ProductConstants.ActiveStatus,
            null,
            Now));
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
