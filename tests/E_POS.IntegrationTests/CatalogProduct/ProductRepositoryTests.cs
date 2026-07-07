using System.Reflection;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using E_POS.Domain.Modules.Tenant.PricingTax.Entities;
using E_POS.Infrastructure.Modules.Tenant.CatalogProduct.Repositories;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace E_POS.IntegrationTests.CatalogProduct;

public sealed class ProductRepositoryTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 4, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ProductCodeExistsAsync_ReturnsTrue_WhenCodeExistsForTenant()
    {
        var tenantId = Guid.NewGuid();
        var code = "PROD-123";
        await using var dbContext = CreateDbContext();
        var product = Product.Create(
            Guid.NewGuid(), 
            tenantId, 
            code, 
            "Test Product", 
            "test-product",
            "STANDARD",
            "SIMPLE",
            null, null, null,
            null, null,
            true, true,
            ProductConstants.ActiveStatus, 
            null, 
            Now);
        dbContext.Products.Add(product);
        await dbContext.SaveChangesAsync();
        var repository = new ProductRepository(dbContext);

        var exists = await repository.ProductCodeExistsAsync(tenantId, "PROD-123", null, CancellationToken.None);

        Assert.True(exists);
    }

    [Fact]
    public async Task SkuExistsAsync_ReturnsTrue_WhenSkuExistsForTenant()
    {
        var tenantId = Guid.NewGuid();
        var sku = "SKU-456";
        await using var dbContext = CreateDbContext();
        var variant = ProductVariant.Create(
            Guid.NewGuid(), 
            tenantId, 
            Guid.NewGuid(), 
            "VAR-1", 
            "Variant 1", 
            sku, 
            Guid.Empty, Guid.Empty,
            false, true, false,
            ProductConstants.ActiveStatus, 
            null,
            Now);
        dbContext.ProductVariants.Add(variant);
        await dbContext.SaveChangesAsync();
        var repository = new ProductRepository(dbContext);

        var exists = await repository.SkuExistsAsync(tenantId, sku, null, CancellationToken.None);

        Assert.True(exists);
    }

    [Fact]
    public async Task ListAsync_ReturnsPaginatedProductsWithDetails()
    {
        var tenantId = Guid.NewGuid();
        await using var dbContext = CreateDbContext();
        
        var defaultPriceList = new PriceList();
        Set(defaultPriceList, "Id", Guid.NewGuid());
        Set(defaultPriceList, "TenantId", tenantId);
        Set(defaultPriceList, "IsDefaultPriceList", true);
        Set(defaultPriceList, "Status", "ACTIVE");
        Set(defaultPriceList, "PriceListCode", "DEFAULT");
        Set(defaultPriceList, "PriceListName", "Default Price List");
        Set(defaultPriceList, "PriceListType", "POS");
        Set(defaultPriceList, "CurrencyCode", "LKR");
        Set(defaultPriceList, "CreatedAt", Now);
        Set(defaultPriceList, "UpdatedAt", Now);
        dbContext.PriceLists.Add(defaultPriceList);

        var productId1 = Guid.NewGuid();
        var product1 = Product.Create(
            productId1, 
            tenantId, 
            "P1", 
            "Alpha Product", 
            "alpha-product",
            "STANDARD",
            "SIMPLE",
            null, null, null,
            null, null,
            true, true,
            ProductConstants.ActiveStatus, 
            null, 
            Now);
        dbContext.Products.Add(product1);

        var variantId1 = Guid.NewGuid();
        var variant1 = ProductVariant.Create(
            variantId1, 
            tenantId, 
            productId1, 
            "DEFAULT", 
            "Alpha Product", 
            "SKU-A", 
            Guid.Empty, Guid.Empty,
            true, true, false,
            ProductConstants.ActiveStatus, 
            null, 
            Now);
        dbContext.ProductVariants.Add(variant1);

        var priceItem1 = new PriceListItem();
        Set(priceItem1, "Id", Guid.NewGuid());
        Set(priceItem1, "TenantId", tenantId);
        Set(priceItem1, "PriceListId", defaultPriceList.Id);
        Set(priceItem1, "ProductId", productId1);
        Set(priceItem1, "ProductVariantId", variantId1);
        Set(priceItem1, "SellingPrice", 19.99m);
        Set(priceItem1, "MinQuantity", 1m);
        Set(priceItem1, "Status", "ACTIVE");
        Set(priceItem1, "CreatedAt", Now);
        Set(priceItem1, "UpdatedAt", Now);
        dbContext.PriceListItems.Add(priceItem1);

        await dbContext.SaveChangesAsync();
        var repository = new ProductRepository(dbContext);

        var result = await repository.ListAsync(tenantId, 1, 10, null, CancellationToken.None);

        Assert.Equal(1, result.TotalCount);
        var first = Assert.Single(result.Items);
        Assert.Equal("Alpha Product", first.Name);
        Assert.Equal("SKU-A", first.Sku);
        Assert.Equal(19.99m, first.Price);
    }

    private static void Set<T>(object entity, string propertyName, T value)
    {
        var prop = entity.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        prop?.SetValue(entity, value);
    }

    private static EPosDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<EPosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new EPosDbContext(options);
    }
}


