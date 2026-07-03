using System.Reflection;
using E_POS.Domain.Modules.CatalogProduct.Constants;
using E_POS.Domain.Modules.CatalogProduct.Entities;
using E_POS.Infrastructure.Modules.CatalogProduct.Repositories;
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
            Brand.Create(Guid.NewGuid(), tenantId, "ACME", "Acme", BrandConstants.ActiveStatus, Now),
            Brand.Create(Guid.NewGuid(), tenantId, "OLD", "Old", BrandConstants.DeletedStatus, Now),
            Brand.Create(Guid.NewGuid(), otherTenantId, "OTHER", "Other", BrandConstants.ActiveStatus, Now));
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
            Collection.Create(Guid.NewGuid(), tenantId, "SUMMER", "Summer", CollectionConstants.ActiveStatus, Now),
            Collection.Create(Guid.NewGuid(), tenantId, "OLD", "Old", CollectionConstants.DeletedStatus, Now),
            Collection.Create(Guid.NewGuid(), otherTenantId, "OTHER", "Other", CollectionConstants.ActiveStatus, Now));
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
        dbContext.Collections.Add(Collection.Create(collectionId, tenantId, "SUMMER", "Summer", CollectionConstants.ActiveStatus, Now));
        dbContext.Products.Add(CreateProduct(productId, tenantId, "PROD-1", "Product 1", "ACTIVE"));
        dbContext.ProductCollections.Add(CreateProductCollection(productId, collectionId));
        await dbContext.SaveChangesAsync();
        var repository = new CollectionRepository(dbContext);

        var result = await repository.HasProductLinksAsync(tenantId, collectionId, CancellationToken.None);

        Assert.True(result);
    }

    private static Product CreateProduct(Guid productId, Guid tenantId, string code, string name, string status)
    {
        var product = (Product)Activator.CreateInstance(typeof(Product), nonPublic: true)!;
        Set(product, "Id", productId);
        Set(product, "TenantId", tenantId);
        Set(product, "ProductCode", code);
        Set(product, "Name", name);
        Set<string?>(product, "Description", null);
        Set(product, "Status", status);
        Set(product, "CreatedAt", Now);
        Set<DateTimeOffset?>(product, "UpdatedAt", Now);
        return product;
    }

    private static ProductCollection CreateProductCollection(Guid productId, Guid collectionId)
    {
        var productCollection = (ProductCollection)Activator.CreateInstance(typeof(ProductCollection), nonPublic: true)!;
        Set(productCollection, "Id", Guid.NewGuid());
        Set(productCollection, "ProductId", productId);
        Set(productCollection, "CollectionId", collectionId);
        Set(productCollection, "CreatedAt", Now);
        Set<DateTimeOffset?>(productCollection, "UpdatedAt", Now);
        return productCollection;
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