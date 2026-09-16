using E_POS.Application.Modules.Tenant.CatalogProduct.Constants;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.TenantAdmin;
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

public sealed class TenantAdminProductPublishRepositoryTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 13, 18, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ReviewCreate_StaleRowVersion_ReturnsConcurrencyConflict_NoPublish()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        await using var db = CreateDbContext(tenantId);
        var repository = new TenantAdminProductRepository(db, new NoOpCodeSequenceRepository());

        SeedDraft(db, tenantId, productId);
        await db.SaveChangesAsync();
        var current = (await db.Products.SingleAsync(x => x.Id == productId)).RowVersion;

        var result = await repository.SaveProductDraftAsync(
            tenantId,
            userId,
            new SaveProductDraftCommand(
                productId,
                "Publish Product",
                "PUB-1",
                "publish-product",
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
                ProductWizardStage.ReviewCreate,
                7,
                current - 1,
                [],
                IsExplicitDraftSave: false,
                WizardAction: "PUBLISH"),
            Now,
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("product.concurrency_conflict", result.Error!.Code);
        var status = (await db.Products.AsNoTracking().SingleAsync(x => x.Id == productId)).Status;
        Assert.Equal(ProductConstants.DraftStatus, status);
    }

    private static void SeedDraft(EPosDbContext db, Guid tenantId, Guid productId)
    {
        var product = Product.Create(
            productId,
            tenantId,
            "PUB-1",
            "Publish Product",
            "publish-product",
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
        product.SetDraftSaved(7, Now);
        db.Products.Add(product);

        var uomId = db.UnitOfMeasures.Select(x => x.Id).First();
        var variant = ProductVariant.Create(
            Guid.NewGuid(),
            tenantId,
            productId,
            "DEFAULT",
            "Publish Product",
            null,
            uomId,
            uomId,
            isDefaultVariant: true,
            isSellable: true,
            allowFractionalQuantity: false,
            ProductConstants.DraftStatus,
            null,
            Now);
        variant.UpdateSku("SKU-PUB", Guid.NewGuid(), Now);
        db.ProductVariants.Add(variant);
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
