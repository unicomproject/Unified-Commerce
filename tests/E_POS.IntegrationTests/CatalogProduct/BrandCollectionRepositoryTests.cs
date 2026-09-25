using System.Reflection;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using E_POS.Infrastructure.Modules.Tenant.CatalogProduct.Repositories;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace E_POS.IntegrationTests.CatalogProduct;

public sealed class BrandCollectionRepositoryTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 3, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task BrandListAsync_ReturnsCurrentTenantNonDeletedBrandsOnly()
    {
        var tenantId = Guid.NewGuid();
        var otherTenantId = Guid.NewGuid();
        await using var dbContext = CreateDbContext();
        dbContext.Brands.AddRange(
            Brand.Create(Guid.NewGuid(), tenantId, "ACME", "Acme", "acme", null, null, BrandConstants.ActiveStatus, null, Now),
            Brand.Create(Guid.NewGuid(), tenantId, "OLD", "Old", "old", null, null, BrandConstants.DeletedStatus, null, Now),
            Brand.Create(Guid.NewGuid(), otherTenantId, "OTHER", "Other", "other", null, null, BrandConstants.ActiveStatus, null, Now));
        await dbContext.SaveChangesAsync();
        var repository = new BrandRepository(dbContext);

        var result = await repository.ListAsync(tenantId, 1, 50, null, CancellationToken.None);

        Assert.Equal(1, result.TotalCount);
        Assert.Equal("ACME", Assert.Single(result.Items).BrandCode);
    }

    [Fact]
    public async Task BrandDetailAsync_ReturnsDescriptionAndSortOrder()
    {
        var tenantId = Guid.NewGuid();
        var brandId = Guid.NewGuid();
        await using var dbContext = CreateDbContext();
        dbContext.Brands.Add(Brand.Create(brandId, tenantId, "ACME", "Acme", "acme", "Detail", null, BrandConstants.ActiveStatus, null, Now, 7));
        await dbContext.SaveChangesAsync();

        var result = await new BrandRepository(dbContext).GetByIdAsync(tenantId, brandId, false, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("Detail", result.Description);
        Assert.Equal(7, result.SortOrder);
    }

    [Fact]
    public async Task BrandListAsync_OrdersPaginatesAndCountsLifecycleProductsPerTenant()
    {
        var tenantId = Guid.NewGuid();
        var otherTenantId = Guid.NewGuid();
        var firstId = Guid.NewGuid();
        var secondId = Guid.NewGuid();
        await using var dbContext = CreateDbContext();
        dbContext.Brands.AddRange(
            Brand.Create(firstId, tenantId, "B", "Second", "second", null, null, BrandConstants.ActiveStatus, null, Now, 2),
            Brand.Create(secondId, tenantId, "A", "First", "first", null, null, BrandConstants.ActiveStatus, null, Now, 1),
            Brand.Create(Guid.NewGuid(), otherTenantId, "A", "Other", "other", null, null, BrandConstants.ActiveStatus, null, Now, 0));
        dbContext.Products.AddRange(
            CreateProduct(Guid.NewGuid(), tenantId, "DRAFT", "Draft", "DRAFT", secondId),
            CreateProduct(Guid.NewGuid(), tenantId, "ACTIVE", "Active", "ACTIVE", secondId),
            CreateProduct(Guid.NewGuid(), tenantId, "INACTIVE", "Inactive", "INACTIVE", secondId),
            CreateProduct(Guid.NewGuid(), tenantId, "ARCHIVED", "Archived", "ARCHIVED", secondId),
            CreateProduct(Guid.NewGuid(), tenantId, "INDEPENDENT", "Independent", "ACTIVE", firstId),
            CreateProduct(Guid.NewGuid(), otherTenantId, "OTHER", "Other", "ACTIVE", secondId));
        await dbContext.SaveChangesAsync();
        var repository = new BrandRepository(dbContext);

        var firstPage = await repository.ListAsync(tenantId, 1, 1, null, CancellationToken.None);
        var secondPage = await repository.ListAsync(tenantId, 2, 1, null, CancellationToken.None);

        Assert.Equal(2, firstPage.TotalCount);
        Assert.Equal(2, firstPage.TotalPages);
        Assert.Equal("A", Assert.Single(firstPage.Items).BrandCode);
        Assert.Equal(3, firstPage.Items[0].ProductCount);
        Assert.Equal("B", Assert.Single(secondPage.Items).BrandCode);
        Assert.Equal(1, secondPage.Items[0].ProductCount);
    }

    [Fact]
    public async Task CollectionListAsync_ReturnsCurrentTenantNonDeletedCollectionsOnly()
    {
        var tenantId = Guid.NewGuid();
        var otherTenantId = Guid.NewGuid();
        await using var dbContext = CreateDbContext();
        dbContext.Collections.AddRange(
            Collection.Create(Guid.NewGuid(), tenantId, "SUMMER", "Summer", "summer", null, "STANDARD", null, null, 0, CollectionConstants.ActiveStatus, null, Now),
            Collection.Create(Guid.NewGuid(), tenantId, "OLD", "Old", "old", null, "STANDARD", null, null, 0, CollectionConstants.DeletedStatus, null, Now),
            Collection.Create(Guid.NewGuid(), otherTenantId, "OTHER", "Other", "other", null, "STANDARD", null, null, 0, CollectionConstants.ActiveStatus, null, Now));
        await dbContext.SaveChangesAsync();
        var repository = new CollectionRepository(dbContext);

        var result = await repository.ListAsync(tenantId, 1, 50, null, CancellationToken.None);

        Assert.Equal(1, result.TotalCount);
        Assert.Equal("SUMMER", Assert.Single(result.Items).CollectionCode);
    }

    [Fact]
    public async Task HasProductLinksAsync_WhenActiveTenantProductLinked_ReturnsTrue()
    {
        var tenantId = Guid.NewGuid();
        var collectionId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        await using var dbContext = CreateDbContext();
        dbContext.Collections.Add(Collection.Create(collectionId, tenantId, "SUMMER", "Summer", "summer", null, "STANDARD", null, null, 0, CollectionConstants.ActiveStatus, null, Now));
        dbContext.Products.Add(CreateProduct(productId, tenantId, "PROD-1", "Product 1", "ACTIVE"));
        dbContext.ProductCollections.Add(CreateProductCollection(productId, collectionId));
        await dbContext.SaveChangesAsync();
        var repository = new CollectionRepository(dbContext);

        var result = await repository.HasProductLinksAsync(tenantId, collectionId, CancellationToken.None);

        Assert.True(result);
    }

    private static Product CreateProduct(Guid productId, Guid tenantId, string code, string name, string status, Guid? brandId = null)
    {
        return Product.Create(
            id: productId,
            tenantId: tenantId,
            productCode: code,
            productName: name,
            productSlug: name.ToLowerInvariant(),
            productType: "STANDARD",
            productStructure: "SIMPLE",
            businessTypeId: null,
            brandId: brandId,
            returnPolicyId: null,
            shortDescription: null,
            longDescription: null,
            isSellable: true,
            isTaxable: true,
            status: status,
            createdByTenantUserId: null,
            now: Now);
    }

    private static ProductCollection CreateProductCollection(Guid productId, Guid collectionId)
    {
        return ProductCollection.Create(
            id: Guid.NewGuid(),
            tenantId: Guid.Empty,
            productId: productId,
            collectionId: collectionId,
            sortOrder: 0,
            createdByTenantUserId: null,
            now: Now);
    }

    private static void Set<T>(object entity, string propertyName, T value)
    {
        entity.GetType()
            .GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
            .SetValue(entity, value);
    }

    private static EPosDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<EPosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new EPosDbContext(options);
    }
}

