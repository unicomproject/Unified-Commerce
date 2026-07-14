using System.Reflection;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Entities;
using E_POS.Domain.Modules.Tenant.PricingTax.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using E_POS.Infrastructure.Modules.Tenant.CatalogProduct.Repositories;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace E_POS.IntegrationTests.CatalogProduct;

public sealed class PosProductCatalogRepositoryTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 10, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ListProductsAsync_ReturnsActiveSellableProductsForDevice()
    {
        var tenantId = Guid.NewGuid();
        var outletId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var departmentId = Guid.NewGuid();

        await using var dbContext = CreateDbContext();
        await SeedDeviceAsync(dbContext, tenantId, outletId, deviceId);
        await SeedDefaultPriceListAsync(dbContext, tenantId, productId, variantId, 1250m);
        await SeedCategoryAsync(dbContext, tenantId, departmentId, categoryId, productId);

        dbContext.Products.Add(Product.Create(
            productId,
            tenantId,
            "JER-001",
            "Team Jersey",
            "team-jersey",
            "STANDARD",
            "SIMPLE",
            null,
            null,
            null,
            "Official team jersey",
            null,
            true,
            true,
            ProductConstants.ActiveStatus,
            null,
            Now));

        dbContext.ProductVariants.Add(ProductVariant.Create(
            variantId,
            tenantId,
            productId,
            "DEFAULT",
            "Team Jersey",
            "JER-SKU",
            Guid.NewGuid(),
            Guid.NewGuid(),
            true,
            true,
            false,
            ProductConstants.ActiveStatus,
            null,
            Now));

        dbContext.ProductImages.Add(ProductImage.Create(
            Guid.NewGuid(),
            tenantId,
            productId,
            null,
            null,
            "https://example.com/jersey.png",
            "https://example.com/jersey.png",
            "Team Jersey",
            "PRIMARY",
            "image/png",
            1024,
            400,
            400,
            null,
            0,
            true,
            "ACTIVE",
            null,
            Now));

        await dbContext.SaveChangesAsync();

        var repository = new PosProductCatalogRepository(dbContext);
        var result = await repository.ListProductsAsync(
            tenantId,
            deviceId,
            null,
            null,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var summary = Assert.Single(result.Products);
        Assert.Equal(productId, summary.Id);
        Assert.Equal(variantId, summary.VariantId);
        Assert.Equal("Team Jersey", summary.Name);
        Assert.Equal("Official team jersey", summary.Description);
        Assert.Equal("Apparel", summary.CategoryName);
        Assert.Equal(categoryId, summary.CategoryId);
        Assert.Equal(1250, summary.BasePrice);
        Assert.False(summary.HasVariants);
        Assert.Equal("https://example.com/jersey.png", summary.ImageStorageKey);
        Assert.Equal("in_stock", summary.StockStatus);
        Assert.Null(summary.AvailableQuantity);
    }

    [Fact]
    public async Task ListCategoriesAsync_ReturnsCategoriesWithSellableProducts()
    {
        var tenantId = Guid.NewGuid();
        var outletId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var departmentId = Guid.NewGuid();

        await using var dbContext = CreateDbContext();
        await SeedDeviceAsync(dbContext, tenantId, outletId, deviceId);
        await SeedDefaultPriceListAsync(dbContext, tenantId, productId, variantId, 1250m);
        await SeedCategoryAsync(dbContext, tenantId, departmentId, categoryId, productId);

        dbContext.Products.Add(Product.Create(
            productId,
            tenantId,
            "JER-001",
            "Team Jersey",
            "team-jersey",
            "STANDARD",
            "SIMPLE",
            null,
            null,
            null,
            "Official team jersey",
            null,
            true,
            true,
            ProductConstants.ActiveStatus,
            null,
            Now));

        dbContext.ProductVariants.Add(ProductVariant.Create(
            variantId,
            tenantId,
            productId,
            "DEFAULT",
            "Team Jersey",
            "JER-SKU",
            Guid.NewGuid(),
            Guid.NewGuid(),
            true,
            true,
            false,
            ProductConstants.ActiveStatus,
            null,
            Now));

        await dbContext.SaveChangesAsync();

        var repository = new PosProductCatalogRepository(dbContext);
        var result = await repository.ListCategoriesAsync(
            tenantId,
            deviceId,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var category = Assert.Single(result.Categories);
        Assert.Equal(categoryId, category.Id);
        Assert.Equal("Apparel", category.Name);
    }

    [Fact]
    public async Task ListProductsAsync_WhenDeviceMissing_ReturnsDeviceNotFound()
    {
        await using var dbContext = CreateDbContext();
        var repository = new PosProductCatalogRepository(dbContext);

        var result = await repository.ListProductsAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            null,
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("pos_products.device_not_found", result.ErrorCode);
        Assert.Empty(result.Products);
    }

    [Fact]
    public async Task ListProductsAsync_WithSkuSearch_ReturnsMatchingProduct()
    {
        var tenantId = Guid.NewGuid();
        var outletId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var variantId = Guid.NewGuid();

        await using var dbContext = CreateDbContext();
        await SeedDeviceAsync(dbContext, tenantId, outletId, deviceId);
        await SeedDefaultPriceListAsync(dbContext, tenantId, productId, variantId, 500m);

        dbContext.Products.Add(Product.Create(
            productId,
            tenantId,
            "CAP-001",
            "Sports Cap",
            "sports-cap",
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

        dbContext.ProductVariants.Add(ProductVariant.Create(
            variantId,
            tenantId,
            productId,
            "DEFAULT",
            "Sports Cap",
            "CAP-SKU-99",
            Guid.NewGuid(),
            Guid.NewGuid(),
            true,
            true,
            false,
            ProductConstants.ActiveStatus,
            null,
            Now));

        await dbContext.SaveChangesAsync();

        var repository = new PosProductCatalogRepository(dbContext);
        var result = await repository.ListProductsAsync(
            tenantId,
            deviceId,
            null,
            "CAP-SKU-99",
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var summary = Assert.Single(result.Products);
        Assert.Equal("Sports Cap", summary.Name);
    }

    private static async Task SeedDeviceAsync(
        EPosDbContext dbContext,
        Guid tenantId,
        Guid outletId,
        Guid deviceId)
    {
        dbContext.PosDevices.Add(PosDevice.Create(
            deviceId,
            tenantId,
            outletId,
            "POS-01",
            "Front Counter",
            "TABLET",
            "ACTIVE",
            null,
            Now));
        await dbContext.SaveChangesAsync();
    }

    private static async Task SeedDefaultPriceListAsync(
        EPosDbContext dbContext,
        Guid tenantId,
        Guid productId,
        Guid variantId,
        decimal sellingPrice)
    {
        var priceList = new PriceList();
        Set(priceList, "Id", Guid.NewGuid());
        Set(priceList, "TenantId", tenantId);
        Set(priceList, "IsDefaultPriceList", true);
        Set(priceList, "Status", "ACTIVE");
        Set(priceList, "PriceListCode", "DEFAULT");
        Set(priceList, "PriceListName", "Default Price List");
        Set(priceList, "PriceListType", "POS");
        Set(priceList, "CurrencyCode", "LKR");
        Set(priceList, "CreatedAt", Now);
        Set(priceList, "UpdatedAt", Now);
        dbContext.PriceLists.Add(priceList);

        var priceItem = new PriceListItem();
        Set(priceItem, "Id", Guid.NewGuid());
        Set(priceItem, "TenantId", tenantId);
        Set(priceItem, "PriceListId", priceList.Id);
        Set(priceItem, "ProductId", productId);
        Set(priceItem, "ProductVariantId", variantId);
        Set(priceItem, "SellingPrice", sellingPrice);
        Set(priceItem, "MinQuantity", 1m);
        Set(priceItem, "Status", "ACTIVE");
        Set(priceItem, "CreatedAt", Now);
        Set(priceItem, "UpdatedAt", Now);
        dbContext.PriceListItems.Add(priceItem);
    }

    private static async Task SeedCategoryAsync(
        EPosDbContext dbContext,
        Guid tenantId,
        Guid departmentId,
        Guid categoryId,
        Guid productId)
    {
        dbContext.Departments.Add(Department.Create(
            departmentId,
            tenantId,
            "DEPT-01",
            "Merchandise",
            null,
            0,
            "ACTIVE",
            null,
            Now));

        dbContext.Categories.Add(Category.Create(
            categoryId,
            tenantId,
            departmentId,
            null,
            "APPAREL",
            "Apparel",
            "apparel",
            null,
            null,
            0,
            "ACTIVE",
            null,
            Now));

        dbContext.ProductCategories.Add(ProductCategory.Create(
            Guid.NewGuid(),
            tenantId,
            productId,
            categoryId,
            true,
            0,
            null,
            Now));

        await dbContext.SaveChangesAsync();
    }

    private static void Set<T>(object entity, string propertyName, T value)
    {
        var prop = entity.GetType().GetProperty(
            propertyName,
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
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
