using E_POS.Application.Modules.Tenant.CatalogProduct.Services;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Contracts;
using E_POS.Domain.Modules.Platform.PlatformFoundation.Entities;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using E_POS.Domain.Modules.Tenant.Inventory.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using E_POS.Infrastructure.Modules.Tenant.CatalogProduct.Repositories;
using E_POS.Infrastructure.Persistence;
using E_POS.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace E_POS.IntegrationTests.CatalogProduct;

public sealed class TenantAdminProductSetupZeroWriteRepositoryTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 13, 14, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task GetSetupAsync_MissingSalesChannels_ReturnsCanonicalDefaults_DoesNotProvision_Idempotent()
    {
        var tenantId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        await using var db = CreateDbContext();
        var repository = new TenantAdminProductRepository(db, new NoOpCodeSequenceRepository());

        SeedProduct(db, tenantId, productId, currentSetupStep: 1);
        await db.SaveChangesAsync();

        var before = await CaptureCountsAsync(db, tenantId, productId);
        Assert.Equal(0, before.SalesChannels);
        Assert.Equal(0, before.Visibilities);

        var setup1 = await repository.GetSetupAsync(tenantId, productId, CancellationToken.None);
        Assert.NotNull(setup1);
        Assert.False(setup1!.PosSellable);
        Assert.False(setup1.AllowOnlineSale);
        Assert.Equal(1, setup1.CurrentSetupStep);
        Assert.Equal(before.RowVersion, setup1.RowVersion);

        var after1 = await CaptureCountsAsync(db, tenantId, productId);
        Assert.Equal(before, after1);

        var remapped = ScannerFirstSetupReadMapper.ApplyReadCompatibility(setup1, hasScanContextRow: false);
        Assert.Equal(2, remapped.CurrentSetupStep);
        Assert.Equal("LEGACY", remapped.ScanContext!.AcquisitionMode);

        var setup2 = await repository.GetSetupAsync(tenantId, productId, CancellationToken.None);
        Assert.NotNull(setup2);
        Assert.False(setup2!.PosSellable);
        Assert.False(setup2.AllowOnlineSale);

        var after2 = await CaptureCountsAsync(db, tenantId, productId);
        Assert.Equal(before, after2);
        Assert.Equal(1, (await db.Products.AsNoTracking().SingleAsync(x => x.Id == productId)).CurrentSetupStep);
    }

    [Fact]
    public async Task GetSetupAsync_ExistingVisibilityRows_ReturnsPersistedFlags_NoMutation()
    {
        var tenantId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        await using var db = CreateDbContext();
        var repository = new TenantAdminProductRepository(db, new NoOpCodeSequenceRepository());

        SeedProduct(db, tenantId, productId, currentSetupStep: 2);
        var (posId, onlineId) = SeedTenantSalesChannels(db, tenantId);
        db.ProductChannelVisibilities.Add(ProductChannelVisibility.Create(
            Guid.NewGuid(), tenantId, productId, null, posId, true, true, null, null, "ACTIVE", null, Now));
        db.ProductChannelVisibilities.Add(ProductChannelVisibility.Create(
            Guid.NewGuid(), tenantId, productId, null, onlineId, false, false, null, null, "ACTIVE", null, Now));
        await db.SaveChangesAsync();
        var before = await CaptureCountsAsync(db, tenantId, productId);

        var setup = await repository.GetSetupAsync(tenantId, productId, CancellationToken.None);

        Assert.True(setup!.PosSellable);
        Assert.False(setup.AllowOnlineSale);
        Assert.Equal(before, await CaptureCountsAsync(db, tenantId, productId));
    }

    [Fact]
    public async Task GetSetupAsync_PosExistsOnlineMissing_ProjectsPartialDefaults_NoWrite()
    {
        var tenantId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        await using var db = CreateDbContext();
        var repository = new TenantAdminProductRepository(db, new NoOpCodeSequenceRepository());

        SeedProduct(db, tenantId, productId, currentSetupStep: 3);
        var posId = SeedPosSalesChannelOnly(db, tenantId);
        db.ProductChannelVisibilities.Add(ProductChannelVisibility.Create(
            Guid.NewGuid(), tenantId, productId, null, posId, true, true, null, null, "ACTIVE", null, Now));
        await db.SaveChangesAsync();
        var before = await CaptureCountsAsync(db, tenantId, productId);
        Assert.Equal(1, before.SalesChannels);

        var setup = await repository.GetSetupAsync(tenantId, productId, CancellationToken.None);

        Assert.True(setup!.PosSellable);
        Assert.False(setup.AllowOnlineSale);
        Assert.Equal(before, await CaptureCountsAsync(db, tenantId, productId));
    }

    [Fact]
    public async Task GetSetupAsync_OnlineExistsPosMissing_ProjectsPartialDefaults_NoWrite()
    {
        var tenantId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        await using var db = CreateDbContext();
        var repository = new TenantAdminProductRepository(db, new NoOpCodeSequenceRepository());

        SeedProduct(db, tenantId, productId, currentSetupStep: 3);
        var onlineId = SeedOnlineSalesChannelOnly(db, tenantId);
        db.ProductChannelVisibilities.Add(ProductChannelVisibility.Create(
            Guid.NewGuid(), tenantId, productId, null, onlineId, true, true, null, null, "ACTIVE", null, Now));
        await db.SaveChangesAsync();
        var before = await CaptureCountsAsync(db, tenantId, productId);

        var setup = await repository.GetSetupAsync(tenantId, productId, CancellationToken.None);

        Assert.False(setup!.PosSellable);
        Assert.True(setup.AllowOnlineSale);
        Assert.Equal(before, await CaptureCountsAsync(db, tenantId, productId));
    }

    [Fact]
    public async Task GetSetupAsync_SalesChannelsExistWithoutVisibility_ReturnsFalseDefaults_DoesNotCreateVisibility()
    {
        var tenantId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        await using var db = CreateDbContext();
        var repository = new TenantAdminProductRepository(db, new NoOpCodeSequenceRepository());

        SeedProduct(db, tenantId, productId, currentSetupStep: 2);
        SeedTenantSalesChannels(db, tenantId);
        await db.SaveChangesAsync();
        var before = await CaptureCountsAsync(db, tenantId, productId);
        Assert.Equal(0, before.Visibilities);

        var setup = await repository.GetSetupAsync(tenantId, productId, CancellationToken.None);

        Assert.False(setup!.PosSellable);
        Assert.False(setup.AllowOnlineSale);
        Assert.Equal(before, await CaptureCountsAsync(db, tenantId, productId));
    }

    [Fact]
    public async Task GetSetupAsync_WithScanContext_DoesNotMutateScanContextOrRowVersion()
    {
        var tenantId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        await using var db = CreateDbContext();
        var repository = new TenantAdminProductRepository(db, new NoOpCodeSequenceRepository());

        SeedProduct(db, tenantId, productId, currentSetupStep: 2);
        db.ProductSetupScanContexts.Add(ProductSetupScanContext.Create(
            Guid.NewGuid(), tenantId, productId, "SCAN", "012345678905", "GTIN12",
            "UNKNOWN", null, "NOT_STARTED", null, null, null, null, Now));
        await db.SaveChangesAsync();
        var before = await CaptureCountsAsync(db, tenantId, productId);

        var setup = await repository.GetSetupAsync(tenantId, productId, CancellationToken.None);
        Assert.Equal("SCAN", setup!.ScanContext!.AcquisitionMode);
        Assert.Equal("012345678905", setup.ScanContext.CandidateIdentifier);
        Assert.Equal(before, await CaptureCountsAsync(db, tenantId, productId));
    }

    private static void SeedProduct(EPosDbContext db, Guid tenantId, Guid productId, int currentSetupStep)
    {
        var product = Product.Create(
            productId,
            tenantId,
            $"P-{productId.ToString()[..8]}",
            "Zero Write Product",
            "zero-write-product",
            "GOODS",
            ProductStructureConstants.Simple,
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

    private static (Guid PosId, Guid OnlineId) SeedTenantSalesChannels(EPosDbContext db, Guid tenantId)
    {
        EnsurePlatformChannels(db);
        var posId = Guid.NewGuid();
        var onlineId = Guid.NewGuid();
        db.SalesChannels.Add(SalesChannel.Create(
            posId, tenantId, PlatformSalesChannelSeedConstants.PosChannelId, "POS Storefront", "ACTIVE", 0, Now));
        db.SalesChannels.Add(SalesChannel.Create(
            onlineId, tenantId, PlatformSalesChannelSeedConstants.OnlineChannelId, "Online", "ACTIVE", 1, Now));
        return (posId, onlineId);
    }

    private static Guid SeedPosSalesChannelOnly(EPosDbContext db, Guid tenantId)
    {
        EnsurePlatformChannels(db);
        var posId = Guid.NewGuid();
        db.SalesChannels.Add(SalesChannel.Create(
            posId, tenantId, PlatformSalesChannelSeedConstants.PosChannelId, "POS Storefront", "ACTIVE", 0, Now));
        return posId;
    }

    private static Guid SeedOnlineSalesChannelOnly(EPosDbContext db, Guid tenantId)
    {
        EnsurePlatformChannels(db);
        var onlineId = Guid.NewGuid();
        db.SalesChannels.Add(SalesChannel.Create(
            onlineId, tenantId, PlatformSalesChannelSeedConstants.OnlineChannelId, "Online", "ACTIVE", 1, Now));
        return onlineId;
    }

    private static void EnsurePlatformChannels(EPosDbContext db)
    {
        if (!db.PlatformSalesChannels.Any(x => x.Id == PlatformSalesChannelSeedConstants.PosChannelId))
        {
            db.PlatformSalesChannels.Add(PlatformSalesChannel.Create(
                PlatformSalesChannelSeedConstants.PosChannelId,
                PlatformSalesChannelSeedConstants.PosChannelCode,
                PlatformSalesChannelSeedConstants.PosChannelName,
                PlatformSalesChannelSeedConstants.PosChannelType,
                Now));
        }

        if (!db.PlatformSalesChannels.Any(x => x.Id == PlatformSalesChannelSeedConstants.OnlineChannelId))
        {
            db.PlatformSalesChannels.Add(PlatformSalesChannel.Create(
                PlatformSalesChannelSeedConstants.OnlineChannelId,
                "ONLINE",
                "Online Store",
                "ONLINE",
                Now));
        }
    }

    private static async Task<DbSnapshot> CaptureCountsAsync(EPosDbContext db, Guid tenantId, Guid productId)
    {
        var product = await db.Products.AsNoTracking().SingleAsync(x => x.TenantId == tenantId && x.Id == productId);
        return new DbSnapshot(
            Products: await db.Products.CountAsync(x => x.TenantId == tenantId),
            SalesChannels: await db.SalesChannels.CountAsync(x => x.TenantId == tenantId),
            PlatformSalesChannels: await db.PlatformSalesChannels.CountAsync(),
            Visibilities: await db.ProductChannelVisibilities.CountAsync(x => x.TenantId == tenantId && x.ProductId == productId),
            ScanContexts: await db.ProductSetupScanContexts.CountAsync(x => x.TenantId == tenantId && x.ProductId == productId),
            Barcodes: await db.ProductBarcodes.CountAsync(x => x.TenantId == tenantId && x.ProductId == productId),
            Variants: await db.ProductVariants.CountAsync(x => x.TenantId == tenantId && x.ProductId == productId),
            RowVersion: product.RowVersion,
            CurrentSetupStep: product.CurrentSetupStep);
    }

    private static EPosDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<EPosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        var db = new EPosDbContext(options);
        db.UnitOfMeasures.Add(UnitOfMeasure.Create(
            Guid.NewGuid(),
            null,
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

    private sealed record DbSnapshot(
        int Products,
        int SalesChannels,
        int PlatformSalesChannels,
        int Visibilities,
        int ScanContexts,
        int Barcodes,
        int Variants,
        long RowVersion,
        int CurrentSetupStep);

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
