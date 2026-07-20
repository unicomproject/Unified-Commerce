using System.Reflection;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using E_POS.Domain.Modules.Tenant.Inventory.Entities;
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

    [Fact]
    public async Task ListProductsAsync_VariableProduct_ReturnsHasVariantsTrueWithoutVariantId()
    {
        var tenantId = Guid.NewGuid();
        var outletId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var smallBlueVariantId = Guid.NewGuid();
        var mediumBlueVariantId = Guid.NewGuid();

        await using var dbContext = CreateDbContext();
        await SeedDeviceAsync(dbContext, tenantId, outletId, deviceId);
        await SeedVariableProductAsync(
            dbContext,
            tenantId,
            productId,
            smallBlueVariantId,
            mediumBlueVariantId,
            outletId);

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
        Assert.True(summary.HasVariants);
        Assert.Null(summary.VariantId);
        Assert.Equal(10000, summary.BasePrice);
        Assert.Equal("in_stock", summary.StockStatus);
        Assert.Equal(35m, summary.AvailableQuantity);
    }

    [Fact]
    public async Task ListAndDetail_AllActiveVariantsHaveZeroAvailable_ReturnOutOfStock()
    {
        var tenantId = Guid.NewGuid();
        var outletId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var firstVariantId = Guid.NewGuid();
        var secondVariantId = Guid.NewGuid();

        await using var dbContext = CreateDbContext();
        await SeedDeviceAsync(dbContext, tenantId, outletId, deviceId);
        await SeedVariableProductAsync(
            dbContext, tenantId, productId, firstVariantId, secondVariantId, outletId);

        foreach (var balance in dbContext.InventoryBalances)
        {
            balance.AdjustQuantities(-balance.AvailableQuantity, 0m, 0m, 0m, Now);
        }
        await dbContext.SaveChangesAsync();

        var repository = new PosProductCatalogRepository(dbContext);
        var listResult = await repository.ListProductsAsync(
            tenantId, deviceId, null, null, CancellationToken.None);
        var detailResult = await repository.GetProductDetailAsync(
            tenantId, deviceId, productId, CancellationToken.None);

        var summary = Assert.Single(listResult.Products);
        Assert.Equal("out_of_stock", summary.StockStatus);
        Assert.Equal(0m, summary.AvailableQuantity);
        Assert.NotNull(detailResult.Product);
        Assert.Equal(summary.StockStatus, detailResult.Product.StockStatus);
        Assert.All(detailResult.Product.Variants, variant =>
        {
            Assert.Equal(0m, variant.StockQty);
            Assert.Equal("out_of_stock", variant.StockStatus);
        });
    }

    [Fact]
    public async Task ListAndDetail_StockAtAnotherOutletOnly_ReturnOutOfStockForDeviceOutlet()
    {
        var tenantId = Guid.NewGuid();
        var deviceOutletId = Guid.NewGuid();
        var otherOutletId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var firstVariantId = Guid.NewGuid();
        var secondVariantId = Guid.NewGuid();

        await using var dbContext = CreateDbContext();
        await SeedDeviceAsync(dbContext, tenantId, deviceOutletId, deviceId);
        await SeedVariableProductAsync(
            dbContext, tenantId, productId, firstVariantId, secondVariantId, otherOutletId);

        var repository = new PosProductCatalogRepository(dbContext);
        var listResult = await repository.ListProductsAsync(
            tenantId, deviceId, null, null, CancellationToken.None);
        var detailResult = await repository.GetProductDetailAsync(
            tenantId, deviceId, productId, CancellationToken.None);

        var summary = Assert.Single(listResult.Products);
        Assert.Equal("in_stock", summary.StockStatus);
        Assert.Null(summary.AvailableQuantity);
        Assert.NotNull(detailResult.Product);
        Assert.Equal("in_stock", detailResult.Product.StockStatus);
        Assert.All(detailResult.Product.Variants, variant => Assert.Null(variant.StockQty));
    }

    [Fact]
    public async Task GetProductDetailAsync_VariableProduct_ReturnsVariantGroupsAndVariants()
    {
        var tenantId = Guid.NewGuid();
        var outletId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var smallBlueVariantId = Guid.NewGuid();
        var mediumBlueVariantId = Guid.NewGuid();

        await using var dbContext = CreateDbContext();
        await SeedDeviceAsync(dbContext, tenantId, outletId, deviceId);
        await SeedVariableProductAsync(
            dbContext,
            tenantId,
            productId,
            smallBlueVariantId,
            mediumBlueVariantId,
            outletId);

        var repository = new PosProductCatalogRepository(dbContext);
        var result = await repository.GetProductDetailAsync(
            tenantId,
            deviceId,
            productId,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Product);
        Assert.Equal(productId, result.Product.Id);
        Assert.True(result.Product.HasVariants);
        Assert.Equal(2, result.Product.VariantGroups.Count);
        Assert.Contains(result.Product.VariantGroups, group => group.Name == "Size");
        Assert.Contains(result.Product.VariantGroups, group => group.Name == "Color");

        var smallBlue = result.Product.Variants.Single(x => x.VariantId == smallBlueVariantId);
        Assert.Equal("JER-S-BLU", smallBlue.Sku);
        Assert.Equal(10000, smallBlue.Price);
        Assert.Equal("in_stock", smallBlue.StockStatus);
        Assert.Equal("Small", smallBlue.Attributes["Size"]);
        Assert.Equal("Blue", smallBlue.Attributes["Color"]);

        var mediumBlue = result.Product.Variants.Single(x => x.VariantId == mediumBlueVariantId);
        Assert.Equal(12000, mediumBlue.Price);
    }

    [Fact]
    public async Task GetProductDetailAsync_WhenProductMissing_ReturnsProductNotFound()
    {
        var tenantId = Guid.NewGuid();
        var outletId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();

        await using var dbContext = CreateDbContext();
        await SeedDeviceAsync(dbContext, tenantId, outletId, deviceId);

        var repository = new PosProductCatalogRepository(dbContext);
        var result = await repository.GetProductDetailAsync(
            tenantId,
            deviceId,
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("pos_products.product_not_found", result.ErrorCode);
        Assert.Null(result.Product);
    }

    [Fact]
    public async Task GetProductDetailAsync_WhenDeviceMissing_ReturnsDeviceNotFound()
    {
        await using var dbContext = CreateDbContext();
        var repository = new PosProductCatalogRepository(dbContext);

        var result = await repository.GetProductDetailAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("pos_products.device_not_found", result.ErrorCode);
    }

    [Fact]
    public async Task GetProductDetailAsync_WhenProductInactive_ReturnsProductNotFound()
    {
        var tenantId = Guid.NewGuid();
        var outletId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var variantId = Guid.NewGuid();

        await using var dbContext = CreateDbContext();
        await SeedDeviceAsync(dbContext, tenantId, outletId, deviceId);
        await SeedDefaultPriceListAsync(dbContext, tenantId, productId, variantId, 1250m);

        dbContext.Products.Add(Product.Create(
            productId,
            tenantId,
            "JER-001",
            "Inactive Jersey",
            "inactive-jersey",
            "STANDARD",
            "SIMPLE",
            null,
            null,
            null,
            null,
            null,
            true,
            true,
            "INACTIVE",
            null,
            Now));

        dbContext.ProductVariants.Add(ProductVariant.Create(
            variantId,
            tenantId,
            productId,
            "DEFAULT",
            "Inactive Jersey",
            "JER-INACTIVE",
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
        var result = await repository.GetProductDetailAsync(
            tenantId,
            deviceId,
            productId,
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("pos_products.product_not_found", result.ErrorCode);
    }

    private static async Task SeedVariableProductAsync(
        EPosDbContext dbContext,
        Guid tenantId,
        Guid productId,
        Guid smallBlueVariantId,
        Guid mediumBlueVariantId,
        Guid outletId)
    {
        var sizeOptionId = Guid.NewGuid();
        var colorOptionId = Guid.NewGuid();
        var smallValueId = Guid.NewGuid();
        var mediumValueId = Guid.NewGuid();
        var blueValueId = Guid.NewGuid();
        var uomId = Guid.NewGuid();
        var inventoryLocationId = Guid.NewGuid();
        var priceListId = Guid.NewGuid();

        var priceList = new PriceList();
        Set(priceList, "Id", priceListId);
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

        dbContext.PriceListItems.Add(CreatePriceListItem(
            Guid.NewGuid(),
            tenantId,
            priceListId,
            productId,
            smallBlueVariantId,
            10000m));
        dbContext.PriceListItems.Add(CreatePriceListItem(
            Guid.NewGuid(),
            tenantId,
            priceListId,
            productId,
            mediumBlueVariantId,
            12000m));

        dbContext.InventoryLocations.Add(InventoryLocation.Create(
            inventoryLocationId,
            tenantId,
            outletId,
            null,
            "STORE-FLOOR",
            "Store Floor",
            "SALES",
            true,
            true,
            true,
            false,
            "ACTIVE",
            null,
            Now));

        dbContext.Products.Add(Product.Create(
            productId,
            tenantId,
            "JER-VAR",
            "Pro Team Jersey",
            "pro-team-jersey",
            "STANDARD",
            "VARIABLE",
            null,
            null,
            null,
            "Sized team jersey",
            null,
            true,
            true,
            ProductConstants.ActiveStatus,
            null,
            Now));

        dbContext.ProductOptions.Add(ProductOption.Create(
            sizeOptionId,
            tenantId,
            productId,
            null,
            "SIZE",
            "Size",
            "VARIANT",
            "SELECT",
            true,
            0,
            "ACTIVE",
            null,
            Now));

        dbContext.ProductOptions.Add(ProductOption.Create(
            colorOptionId,
            tenantId,
            productId,
            null,
            "COLOR",
            "Color",
            "VARIANT",
            "SELECT",
            true,
            1,
            "ACTIVE",
            null,
            Now));

        dbContext.ProductOptionValues.Add(ProductOptionValue.Create(
            smallValueId,
            tenantId,
            sizeOptionId,
            null,
            "SMALL",
            "Small",
            "Small",
            null,
            null,
            0,
            "ACTIVE",
            null,
            Now));

        dbContext.ProductOptionValues.Add(ProductOptionValue.Create(
            mediumValueId,
            tenantId,
            sizeOptionId,
            null,
            "MEDIUM",
            "Medium",
            "Medium",
            null,
            null,
            1,
            "ACTIVE",
            null,
            Now));

        dbContext.ProductOptionValues.Add(ProductOptionValue.Create(
            blueValueId,
            tenantId,
            colorOptionId,
            null,
            "BLUE",
            "Blue",
            "Blue",
            null,
            null,
            0,
            "ACTIVE",
            null,
            Now));

        dbContext.ProductVariants.Add(ProductVariant.Create(
            smallBlueVariantId,
            tenantId,
            productId,
            "S-BLU",
            "Small / Blue",
            "JER-S-BLU",
            uomId,
            uomId,
            true,
            true,
            false,
            ProductConstants.ActiveStatus,
            null,
            Now));

        dbContext.ProductVariants.Add(ProductVariant.Create(
            mediumBlueVariantId,
            tenantId,
            productId,
            "M-BLU",
            "Medium / Blue",
            "JER-M-BLU",
            uomId,
            uomId,
            false,
            true,
            false,
            ProductConstants.ActiveStatus,
            null,
            Now));

        dbContext.ProductVariantOptionValues.Add(ProductVariantOptionValue.Create(
            Guid.NewGuid(),
            tenantId,
            productId,
            smallBlueVariantId,
            sizeOptionId,
            smallValueId,
            null,
            Now));

        dbContext.ProductVariantOptionValues.Add(ProductVariantOptionValue.Create(
            Guid.NewGuid(),
            tenantId,
            productId,
            smallBlueVariantId,
            colorOptionId,
            blueValueId,
            null,
            Now));

        dbContext.ProductVariantOptionValues.Add(ProductVariantOptionValue.Create(
            Guid.NewGuid(),
            tenantId,
            productId,
            mediumBlueVariantId,
            sizeOptionId,
            mediumValueId,
            null,
            Now));

        dbContext.ProductVariantOptionValues.Add(ProductVariantOptionValue.Create(
            Guid.NewGuid(),
            tenantId,
            productId,
            mediumBlueVariantId,
            colorOptionId,
            blueValueId,
            null,
            Now));

        var smallBalance = InventoryBalance.Create(
            Guid.NewGuid(),
            tenantId,
            inventoryLocationId,
            productId,
            smallBlueVariantId,
            null,
            Now);
        smallBalance.AdjustQuantities(20m, 0m, 0m, 0m, Now);
        dbContext.InventoryBalances.Add(smallBalance);

        var mediumBalance = InventoryBalance.Create(
            Guid.NewGuid(),
            tenantId,
            inventoryLocationId,
            productId,
            mediumBlueVariantId,
            null,
            Now);
        mediumBalance.AdjustQuantities(15m, 0m, 0m, 0m, Now);
        dbContext.InventoryBalances.Add(mediumBalance);

        await dbContext.SaveChangesAsync();
    }

    private static PriceListItem CreatePriceListItem(
        Guid id,
        Guid tenantId,
        Guid priceListId,
        Guid productId,
        Guid variantId,
        decimal sellingPrice)
    {
        var priceItem = new PriceListItem();
        Set(priceItem, "Id", id);
        Set(priceItem, "TenantId", tenantId);
        Set(priceItem, "PriceListId", priceListId);
        Set(priceItem, "ProductId", productId);
        Set(priceItem, "ProductVariantId", variantId);
        Set(priceItem, "SellingPrice", sellingPrice);
        Set(priceItem, "MinQuantity", 1m);
        Set(priceItem, "Status", "ACTIVE");
        Set(priceItem, "CreatedAt", Now);
        Set(priceItem, "UpdatedAt", Now);
        return priceItem;
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
