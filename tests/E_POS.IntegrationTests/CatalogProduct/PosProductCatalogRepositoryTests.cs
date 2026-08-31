using System.Reflection;
using E_POS.Domain.Modules.Shared.Media.Entities;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using E_POS.Domain.Modules.Tenant.Inventory.Entities;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Entities;
using E_POS.Domain.Modules.Tenant.PricingTax.Entities;
using E_POS.Domain.Modules.Tenant.Discount.Entities;
using E_POS.Domain.Modules.Platform.PlatformFoundation.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using E_POS.Domain.Modules.Tenant.Orders.Entities;
using E_POS.Infrastructure.Modules.Tenant.CatalogProduct.Repositories;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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
        AddProductImage(dbContext, tenantId, productId, "https://example.com/jersey.png");

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
    public async Task ListProductsAsync_WithPartialSkuOrBarcodeSearch_ReturnsMatchingProduct()
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

        dbContext.ProductBarcodes.Add(ProductBarcode.Create(
            Guid.NewGuid(),
            tenantId,
            productId,
            variantId,
            "1234567890123",
            "EAN13",
            null,
            1m,
            true,
            ProductConstants.ActiveStatus,
            null,
            Now));

        await dbContext.SaveChangesAsync();

        var repository = new PosProductCatalogRepository(dbContext);
        var skuResult = await repository.ListProductsAsync(
            tenantId,
            deviceId,
            null,
            "SKU-9",
            CancellationToken.None);

        Assert.True(skuResult.IsSuccess);
        var skuProduct = Assert.Single(skuResult.Products);
        Assert.Equal("Sports Cap", skuProduct.Name);
        Assert.Equal(variantId, skuProduct.VariantId);
        Assert.Equal("CAP-SKU-99", skuProduct.Sku);

        var barcodeResult = await repository.ListProductsAsync(
            tenantId,
            deviceId,
            null,
            "456789",
            CancellationToken.None);

        Assert.True(barcodeResult.IsSuccess);
        var barcodeProduct = Assert.Single(barcodeResult.Products);
        Assert.Equal("Sports Cap", barcodeProduct.Name);
        Assert.Equal(variantId, barcodeProduct.VariantId);
        Assert.Equal("1234567890123", barcodeProduct.Barcode);
    }

    [Fact]
    public async Task GetProductByBarcodeAsync_ExactSecondaryBarcode_ReturnsExactVariantQuantityPriceAndStock()
    {
        var tenantId = Guid.NewGuid();
        var outletId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        var locationId = Guid.NewGuid();

        await using var dbContext = CreateDbContext();
        await SeedDeviceAsync(dbContext, tenantId, outletId, deviceId);
        await SeedDefaultPriceListAsync(dbContext, tenantId, productId, variantId, 2500m);
        dbContext.Products.Add(Product.Create(
            productId, tenantId, "CAP-EXACT", "Exact Cap", "exact-cap", "STANDARD", "SIMPLE",
            null, null, null, null, null, true, true, ProductConstants.ActiveStatus, null, Now));
        dbContext.ProductVariants.Add(ProductVariant.Create(
            variantId, tenantId, productId, "BLUE", "Blue", "CAP-EXACT-BLU",
            Guid.NewGuid(), Guid.NewGuid(), true, true, false, ProductConstants.ActiveStatus, null, Now));
        dbContext.ProductBarcodes.AddRange(
            ProductBarcode.Create(Guid.NewGuid(), tenantId, productId, variantId, "2000000000114",
                "EAN13", null, 1m, true, ProductConstants.ActiveStatus, null, Now),
            ProductBarcode.Create(Guid.NewGuid(), tenantId, productId, variantId, "82111001003",
                "CODE128", null, 2m, false, ProductConstants.ActiveStatus, null, Now));
        dbContext.InventoryLocations.Add(InventoryLocation.Create(
            locationId, tenantId, outletId, null, "SALES", "Sales Floor", "SALES",
            true, true, true, false, "ACTIVE", null, Now));
        var balance = InventoryBalance.Create(
            Guid.NewGuid(), tenantId, locationId, productId, variantId, null, Now);
        balance.AdjustQuantities(10m, 0m, 0m, 0m, Now);
        dbContext.InventoryBalances.Add(balance);
        await dbContext.SaveChangesAsync();

        var repository = new PosProductCatalogRepository(dbContext);
        var result = await repository.GetProductByBarcodeAsync(
            tenantId, deviceId, "82111001003", CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Product);
        Assert.Equal(variantId, result.Product.VariantId);
        Assert.Equal("82111001003", result.Product.Barcode);
        Assert.Equal(2m, result.Product.QuantityPerScan);
        Assert.Equal(2500, result.Product.Price);
        Assert.Equal(10m, result.Product.AvailableQuantity);
    }

    [Theory]
    [InlineData("200000000011")]
    [InlineData("00000000114")]
    [InlineData("200000")]
    public async Task GetProductByBarcodeAsync_PartialBarcode_DoesNotMatch(string partialBarcode)
    {
        var tenantId = Guid.NewGuid();
        var outletId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        await using var dbContext = CreateDbContext();
        await SeedDeviceAsync(dbContext, tenantId, outletId, deviceId);
        dbContext.Products.Add(Product.Create(
            productId, tenantId, "EXACT", "Exact", "exact", "STANDARD", "SIMPLE",
            null, null, null, null, null, true, true, ProductConstants.ActiveStatus, null, Now));
        dbContext.ProductVariants.Add(ProductVariant.Create(
            variantId, tenantId, productId, "DEFAULT", "Default", "EXACT-SKU",
            Guid.NewGuid(), Guid.NewGuid(), true, true, false, ProductConstants.ActiveStatus, null, Now));
        dbContext.ProductBarcodes.Add(ProductBarcode.Create(
            Guid.NewGuid(), tenantId, productId, variantId, "2000000000114", "EAN13", null,
            1m, true, ProductConstants.ActiveStatus, null, Now));
        await dbContext.SaveChangesAsync();

        var result = await new PosProductCatalogRepository(dbContext).GetProductByBarcodeAsync(
            tenantId, deviceId, partialBarcode, CancellationToken.None);

        Assert.Equal("pos_barcode.not_found", result.ErrorCode);
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

    [Fact]
    public async Task ListProductsAsync_FrequentlySold_ReturnsRankedProducts()
    {
        var tenantId = Guid.NewGuid();
        var outletId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();

        var productIdA = Guid.NewGuid();
        var variantIdA = Guid.NewGuid();
        var productIdB = Guid.NewGuid();
        var variantIdB = Guid.NewGuid();

        await using var dbContext = CreateDbContext();
        await SeedDeviceAsync(dbContext, tenantId, outletId, deviceId);
        await SeedDefaultPriceListAsync(dbContext, tenantId, productIdA, variantIdA, 1000m);
        await SeedDefaultPriceListAsync(dbContext, tenantId, productIdB, variantIdB, 2000m);

        dbContext.Products.Add(Product.Create(productIdA, tenantId, "P-A", "Product A", "p-a", "STANDARD", "SIMPLE", null, null, null, null, null, true, true, ProductConstants.ActiveStatus, null, Now));
        dbContext.ProductVariants.Add(ProductVariant.Create(variantIdA, tenantId, productIdA, "DEFAULT", "Product A", "SKU-A", Guid.NewGuid(), Guid.NewGuid(), true, true, false, ProductConstants.ActiveStatus, null, Now));

        dbContext.Products.Add(Product.Create(productIdB, tenantId, "P-B", "Product B", "p-b", "STANDARD", "SIMPLE", null, null, null, null, null, true, true, ProductConstants.ActiveStatus, null, Now));
        dbContext.ProductVariants.Add(ProductVariant.Create(variantIdB, tenantId, productIdB, "DEFAULT", "Product B", "SKU-B", Guid.NewGuid(), Guid.NewGuid(), true, true, false, ProductConstants.ActiveStatus, null, Now));
        await dbContext.SaveChangesAsync();

        // Seed Sales: Product B has 15 units sold, Product A has 10 units sold
        await SeedCompletedPosSaleAsync(dbContext, tenantId, Guid.NewGuid(), outletId, DateTimeOffset.UtcNow.AddDays(-2), productIdA, variantIdA, 10m);
        await SeedCompletedPosSaleAsync(dbContext, tenantId, Guid.NewGuid(), outletId, DateTimeOffset.UtcNow.AddDays(-1), productIdB, variantIdB, 15m);

        var repository = new PosProductCatalogRepository(dbContext);
        var result = await repository.ListProductsAsync(
            tenantId,
            deviceId,
            null,
            null,
            CancellationToken.None,
            segment: "frequently-sold");

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Products.Count);
        // Product B should be first (15 sold)
        Assert.Equal(productIdB, result.Products[0].Id);
        Assert.Equal(productIdA, result.Products[1].Id);
    }

    [Fact]
    public async Task ListProductsAsync_FrequentlySold_DeductsCancellationsAndReturns()
    {
        var tenantId = Guid.NewGuid();
        var outletId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();

        var productIdA = Guid.NewGuid();
        var variantIdA = Guid.NewGuid();
        var productIdB = Guid.NewGuid();
        var variantIdB = Guid.NewGuid();
        var productIdC = Guid.NewGuid();
        var variantIdC = Guid.NewGuid();

        await using var dbContext = CreateDbContext();
        await SeedDeviceAsync(dbContext, tenantId, outletId, deviceId);
        await SeedDefaultPriceListAsync(dbContext, tenantId, productIdA, variantIdA, 1000m);
        await SeedDefaultPriceListAsync(dbContext, tenantId, productIdB, variantIdB, 2000m);
        await SeedDefaultPriceListAsync(dbContext, tenantId, productIdC, variantIdC, 3000m);

        dbContext.Products.Add(Product.Create(productIdA, tenantId, "P-A", "Product A", "p-a", "STANDARD", "SIMPLE", null, null, null, null, null, true, true, ProductConstants.ActiveStatus, null, Now));
        dbContext.ProductVariants.Add(ProductVariant.Create(variantIdA, tenantId, productIdA, "DEFAULT", "Product A", "SKU-A", Guid.NewGuid(), Guid.NewGuid(), true, true, false, ProductConstants.ActiveStatus, null, Now));

        dbContext.Products.Add(Product.Create(productIdB, tenantId, "P-B", "Product B", "p-b", "STANDARD", "SIMPLE", null, null, null, null, null, true, true, ProductConstants.ActiveStatus, null, Now));
        dbContext.ProductVariants.Add(ProductVariant.Create(variantIdB, tenantId, productIdB, "DEFAULT", "Product B", "SKU-B", Guid.NewGuid(), Guid.NewGuid(), true, true, false, ProductConstants.ActiveStatus, null, Now));

        dbContext.Products.Add(Product.Create(productIdC, tenantId, "P-C", "Product C", "p-c", "STANDARD", "SIMPLE", null, null, null, null, null, true, true, ProductConstants.ActiveStatus, null, Now));
        dbContext.ProductVariants.Add(ProductVariant.Create(variantIdC, tenantId, productIdC, "DEFAULT", "Product C", "SKU-C", Guid.NewGuid(), Guid.NewGuid(), true, true, false, ProductConstants.ActiveStatus, null, Now));
        await dbContext.SaveChangesAsync();

        // Product A: 10 sold, 3 cancelled, 2 returned = net 5
        await SeedCompletedPosSaleAsync(dbContext, tenantId, Guid.NewGuid(), outletId, DateTimeOffset.UtcNow.AddDays(-2), productIdA, variantIdA, 10m, cancelledQuantity: 3m, returnedQuantity: 2m);
        // Product B: 4 sold = net 4
        await SeedCompletedPosSaleAsync(dbContext, tenantId, Guid.NewGuid(), outletId, DateTimeOffset.UtcNow.AddDays(-1), productIdB, variantIdB, 4m);
        // Product C: 5 sold, 5 cancelled = net 0 (excluded)
        await SeedCompletedPosSaleAsync(dbContext, tenantId, Guid.NewGuid(), outletId, DateTimeOffset.UtcNow.AddDays(-3), productIdC, variantIdC, 5m, cancelledQuantity: 5m);

        var repository = new PosProductCatalogRepository(dbContext);
        var result = await repository.ListProductsAsync(
            tenantId,
            deviceId,
            null,
            null,
            CancellationToken.None,
            segment: "frequently-sold");

        Assert.True(result.IsSuccess);
        // Only Product A and B should qualify. Product C has net 0, so excluded.
        Assert.Equal(2, result.Products.Count);
        Assert.Equal(productIdA, result.Products[0].Id);
        Assert.Equal(productIdB, result.Products[1].Id);
    }

    [Fact]
    public async Task ListProductsAsync_FrequentlySold_ExcludesNonCompletedAndOldOrders()
    {
        var tenantId = Guid.NewGuid();
        var outletId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();

        var productIdA = Guid.NewGuid();
        var variantIdA = Guid.NewGuid();
        var productIdB = Guid.NewGuid();
        var variantIdB = Guid.NewGuid();

        await using var dbContext = CreateDbContext();
        await SeedDeviceAsync(dbContext, tenantId, outletId, deviceId);
        await SeedDefaultPriceListAsync(dbContext, tenantId, productIdA, variantIdA, 1000m);
        await SeedDefaultPriceListAsync(dbContext, tenantId, productIdB, variantIdB, 2000m);

        dbContext.Products.Add(Product.Create(productIdA, tenantId, "P-A", "Product A", "p-a", "STANDARD", "SIMPLE", null, null, null, null, null, true, true, ProductConstants.ActiveStatus, null, Now));
        dbContext.ProductVariants.Add(ProductVariant.Create(variantIdA, tenantId, productIdA, "DEFAULT", "Product A", "SKU-A", Guid.NewGuid(), Guid.NewGuid(), true, true, false, ProductConstants.ActiveStatus, null, Now));

        dbContext.Products.Add(Product.Create(productIdB, tenantId, "P-B", "Product B", "p-b", "STANDARD", "SIMPLE", null, null, null, null, null, true, true, ProductConstants.ActiveStatus, null, Now));
        dbContext.ProductVariants.Add(ProductVariant.Create(variantIdB, tenantId, productIdB, "DEFAULT", "Product B", "SKU-B", Guid.NewGuid(), Guid.NewGuid(), true, true, false, ProductConstants.ActiveStatus, null, Now));
        await dbContext.SaveChangesAsync();

        // 1. Seed completed sale: Product A (10 sold)
        await SeedCompletedPosSaleAsync(dbContext, tenantId, Guid.NewGuid(), outletId, DateTimeOffset.UtcNow.AddDays(-2), productIdA, variantIdA, 10m);
        // 2. Seed draft sale: Product B (15 sold) -> should be excluded
        await SeedPosSaleWithStatusAsync(dbContext, tenantId, Guid.NewGuid(), outletId, "DRAFT", DateTimeOffset.UtcNow.AddDays(-1), productIdB, variantIdB, 15m);
        // 3. Seed cancelled sale: Product B (20 sold) -> should be excluded
        await SeedPosSaleWithStatusAsync(dbContext, tenantId, Guid.NewGuid(), outletId, "CANCELLED", DateTimeOffset.UtcNow.AddDays(-1), productIdB, variantIdB, 20m);
        // 4. Seed old completed sale: Product B (25 sold) -> outside 30 days lookback -> should be excluded
        await SeedCompletedPosSaleAsync(dbContext, tenantId, Guid.NewGuid(), outletId, DateTimeOffset.UtcNow.AddDays(-35), productIdB, variantIdB, 25m);

        var repository = new PosProductCatalogRepository(dbContext);
        var result = await repository.ListProductsAsync(
            tenantId,
            deviceId,
            null,
            null,
            CancellationToken.None,
            segment: "frequently-sold");

        Assert.True(result.IsSuccess);
        // Only Product A qualifies
        var summary = Assert.Single(result.Products);
        Assert.Equal(productIdA, summary.Id);
    }

    [Fact]
    public async Task ListProductsAsync_FrequentlySold_ResolvesConfigsAndAppliesLimit()
    {
        var tenantId = Guid.NewGuid();
        var outletId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();

        var productIdA = Guid.NewGuid();
        var variantIdA = Guid.NewGuid();
        var productIdB = Guid.NewGuid();
        var variantIdB = Guid.NewGuid();
        var productIdC = Guid.NewGuid();
        var variantIdC = Guid.NewGuid();

        await using var dbContext = CreateDbContext();
        await SeedDeviceAsync(dbContext, tenantId, outletId, deviceId);
        await SeedDefaultPriceListAsync(dbContext, tenantId, productIdA, variantIdA, 1000m);
        await SeedDefaultPriceListAsync(dbContext, tenantId, productIdB, variantIdB, 2000m);
        await SeedDefaultPriceListAsync(dbContext, tenantId, productIdC, variantIdC, 3000m);

        dbContext.Products.Add(Product.Create(productIdA, tenantId, "P-A", "Product A", "p-a", "STANDARD", "SIMPLE", null, null, null, null, null, true, true, ProductConstants.ActiveStatus, null, Now));
        dbContext.ProductVariants.Add(ProductVariant.Create(variantIdA, tenantId, productIdA, "DEFAULT", "Product A", "SKU-A", Guid.NewGuid(), Guid.NewGuid(), true, true, false, ProductConstants.ActiveStatus, null, Now));

        dbContext.Products.Add(Product.Create(productIdB, tenantId, "P-B", "Product B", "p-b", "STANDARD", "SIMPLE", null, null, null, null, null, true, true, ProductConstants.ActiveStatus, null, Now));
        dbContext.ProductVariants.Add(ProductVariant.Create(variantIdB, tenantId, productIdB, "DEFAULT", "Product B", "SKU-B", Guid.NewGuid(), Guid.NewGuid(), true, true, false, ProductConstants.ActiveStatus, null, Now));

        dbContext.Products.Add(Product.Create(productIdC, tenantId, "P-C", "Product C", "p-c", "STANDARD", "SIMPLE", null, null, null, null, null, true, true, ProductConstants.ActiveStatus, null, Now));
        dbContext.ProductVariants.Add(ProductVariant.Create(variantIdC, tenantId, productIdC, "DEFAULT", "Product C", "SKU-C", Guid.NewGuid(), Guid.NewGuid(), true, true, false, ProductConstants.ActiveStatus, null, Now));
        await dbContext.SaveChangesAsync();

        // Product A: 10 sold, 2 days ago (qualifies for 10-day lookback)
        await SeedCompletedPosSaleAsync(dbContext, tenantId, Guid.NewGuid(), outletId, DateTimeOffset.UtcNow.AddDays(-2), productIdA, variantIdA, 10m);
        // Product B: 8 sold, 1 day ago (qualifies for 10-day lookback)
        await SeedCompletedPosSaleAsync(dbContext, tenantId, Guid.NewGuid(), outletId, DateTimeOffset.UtcNow.AddDays(-1), productIdB, variantIdB, 8m);
        // Product C: 20 sold, 12 days ago (does not qualify for 10-day lookback)
        await SeedCompletedPosSaleAsync(dbContext, tenantId, Guid.NewGuid(), outletId, DateTimeOffset.UtcNow.AddDays(-12), productIdC, variantIdC, 20m);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "PosProducts:FrequentlySold:LookbackDays", "10" },
                { "PosProducts:FrequentlySold:Limit", "1" }
            })
            .Build();

        var repository = new PosProductCatalogRepository(dbContext, config);
        var result = await repository.ListProductsAsync(
            tenantId,
            deviceId,
            null,
            null,
            CancellationToken.None,
            segment: "frequently-sold");

        Assert.True(result.IsSuccess);
        // Limit is 1, so only Product A (highest quantity within 10 days) should be returned
        var summary = Assert.Single(result.Products);
        Assert.Equal(productIdA, summary.Id);
    }

    [Fact]
    public async Task ListProductsAsync_FrequentlySold_ObeysTenantAndOutletIsolation()
    {
        var tenantId = Guid.NewGuid();
        var otherTenantId = Guid.NewGuid();
        var outletId = Guid.NewGuid();
        var otherOutletId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();

        var productId = Guid.NewGuid();
        var variantId = Guid.NewGuid();

        await using var dbContext = CreateDbContext();
        await SeedDeviceAsync(dbContext, tenantId, outletId, deviceId);
        await SeedDefaultPriceListAsync(dbContext, tenantId, productId, variantId, 1000m);

        dbContext.Products.Add(Product.Create(productId, tenantId, "P-A", "Product A", "p-a", "STANDARD", "SIMPLE", null, null, null, null, null, true, true, ProductConstants.ActiveStatus, null, Now));
        dbContext.ProductVariants.Add(ProductVariant.Create(variantId, tenantId, productId, "DEFAULT", "Product A", "SKU-A", Guid.NewGuid(), Guid.NewGuid(), true, true, false, ProductConstants.ActiveStatus, null, Now));
        await dbContext.SaveChangesAsync();

        // Sales for different tenant -> should not qualify
        await SeedCompletedPosSaleAsync(dbContext, otherTenantId, Guid.NewGuid(), outletId, DateTimeOffset.UtcNow.AddDays(-1), productId, variantId, 10m);
        // Sales for different outlet on same tenant -> should not qualify
        await SeedCompletedPosSaleAsync(dbContext, tenantId, Guid.NewGuid(), otherOutletId, DateTimeOffset.UtcNow.AddDays(-1), productId, variantId, 15m);

        var repository = new PosProductCatalogRepository(dbContext);
        var result = await repository.ListProductsAsync(
            tenantId,
            deviceId,
            null,
            null,
            CancellationToken.None,
            segment: "frequently-sold");

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Products);
    }

    private static async Task SeedCompletedPosSaleAsync(
        EPosDbContext dbContext,
        Guid tenantId,
        Guid orderId,
        Guid outletId,
        DateTimeOffset completedAt,
        Guid productId,
        Guid variantId,
        decimal quantity,
        decimal cancelledQuantity = 0,
        decimal returnedQuantity = 0)
    {
        var order = SalesOrder.CreateCompletedPosSale(
            orderId,
            tenantId,
            $"ORD-{orderId.ToString().Substring(0, 8)}",
            Guid.NewGuid(),
            null,
            null,
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            "LKR",
            true,
            100m,
            0m,
            0m,
            100m,
            100m,
            null,
            completedAt);

        Set(order, "ReportingOutletId", outletId);
        dbContext.SalesOrders.Add(order);

        var line = SalesOrderLine.CreateForPosSale(
            Guid.NewGuid(),
            tenantId,
            orderId,
            1,
            productId,
            variantId,
            Guid.NewGuid(),
            null,
            "SKU",
            "Product Name",
            "Variant Name",
            "UOM",
            "UOM Name",
            "STANDARD",
            "SIMPLE",
            quantity,
            100m,
            100m,
            0m,
            0m,
            true,
            completedAt);

        Set(line, "CancelledQuantity", cancelledQuantity);
        Set(line, "ReturnedQuantity", returnedQuantity);
        dbContext.SalesOrderLines.Add(line);

        await dbContext.SaveChangesAsync();
    }

    private static async Task SeedPosSaleWithStatusAsync(
        EPosDbContext dbContext,
        Guid tenantId,
        Guid orderId,
        Guid outletId,
        string status,
        DateTimeOffset completedAt,
        Guid productId,
        Guid variantId,
        decimal quantity)
    {
        var order = SalesOrder.CreateCompletedPosSale(
            orderId,
            tenantId,
            $"ORD-{orderId.ToString().Substring(0, 8)}",
            Guid.NewGuid(),
            null,
            null,
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            "LKR",
            true,
            100m,
            0m,
            0m,
            100m,
            100m,
            null,
            completedAt);

        Set(order, "ReportingOutletId", outletId);
        Set(order, "Status", status);
        dbContext.SalesOrders.Add(order);

        var line = SalesOrderLine.CreateForPosSale(
            Guid.NewGuid(),
            tenantId,
            orderId,
            1,
            productId,
            variantId,
            Guid.NewGuid(),
            null,
            "SKU",
            "Product Name",
            "Variant Name",
            "UOM",
            "UOM Name",
            "STANDARD",
            "SIMPLE",
            quantity,
            100m,
            100m,
            0m,
            0m,
            true,
            completedAt);

        dbContext.SalesOrderLines.Add(line);
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
        dbContext.Outlets.Add(Outlet.Create(
            outletId,
            tenantId,
            "Main Store",
            "MAIN",
            "ACTIVE",
            "STORE",
            "Asia/Colombo",
            true,
            null,
            null,
            null,
            Now));
        var device = PosDevice.Create(
            deviceId,
            tenantId,
            outletId,
            "POS-01",
            "Front Counter",
            "TABLET",
            "ACTIVE",
            null,
            Now);
        device.PairForActivation(
            "Front Counter", "TABLET", "WINDOWS", "1.0.0", Guid.NewGuid().ToString("N"),
            Guid.NewGuid(), Now);
        dbContext.PosDevices.Add(device);
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

    private static void AddProductImage(
        EPosDbContext dbContext,
        Guid tenantId,
        Guid productId,
        string publicUrl)
    {
        var mediaAssetId = Guid.NewGuid();
        dbContext.MediaAssets.Add(CreateMediaAsset(tenantId, mediaAssetId, publicUrl, "PRODUCT"));
        dbContext.ProductImages.Add(ProductImage.Create(
            Guid.NewGuid(), tenantId, productId, null, null,
            "jersey-main", publicUrl, "Jersey", "MAIN", "image/jpeg",
            null, null, null, null, 0, true, "ACTIVE", null, Now, mediaAssetId));
    }

    private static MediaAsset CreateMediaAsset(
        Guid tenantId,
        Guid mediaAssetId,
        string publicUrl,
        string purpose) =>
        MediaAsset.Create(
            mediaAssetId,
            tenantId,
            "images",
            $"tests/{mediaAssetId:D}.jpg",
            publicUrl,
            "test.jpg",
            "image/jpeg",
            ".jpg",
            1,
            null,
            null,
            mediaAssetId.ToString("N"),
            "IMAGE",
            purpose,
            "ACTIVE",
            null,
            Now);
    [Fact]
    public async Task ListProductsAsync_OffersSegment_ResolvesSpecialPrices()
    {
        var tenantId = Guid.NewGuid();
        var outletId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var variantId = Guid.NewGuid();

        await using var dbContext = CreateDbContext();
        await SeedDeviceAsync(dbContext, tenantId, outletId, deviceId);

        var p = Product.Create(productId, tenantId, "P-1", "Prod 1", "prod-1", "STANDARD", "SIMPLE", null, null, null, "Desc", null, true, true, ProductConstants.ActiveStatus, null, Now);
        dbContext.Products.Add(p);
        var v = ProductVariant.Create(variantId, tenantId, productId, "DEFAULT", "Prod 1", "SKU-1", Guid.NewGuid(), Guid.NewGuid(), true, true, false, ProductConstants.ActiveStatus, null, Now);
        dbContext.ProductVariants.Add(v);

        await SeedDefaultPriceListAsync(dbContext, tenantId, productId, variantId, 1000m);

        // Active Special Price
        var pl = CreateEntity<PriceList>();
        Set(pl, "Id", Guid.NewGuid());
        Set(pl, "TenantId", tenantId);
        Set(pl, "PriceListName", "Offers PL");
        Set(pl, "Status", "ACTIVE");
        Set(pl, "Priority", 20);
        dbContext.PriceLists.Add(pl);

        var pli = CreateEntity<PriceListItem>();
        Set(pli, "Id", Guid.NewGuid());
        Set(pli, "TenantId", tenantId);
        Set(pli, "PriceListId", pl.Id);
        Set(pli, "ProductId", productId);
        Set(pli, "ProductVariantId", variantId);
        Set(pli, "SellingPrice", 700m);
        Set(pli, "CompareAtPrice", 1000m);
        Set(pli, "Status", "ACTIVE");
        dbContext.PriceListItems.Add(pli);

        await SeedPlatformSalesChannelAsync(dbContext, tenantId);
        await dbContext.SaveChangesAsync();

        var repo = new PosProductCatalogRepository(dbContext);
        var res = await repo.ListProductsAsync(tenantId, deviceId, null, null, CancellationToken.None, segment: "offers");

        Assert.True(res.IsSuccess);
        var summary = Assert.Single(res.Products);
        Assert.True(summary.HasOffer);
        Assert.Equal("SPECIAL_PRICE", summary.OfferType);
        Assert.Equal(1000, summary.OriginalPrice);
        Assert.Equal(700, summary.SellingPrice);
        Assert.Equal(700, summary.OfferPrice);
        Assert.Equal("30% OFF", summary.DiscountLabel);
        Assert.False(summary.RequiresCartValidation);
    }

    [Fact]
    public async Task ListProductsAsync_ManualLineDiscountPolicies_AreNotCatalogOffers()
    {
        var tenantId = Guid.NewGuid();
        var outletId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var variantId = Guid.NewGuid();

        await using var dbContext = CreateDbContext();
        await SeedDeviceAsync(dbContext, tenantId, outletId, deviceId);

        var product = Product.Create(productId, tenantId, "P-MANUAL", "Manual envelope product",
            "manual-envelope-product", "STANDARD", "SIMPLE", null, null, null, "Desc", null,
            true, true, ProductConstants.ActiveStatus, null, Now);
        dbContext.Products.Add(product);
        var variant = ProductVariant.Create(variantId, tenantId, productId, "DEFAULT",
            "Manual envelope product", "SKU-MANUAL", Guid.NewGuid(), Guid.NewGuid(), true, true,
            false, ProductConstants.ActiveStatus, null, Now);
        dbContext.ProductVariants.Add(variant);
        await SeedDefaultPriceListAsync(dbContext, tenantId, productId, variantId, 2800m);

        var fixedType = CreateEntity<DiscountType>();
        Set(fixedType, "Id", Guid.NewGuid());
        Set(fixedType, "CalculationMethod", "FIXED_AMOUNT");
        Set(fixedType, "Status", "ACTIVE");
        dbContext.DiscountTypes.Add(fixedType);

        var percentageType = CreateEntity<DiscountType>();
        Set(percentageType, "Id", Guid.NewGuid());
        Set(percentageType, "CalculationMethod", "PERCENTAGE");
        Set(percentageType, "Status", "ACTIVE");
        dbContext.DiscountTypes.Add(percentageType);

        foreach (var definition in new[]
                 {
                     (Id: Guid.NewGuid(), TypeId: fixedType.Id, Code: "POS_MANUAL_FIXED_LINE",
                         Name: "Manual Line Fixed Discount", Value: 10000m),
                     (Id: Guid.NewGuid(), TypeId: percentageType.Id, Code: "POS_MANUAL_PERCENTAGE_LINE",
                         Name: "Manual Line Percentage Discount", Value: 50m)
                 })
        {
            var policy = CreateEntity<DiscountPolicy>();
            Set(policy, "Id", definition.Id);
            Set(policy, "TenantId", tenantId);
            Set(policy, "DiscountTypeId", definition.TypeId);
            Set(policy, "DiscountPolicyCode", definition.Code);
            Set(policy, "DiscountPolicyName", definition.Name);
            Set(policy, "DiscountScope", "LINE");
            Set(policy, "DiscountValue", definition.Value);
            Set(policy, "Status", "ACTIVE");
            dbContext.DiscountPolicies.Add(policy);
        }

        await SeedPlatformSalesChannelAsync(dbContext, tenantId);
        await dbContext.SaveChangesAsync();

        var repository = new PosProductCatalogRepository(dbContext);
        var catalog = await repository.ListProductsAsync(
            tenantId, deviceId, null, null, CancellationToken.None);
        var offers = await repository.ListProductsAsync(
            tenantId, deviceId, null, null, CancellationToken.None, segment: "offers");

        Assert.True(catalog.IsSuccess);
        var summary = Assert.Single(catalog.Products);
        Assert.False(summary.HasOffer);
        Assert.Null(summary.OfferPolicyId);
        Assert.Null(summary.OfferPrice);
        Assert.Null(summary.DiscountLabel);
        Assert.True(offers.IsSuccess);
        Assert.Empty(offers.Products);
    }

    [Fact]
    public async Task ListProductsAsync_OffersSegment_AutomaticFixedPolicy_RemainsVisible()
    {
        var tenantId = Guid.NewGuid();
        var outletId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var variantId = Guid.NewGuid();

        await using var dbContext = CreateDbContext();
        await SeedDeviceAsync(dbContext, tenantId, outletId, deviceId);

        dbContext.Products.Add(Product.Create(productId, tenantId, "P-AUTO", "Automatic offer product",
            "automatic-offer-product", "STANDARD", "SIMPLE", null, null, null, "Desc", null,
            true, true, ProductConstants.ActiveStatus, null, Now));
        dbContext.ProductVariants.Add(ProductVariant.Create(variantId, tenantId, productId, "DEFAULT",
            "Automatic offer product", "SKU-AUTO", Guid.NewGuid(), Guid.NewGuid(), true, true,
            false, ProductConstants.ActiveStatus, null, Now));
        await SeedDefaultPriceListAsync(dbContext, tenantId, productId, variantId, 5000m);

        var type = CreateEntity<DiscountType>();
        Set(type, "Id", Guid.NewGuid());
        Set(type, "CalculationMethod", "FIXED_AMOUNT");
        Set(type, "Status", "ACTIVE");
        dbContext.DiscountTypes.Add(type);

        var policy = CreateEntity<DiscountPolicy>();
        Set(policy, "Id", Guid.NewGuid());
        Set(policy, "TenantId", tenantId);
        Set(policy, "DiscountTypeId", type.Id);
        Set(policy, "DiscountPolicyCode", "AUTO_PRODUCT_500");
        Set(policy, "DiscountPolicyName", "Automatic LKR 500 Off");
        Set(policy, "DiscountScope", "LINE");
        Set(policy, "DiscountValue", 500m);
        Set(policy, "Status", "ACTIVE");
        dbContext.DiscountPolicies.Add(policy);

        var target = CreateEntity<DiscountPolicyTarget>();
        Set(target, "Id", Guid.NewGuid());
        Set(target, "TenantId", tenantId);
        Set(target, "DiscountPolicyId", policy.Id);
        Set(target, "TargetType", "PRODUCT");
        Set(target, "TargetMode", "INCLUDE");
        Set(target, "ProductId", productId);
        Set(target, "Status", "ACTIVE");
        dbContext.DiscountPolicyTargets.Add(target);

        await SeedPlatformSalesChannelAsync(dbContext, tenantId);
        await dbContext.SaveChangesAsync();

        var repository = new PosProductCatalogRepository(dbContext);
        var result = await repository.ListProductsAsync(
            tenantId, deviceId, null, null, CancellationToken.None, segment: "offers");

        Assert.True(result.IsSuccess);
        var summary = Assert.Single(result.Products);
        Assert.True(summary.HasOffer);
        Assert.Equal("FIXED_AMOUNT", summary.OfferType);
        Assert.Equal(policy.Id, summary.OfferPolicyId);
        Assert.Equal(5000, summary.OriginalPrice);
        Assert.Equal(4500, summary.OfferPrice);
        Assert.Equal("LKR 500 OFF", summary.DiscountLabel);
    }

    [Fact]
    public async Task ListProductsAsync_OffersSegment_ResolvesDiscountPolicies_PercentageAndFixed()
    {
        var tenantId = Guid.NewGuid();
        var outletId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var variantId = Guid.NewGuid();

        await using var dbContext = CreateDbContext();
        await SeedDeviceAsync(dbContext, tenantId, outletId, deviceId);

        var p = Product.Create(productId, tenantId, "P-1", "Prod 1", "prod-1", "STANDARD", "SIMPLE", null, null, null, "Desc", null, true, true, ProductConstants.ActiveStatus, null, Now);
        dbContext.Products.Add(p);
        var v = ProductVariant.Create(variantId, tenantId, productId, "DEFAULT", "Prod 1", "SKU-1", Guid.NewGuid(), Guid.NewGuid(), true, true, false, ProductConstants.ActiveStatus, null, Now);
        dbContext.ProductVariants.Add(v);

        await SeedDefaultPriceListAsync(dbContext, tenantId, productId, variantId, 2000m);

        var dt = CreateEntity<DiscountType>();
        Set(dt, "Id", Guid.NewGuid());
        Set(dt, "CalculationMethod", "PERCENTAGE");
        Set(dt, "Status", "ACTIVE");
        dbContext.DiscountTypes.Add(dt);

        var dp = CreateEntity<DiscountPolicy>();
        Set(dp, "Id", Guid.NewGuid());
        Set(dp, "TenantId", tenantId);
        Set(dp, "DiscountTypeId", dt.Id);
        Set(dp, "DiscountPolicyCode", "DP-PCT");
        Set(dp, "DiscountPolicyName", "15% discount");
        Set(dp, "DiscountScope", "LINE");
        Set(dp, "DiscountValue", 15m);
        Set(dp, "Status", "ACTIVE");
        dbContext.DiscountPolicies.Add(dp);

        await SeedPlatformSalesChannelAsync(dbContext, tenantId);
        await dbContext.SaveChangesAsync();

        var repo = new PosProductCatalogRepository(dbContext);
        var res = await repo.ListProductsAsync(tenantId, deviceId, null, null, CancellationToken.None, segment: "offers");

        Assert.True(res.IsSuccess);
        var summary = Assert.Single(res.Products);
        Assert.True(summary.HasOffer);
        Assert.Equal("PERCENTAGE", summary.OfferType);
        Assert.Equal(2000, summary.OriginalPrice);
        Assert.Equal(1700, summary.OfferPrice);
        Assert.Equal("15% OFF", summary.DiscountLabel);
        Assert.False(summary.RequiresCartValidation);
    }

    [Fact]
    public async Task ListProductsAsync_OffersSegment_EvaluatesIncludeAndExcludeTargets()
    {
        var tenantId = Guid.NewGuid();
        var outletId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var p1 = Guid.NewGuid();
        var p2 = Guid.NewGuid();
        var v1 = Guid.NewGuid();
        var v2 = Guid.NewGuid();

        await using var dbContext = CreateDbContext();
        await SeedDeviceAsync(dbContext, tenantId, outletId, deviceId);

        // Product 1
        var prod1 = Product.Create(p1, tenantId, "P-1", "Prod 1", "prod-1", "STANDARD", "SIMPLE", null, null, null, "Desc", null, true, true, ProductConstants.ActiveStatus, null, Now);
        dbContext.Products.Add(prod1);
        var var1 = ProductVariant.Create(v1, tenantId, p1, "DEFAULT", "Prod 1", "SKU-1", Guid.NewGuid(), Guid.NewGuid(), true, true, false, ProductConstants.ActiveStatus, null, Now);
        dbContext.ProductVariants.Add(var1);
        await SeedDefaultPriceListAsync(dbContext, tenantId, p1, v1, 1000m);

        // Product 2 (Excluded)
        var prod2 = Product.Create(p2, tenantId, "P-2", "Prod 2", "prod-2", "STANDARD", "SIMPLE", null, null, null, "Desc", null, true, true, ProductConstants.ActiveStatus, null, Now);
        dbContext.Products.Add(prod2);
        var var2 = ProductVariant.Create(v2, tenantId, p2, "DEFAULT", "Prod 2", "SKU-2", Guid.NewGuid(), Guid.NewGuid(), true, true, false, ProductConstants.ActiveStatus, null, Now);
        dbContext.ProductVariants.Add(var2);
        await SeedDefaultPriceListAsync(dbContext, tenantId, p2, v2, 1000m);

        var dt = CreateEntity<DiscountType>();
        Set(dt, "Id", Guid.NewGuid());
        Set(dt, "CalculationMethod", "FIXED_AMOUNT");
        Set(dt, "Status", "ACTIVE");
        dbContext.DiscountTypes.Add(dt);

        var dp = CreateEntity<DiscountPolicy>();
        Set(dp, "Id", Guid.NewGuid());
        Set(dp, "TenantId", tenantId);
        Set(dp, "DiscountTypeId", dt.Id);
        Set(dp, "DiscountPolicyCode", "DP-FIX");
        Set(dp, "DiscountPolicyName", "100 LKR Off");
        Set(dp, "DiscountScope", "LINE");
        Set(dp, "DiscountValue", 100m);
        Set(dp, "Status", "ACTIVE");
        dbContext.DiscountPolicies.Add(dp);

        // Target Include Product 1
        var tInc = CreateEntity<DiscountPolicyTarget>();
        Set(tInc, "Id", Guid.NewGuid());
        Set(tInc, "TenantId", tenantId);
        Set(tInc, "DiscountPolicyId", dp.Id);
        Set(tInc, "TargetType", "PRODUCT");
        Set(tInc, "TargetMode", "INCLUDE");
        Set(tInc, "ProductId", p1);
        Set(tInc, "Status", "ACTIVE");
        dbContext.DiscountPolicyTargets.Add(tInc);

        // Target Exclude Product 2
        var tExc = CreateEntity<DiscountPolicyTarget>();
        Set(tExc, "Id", Guid.NewGuid());
        Set(tExc, "TenantId", tenantId);
        Set(tExc, "DiscountPolicyId", dp.Id);
        Set(tExc, "TargetType", "PRODUCT");
        Set(tExc, "TargetMode", "EXCLUDE");
        Set(tExc, "ProductId", p2);
        Set(tExc, "Status", "ACTIVE");
        dbContext.DiscountPolicyTargets.Add(tExc);

        await SeedPlatformSalesChannelAsync(dbContext, tenantId);
        await dbContext.SaveChangesAsync();

        var repo = new PosProductCatalogRepository(dbContext);
        var res = await repo.ListProductsAsync(tenantId, deviceId, null, null, CancellationToken.None, segment: "offers");

        Assert.True(res.IsSuccess);
        var summary = Assert.Single(res.Products);
        Assert.Equal(p1, summary.Id);
        Assert.True(summary.HasOffer);
        Assert.Equal(900, summary.OfferPrice);
    }

    [Fact]
    public async Task ListProductsAsync_OffersSegment_EvaluatesConditionsAndRequiresCartValidation()
    {
        var tenantId = Guid.NewGuid();
        var outletId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var variantId = Guid.NewGuid();

        await using var dbContext = CreateDbContext();
        await SeedDeviceAsync(dbContext, tenantId, outletId, deviceId);

        var p = Product.Create(productId, tenantId, "P-1", "Prod 1", "prod-1", "STANDARD", "SIMPLE", null, null, null, "Desc", null, true, true, ProductConstants.ActiveStatus, null, Now);
        dbContext.Products.Add(p);
        var v = ProductVariant.Create(variantId, tenantId, productId, "DEFAULT", "Prod 1", "SKU-1", Guid.NewGuid(), Guid.NewGuid(), true, true, false, ProductConstants.ActiveStatus, null, Now);
        dbContext.ProductVariants.Add(v);

        await SeedDefaultPriceListAsync(dbContext, tenantId, productId, variantId, 1500m);

        var dt = CreateEntity<DiscountType>();
        Set(dt, "Id", Guid.NewGuid());
        Set(dt, "CalculationMethod", "PERCENTAGE");
        Set(dt, "Status", "ACTIVE");
        dbContext.DiscountTypes.Add(dt);

        var dp = CreateEntity<DiscountPolicy>();
        Set(dp, "Id", Guid.NewGuid());
        Set(dp, "TenantId", tenantId);
        Set(dp, "DiscountTypeId", dt.Id);
        Set(dp, "DiscountPolicyCode", "DP-COND");
        Set(dp, "DiscountPolicyName", "20% Conditional Off");
        Set(dp, "DiscountScope", "LINE");
        Set(dp, "DiscountValue", 20m);
        Set(dp, "Status", "ACTIVE");
        dbContext.DiscountPolicies.Add(dp);

        // Condition: Min Quantity = 5
        var cond = CreateEntity<DiscountPolicyCondition>();
        Set(cond, "Id", Guid.NewGuid());
        Set(cond, "TenantId", tenantId);
        Set(cond, "DiscountPolicyId", dp.Id);
        Set(cond, "ConditionGroupNo", 1);
        Set(cond, "GroupOperator", "AND");
        Set(cond, "ConditionType", "MIN_QUANTITY");
        Set(cond, "ConditionOperator", ">=");
        Set(cond, "ConditionValueJson", "5");
        Set(cond, "SortOrder", 1);
        Set(cond, "Status", "ACTIVE");
        dbContext.DiscountPolicyConditions.Add(cond);

        await SeedPlatformSalesChannelAsync(dbContext, tenantId);
        await dbContext.SaveChangesAsync();

        var repo = new PosProductCatalogRepository(dbContext);
        var res = await repo.ListProductsAsync(tenantId, deviceId, null, null, CancellationToken.None, segment: "offers");

        Assert.True(res.IsSuccess);
        var summary = Assert.Single(res.Products);
        Assert.True(summary.HasOffer);
        Assert.True(summary.RequiresCartValidation);
        Assert.Null(summary.OfferPrice);
        Assert.Equal("Offer available", summary.DiscountLabel);
    }

    [Fact]
    public async Task ListProductsAsync_OffersSegment_ResolvesTieBreakerRules()
    {
        var tenantId = Guid.NewGuid();
        var outletId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var variantId = Guid.NewGuid();

        await using var dbContext = CreateDbContext();
        await SeedDeviceAsync(dbContext, tenantId, outletId, deviceId);

        var p = Product.Create(productId, tenantId, "P-1", "Prod 1", "prod-1", "STANDARD", "SIMPLE", null, null, null, "Desc", null, true, true, ProductConstants.ActiveStatus, null, Now);
        dbContext.Products.Add(p);
        var v = ProductVariant.Create(variantId, tenantId, productId, "DEFAULT", "Prod 1", "SKU-1", Guid.NewGuid(), Guid.NewGuid(), true, true, false, ProductConstants.ActiveStatus, null, Now);
        dbContext.ProductVariants.Add(v);

        await SeedDefaultPriceListAsync(dbContext, tenantId, productId, variantId, 1000m);

        var dt = CreateEntity<DiscountType>();
        Set(dt, "Id", Guid.NewGuid());
        Set(dt, "CalculationMethod", "FIXED_AMOUNT");
        Set(dt, "Status", "ACTIVE");
        dbContext.DiscountTypes.Add(dt);

        // Policy 1 (Priority 10, saving 200 => Price 800)
        var dp1 = CreateEntity<DiscountPolicy>();
        Set(dp1, "Id", Guid.NewGuid());
        Set(dp1, "TenantId", tenantId);
        Set(dp1, "DiscountTypeId", dt.Id);
        Set(dp1, "DiscountPolicyCode", "DP-1");
        Set(dp1, "DiscountPolicyName", "200 Off");
        Set(dp1, "DiscountScope", "LINE");
        Set(dp1, "DiscountValue", 200m);
        Set(dp1, "Priority", 10);
        Set(dp1, "Status", "ACTIVE");
        dbContext.DiscountPolicies.Add(dp1);

        // Policy 2 (Priority 20, saving 150 => Price 850)
        // Even though Policy 2 has higher priority, Policy 1 has a lower effective unit price, so Policy 1 wins!
        var dp2 = CreateEntity<DiscountPolicy>();
        Set(dp2, "Id", Guid.NewGuid());
        Set(dp2, "TenantId", tenantId);
        Set(dp2, "DiscountTypeId", dt.Id);
        Set(dp2, "DiscountPolicyCode", "DP-2");
        Set(dp2, "DiscountPolicyName", "150 Off");
        Set(dp2, "DiscountScope", "LINE");
        Set(dp2, "DiscountValue", 150m);
        Set(dp2, "Priority", 20);
        Set(dp2, "Status", "ACTIVE");
        dbContext.DiscountPolicies.Add(dp2);

        await SeedPlatformSalesChannelAsync(dbContext, tenantId);
        await dbContext.SaveChangesAsync();

        var repo = new PosProductCatalogRepository(dbContext);
        var res = await repo.ListProductsAsync(tenantId, deviceId, null, null, CancellationToken.None, segment: "offers");

        Assert.True(res.IsSuccess);
        var summary = Assert.Single(res.Products);
        Assert.Equal(800, summary.OfferPrice); // Lowest effective price wins!
    }

    [Fact]
    public async Task ListProductsAsync_OffersSegment_EnforcesOutletAndChannelLimits()
    {
        var tenantId = Guid.NewGuid();
        var outletId = Guid.NewGuid();
        var otherOutletId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var variantId = Guid.NewGuid();

        await using var dbContext = CreateDbContext();
        await SeedDeviceAsync(dbContext, tenantId, outletId, deviceId);

        var p = Product.Create(productId, tenantId, "P-1", "Prod 1", "prod-1", "STANDARD", "SIMPLE", null, null, null, "Desc", null, true, true, ProductConstants.ActiveStatus, null, Now);
        dbContext.Products.Add(p);
        var v = ProductVariant.Create(variantId, tenantId, productId, "DEFAULT", "Prod 1", "SKU-1", Guid.NewGuid(), Guid.NewGuid(), true, true, false, ProductConstants.ActiveStatus, null, Now);
        dbContext.ProductVariants.Add(v);

        await SeedDefaultPriceListAsync(dbContext, tenantId, productId, variantId, 1000m);

        var dt = CreateEntity<DiscountType>();
        Set(dt, "Id", Guid.NewGuid());
        Set(dt, "CalculationMethod", "PERCENTAGE");
        Set(dt, "Status", "ACTIVE");
        dbContext.DiscountTypes.Add(dt);

        // Policy restricted to otherOutletId
        var dp = CreateEntity<DiscountPolicy>();
        Set(dp, "Id", Guid.NewGuid());
        Set(dp, "TenantId", tenantId);
        Set(dp, "DiscountTypeId", dt.Id);
        Set(dp, "DiscountPolicyCode", "DP-OUTLET");
        Set(dp, "DiscountPolicyName", "Restricted");
        Set(dp, "DiscountScope", "LINE");
        Set(dp, "DiscountValue", 50m);
        Set(dp, "Status", "ACTIVE");
        dbContext.DiscountPolicies.Add(dp);

        var dpo = CreateEntity<DiscountPolicyOutlet>();
        Set(dpo, "Id", Guid.NewGuid());
        Set(dpo, "TenantId", tenantId);
        Set(dpo, "DiscountPolicyId", dp.Id);
        Set(dpo, "OutletId", otherOutletId);
        Set(dpo, "Status", "ACTIVE");
        dbContext.DiscountPolicyOutlets.Add(dpo);

        await SeedPlatformSalesChannelAsync(dbContext, tenantId);
        await dbContext.SaveChangesAsync();

        var repo = new PosProductCatalogRepository(dbContext);
        var res = await repo.ListProductsAsync(tenantId, deviceId, null, null, CancellationToken.None, segment: "offers");

        Assert.True(res.IsSuccess);
        Assert.Empty(res.Products); // No eligible offers since it's restricted to another outlet!
    }

    private static async Task SeedPlatformSalesChannelAsync(EPosDbContext dbContext, Guid tenantId)
    {
        var psc = CreateEntity<PlatformSalesChannel>();
        Set(psc, "Id", E_POS.Infrastructure.Persistence.Seed.PlatformSalesChannelSeedConstants.PhysicalChannelId);
        Set(psc, "ChannelCode", "POS");
        Set(psc, "ChannelType", "POS");
        Set(psc, "Status", "ACTIVE");
        dbContext.PlatformSalesChannels.Add(psc);

        var sc = CreateEntity<SalesChannel>();
        Set(sc, "Id", Guid.NewGuid());
        Set(sc, "TenantId", tenantId);
        Set(sc, "PlatformSalesChannelId", psc.Id);
        Set(sc, "Status", "ACTIVE");
        dbContext.SalesChannels.Add(sc);

        await dbContext.SaveChangesAsync();
    }

    private static T CreateEntity<T>() where T : new()
    {
        var entity = new T();
        Set(entity, "CreatedAt", Now);
        Set(entity, "UpdatedAt", Now);
        return entity;
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
