using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.TenantAdmin;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using E_POS.Infrastructure.Modules.Tenant.CatalogProduct.Repositories;
using E_POS.Infrastructure.Modules.Tenant.OutletTillDevice.Repositories;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace E_POS.IntegrationTests.CatalogProduct;

public sealed class TenantAdminProductCategoryMappingTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 27, 16, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task CategoryBelongsToTenantAsync_AllowsNestedActiveCategoryWithoutParentFilter()
    {
        var tenantId = Guid.NewGuid();
        var rootId = Guid.NewGuid();
        var level3Id = Guid.NewGuid();
        var level4Id = Guid.NewGuid();
        var level5Id = Guid.NewGuid();
        await using var db = CreateDb();
        db.Categories.AddRange(
            Category.Create(rootId, tenantId, null, "A", "A", "a", null, 1, CategoryConstants.ActiveStatus, null, Now),
            Category.Create(Guid.NewGuid(), tenantId, rootId, "B", "B", "b", null, 2, CategoryConstants.ActiveStatus, null, Now),
            Category.Create(level3Id, tenantId, rootId, "C", "C", "c", null, 3, CategoryConstants.ActiveStatus, null, Now),
            Category.Create(level4Id, tenantId, level3Id, "D", "D", "d", null, 4, CategoryConstants.ActiveStatus, null, Now),
            Category.Create(level5Id, tenantId, level4Id, "E", "E", "e", null, 5, CategoryConstants.ActiveStatus, null, Now));
        await db.SaveChangesAsync();
        var repository = new TenantAdminProductRepository(db, new CodeSequenceRepository(db));

        Assert.True(await repository.CategoryBelongsToTenantAsync(tenantId, level3Id, parentCategoryId: null, CancellationToken.None));
        Assert.True(await repository.CategoryBelongsToTenantAsync(tenantId, level5Id, parentCategoryId: null, CancellationToken.None));
        Assert.False(await repository.CategoryBelongsToTenantAsync(Guid.NewGuid(), level5Id, parentCategoryId: null, CancellationToken.None));
    }

    [Fact]
    public async Task CreateProductAsync_PersistsSelectedCategoryOnly_WithoutAncestorMappings()
    {
        var tenantId = Guid.NewGuid();
        var rootId = Guid.NewGuid();
        var childId = Guid.NewGuid();
        var level3Id = Guid.NewGuid();
        var unitId = Guid.NewGuid();
        await using var db = CreateDb();
        db.Categories.AddRange(
            Category.Create(rootId, tenantId, null, "A", "A", "a", null, 1, CategoryConstants.ActiveStatus, null, Now),
            Category.Create(childId, tenantId, rootId, "B", "B", "b", null, 2, CategoryConstants.ActiveStatus, null, Now),
            Category.Create(level3Id, tenantId, childId, "C", "C", "c", null, 3, CategoryConstants.ActiveStatus, null, Now));
        db.UnitOfMeasures.Add(UnitOfMeasure.Create(
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
        await db.SaveChangesAsync();

        var repository = new TenantAdminProductRepository(db, new CodeSequenceRepository(db));
        var created = await repository.CreateProductAsync(
            tenantId,
            Guid.NewGuid(),
            new TenantAdminProductCreateRequest
            {
                ProductName = "Nested Product",
                Sku = "NEST-1",
                CategoryId = level3Id,
                UnitType = "EA",
                SellingPrice = 10m,
                TrackInventory = false,
                HasVariants = false,
                Status = ProductConstants.ActiveStatus
            },
            unitId,
            Now,
            CancellationToken.None);

        var links = await db.ProductCategories.AsNoTracking().Where(x => x.ProductId == created.ProductId).ToListAsync();
        var link = Assert.Single(links);
        Assert.Equal(level3Id, link.CategoryId);
        Assert.True(link.IsPrimaryCategory);
        Assert.DoesNotContain(links, x => x.CategoryId == rootId);
        Assert.DoesNotContain(links, x => x.CategoryId == childId);
    }

    [Fact]
    public async Task CreateProductAsync_LegacySubCategoryId_MapsAsSelectedCategoryIdentityOnly()
    {
        var tenantId = Guid.NewGuid();
        var rootId = Guid.NewGuid();
        var selectedId = Guid.NewGuid();
        var unitId = Guid.NewGuid();
        await using var db = CreateDb();
        db.Categories.AddRange(
            Category.Create(rootId, tenantId, null, "A", "A", "a", null, 1, CategoryConstants.ActiveStatus, null, Now),
            Category.Create(selectedId, tenantId, rootId, "C", "C", "c", null, 2, CategoryConstants.ActiveStatus, null, Now));
        db.UnitOfMeasures.Add(UnitOfMeasure.Create(
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
        await db.SaveChangesAsync();

        var repository = new TenantAdminProductRepository(db, new CodeSequenceRepository(db));
        var created = await repository.CreateProductAsync(
            tenantId,
            Guid.NewGuid(),
            new TenantAdminProductCreateRequest
            {
                ProductName = "Legacy Sub",
                Sku = "NEST-2",
                CategoryId = rootId,
                SubCategoryId = selectedId,
                UnitType = "EA",
                SellingPrice = 10m,
                TrackInventory = false,
                HasVariants = false,
                Status = ProductConstants.ActiveStatus
            },
            unitId,
            Now,
            CancellationToken.None);

        var links = await db.ProductCategories.AsNoTracking().Where(x => x.ProductId == created.ProductId).ToListAsync();
        var link = Assert.Single(links);
        Assert.Equal(selectedId, link.CategoryId);
    }

    [Fact]
    public async Task GetDetail_SelectedNestedCategory_DoesNotInventSubCategoryEntity()
    {
        var tenantId = Guid.NewGuid();
        var rootId = Guid.NewGuid();
        var selectedId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        await using var db = CreateDb();
        db.Categories.AddRange(
            Category.Create(rootId, tenantId, null, "A", "A", "a", null, 1, CategoryConstants.ActiveStatus, null, Now),
            Category.Create(selectedId, tenantId, rootId, "C", "C", "c", null, 2, CategoryConstants.ActiveStatus, null, Now));
        db.Products.Add(Product.Create(
            productId,
            tenantId,
            "P-1",
            "Product 1",
            "p-1",
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
        db.ProductCategories.Add(ProductCategory.Create(Guid.NewGuid(), tenantId, productId, selectedId, true, 0, null, Now));
        await db.SaveChangesAsync();

        var repository = new TenantAdminProductRepository(db, new CodeSequenceRepository(db));
        var detail = await repository.GetDetailAsync(tenantId, productId, CancellationToken.None);

        Assert.NotNull(detail);
        Assert.Equal(selectedId, detail!.CategoryId);
        Assert.Equal("C", detail.CategoryName);
        Assert.Null(detail.SubCategoryId);
    }

    private static EPosDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<EPosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
}
