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

    private static Product CreateProduct(Guid productId, Guid tenantId, string code, string name, string status)
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
            brandId: null,
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

