using System.Reflection;
using E_POS.Domain.Modules.ECommerce.Storefront.Entities;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using E_POS.Domain.Modules.Tenant.Inventory.Entities;
using E_POS.Domain.Modules.Tenant.PricingTax.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Constants;
using E_POS.Infrastructure.Modules.ECommerce.Storefront.Repositories;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;
using TenantEntity = E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant;

namespace E_POS.IntegrationTests.ECommerce.Storefront;

public sealed class StorefrontRepositoryTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 13, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task GetActiveBannersAsync_NormalizesTypeAndReturnsActiveTenantBannersInSortOrder()
    {
        var tenantId = Guid.NewGuid();
        var otherTenantId = Guid.NewGuid();
        await using var dbContext = CreateDbContext();
        var expectedSecond = StorefrontBanner.Create(tenantId, null, "HERO", "Second", null, "/second.jpg", null, null, 2, "ACTIVE");
        var expectedFirst = StorefrontBanner.Create(tenantId, null, "HERO", "First", null, "/first.jpg", null, null, 1, "ACTIVE");
        dbContext.StorefrontBanners.AddRange(
            expectedSecond,
            expectedFirst,
            StorefrontBanner.Create(tenantId, null, "HERO", "Inactive", null, "/inactive.jpg", null, null, 0, "INACTIVE"),
            StorefrontBanner.Create(otherTenantId, null, "HERO", "Other tenant", null, "/other.jpg", null, null, 0, "ACTIVE"),
            StorefrontBanner.Create(tenantId, null, "PROMO", "Wrong type", null, "/promo.jpg", null, null, 0, "ACTIVE"));
        await dbContext.SaveChangesAsync();
        var repository = new StorefrontRepository(dbContext);

        var result = (await repository.GetActiveBannersAsync(tenantId, " hero ", CancellationToken.None)).ToList();

        Assert.Collection(
            result,
            first => Assert.Equal(expectedFirst.Id, first.Id),
            second => Assert.Equal(expectedSecond.Id, second.Id));
    }

    [Fact]
    public async Task GetRootCategoriesAsync_ReturnsCurrentTenantActiveRootCategoriesWithActiveSellableItemCounts()
    {
        var tenantId = Guid.NewGuid();
        var otherTenantId = Guid.NewGuid();
        var rootTwo = CreateCategory(tenantId, "HEADWEAR", "Headwear", 2, CategoryConstants.ActiveStatus);
        var rootOne = CreateCategory(tenantId, "CLOTHING", "Clothing", 1, CategoryConstants.ActiveStatus, description: "Jerseys, T-Shirts, Hoodies & more");
        var child = CreateCategory(tenantId, "JERSEYS", "Jerseys", 0, CategoryConstants.ActiveStatus, rootOne.Id);
        var inactiveRoot = CreateCategory(tenantId, "INACTIVE", "Inactive", 0, CategoryConstants.InactiveStatus);
        var otherTenantRoot = CreateCategory(otherTenantId, "OTHER", "Other", 0, CategoryConstants.ActiveStatus);
        var activeProductId = Guid.NewGuid();
        var secondActiveProductId = Guid.NewGuid();
        var inactiveProductId = Guid.NewGuid();
        var notSellableProductId = Guid.NewGuid();
        var childProductId = Guid.NewGuid();

        await using var dbContext = CreateDbContext();
        dbContext.Categories.AddRange(rootTwo, rootOne, child, inactiveRoot, otherTenantRoot);
        dbContext.Products.AddRange(
            CreateProduct(tenantId, activeProductId, "JERSEY", "Jersey", true, ProductConstants.ActiveStatus),
            CreateProduct(tenantId, secondActiveProductId, "CAP", "Cap", true, ProductConstants.ActiveStatus),
            CreateProduct(tenantId, inactiveProductId, "OLD", "Old", true, ProductConstants.InactiveStatus),
            CreateProduct(tenantId, notSellableProductId, "NOSALE", "No Sale", false, ProductConstants.ActiveStatus),
            CreateProduct(tenantId, childProductId, "CHILD", "Child", true, ProductConstants.ActiveStatus),
            CreateProduct(otherTenantId, Guid.NewGuid(), "OTHER", "Other", true, ProductConstants.ActiveStatus));
        dbContext.ProductCategories.AddRange(
            ProductCategory.Create(Guid.NewGuid(), tenantId, activeProductId, rootOne.Id, true, 0, null, Now),
            ProductCategory.Create(Guid.NewGuid(), tenantId, secondActiveProductId, rootTwo.Id, true, 0, null, Now),
            ProductCategory.Create(Guid.NewGuid(), tenantId, inactiveProductId, rootOne.Id, true, 0, null, Now),
            ProductCategory.Create(Guid.NewGuid(), tenantId, notSellableProductId, rootOne.Id, true, 0, null, Now),
            ProductCategory.Create(Guid.NewGuid(), tenantId, childProductId, child.Id, true, 0, null, Now));
        await dbContext.SaveChangesAsync();
        var repository = new StorefrontRepository(dbContext);

        var result = (await repository.GetRootCategoriesAsync(tenantId, CancellationToken.None)).ToList();

        Assert.Collection(
            result,
            first =>
            {
                Assert.Equal(rootOne.Id, first.Id);
                Assert.Equal("Clothing", first.Name);
                Assert.Equal("clothing", first.Slug);
                Assert.Equal("Jerseys, T-Shirts, Hoodies & more", first.Description);
                Assert.Equal("/images/clothing.jpg", first.ImageUrl);
                Assert.Equal(1, first.ItemCount);
                Assert.Equal(1, first.SortOrder);
            },
            second =>
            {
                Assert.Equal(rootTwo.Id, second.Id);
                Assert.Equal("Headwear", second.Name);
                Assert.Equal(1, second.ItemCount);
                Assert.Equal(2, second.SortOrder);
            });
        Assert.DoesNotContain(result, x => x.Id == child.Id);
        Assert.DoesNotContain(result, x => x.Id == inactiveRoot.Id);
        Assert.DoesNotContain(result, x => x.Id == otherTenantRoot.Id);
    }

    [Fact]
    public async Task GetChildCategoriesAsync_ReturnsCurrentTenantActiveChildrenForSelectedParentWithActiveSellableItemCounts()
    {
        var tenantId = Guid.NewGuid();
        var otherTenantId = Guid.NewGuid();
        var parent = CreateCategory(tenantId, "CLOTHING", "Clothing", 1, CategoryConstants.ActiveStatus);
        var otherParent = CreateCategory(tenantId, "HEADWEAR", "Headwear", 2, CategoryConstants.ActiveStatus);
        var expectedSecond = CreateCategory(tenantId, "TSHIRTS", "T-Shirts", 2, CategoryConstants.ActiveStatus, parent.Id);
        var expectedFirst = CreateCategory(tenantId, "JERSEYS", "Jerseys", 1, CategoryConstants.ActiveStatus, parent.Id, description: "Official jerseys");
        var inactiveChild = CreateCategory(tenantId, "OLD", "Old", 0, CategoryConstants.InactiveStatus, parent.Id);
        var otherParentChild = CreateCategory(tenantId, "CAPS", "Caps", 0, CategoryConstants.ActiveStatus, otherParent.Id);
        var otherTenantChild = CreateCategory(otherTenantId, "OTHER", "Other", 0, CategoryConstants.ActiveStatus, parent.Id);
        var jerseysProductId = Guid.NewGuid();
        var tshirtsProductId = Guid.NewGuid();
        var inactiveProductId = Guid.NewGuid();
        var notSellableProductId = Guid.NewGuid();
        var parentProductId = Guid.NewGuid();

        await using var dbContext = CreateDbContext();
        dbContext.Categories.AddRange(parent, otherParent, expectedSecond, expectedFirst, inactiveChild, otherParentChild, otherTenantChild);
        dbContext.Products.AddRange(
            CreateProduct(tenantId, jerseysProductId, "JERSEY", "Jersey", true, ProductConstants.ActiveStatus),
            CreateProduct(tenantId, tshirtsProductId, "TEE", "Tee", true, ProductConstants.ActiveStatus),
            CreateProduct(tenantId, inactiveProductId, "OLDPRODUCT", "Old Product", true, ProductConstants.InactiveStatus),
            CreateProduct(tenantId, notSellableProductId, "NOSALE", "No Sale", false, ProductConstants.ActiveStatus),
            CreateProduct(tenantId, parentProductId, "PARENT", "Parent Product", true, ProductConstants.ActiveStatus),
            CreateProduct(otherTenantId, Guid.NewGuid(), "OTHER", "Other", true, ProductConstants.ActiveStatus));
        dbContext.ProductCategories.AddRange(
            ProductCategory.Create(Guid.NewGuid(), tenantId, jerseysProductId, expectedFirst.Id, true, 0, null, Now),
            ProductCategory.Create(Guid.NewGuid(), tenantId, tshirtsProductId, expectedSecond.Id, true, 0, null, Now),
            ProductCategory.Create(Guid.NewGuid(), tenantId, inactiveProductId, expectedFirst.Id, true, 0, null, Now),
            ProductCategory.Create(Guid.NewGuid(), tenantId, notSellableProductId, expectedFirst.Id, true, 0, null, Now),
            ProductCategory.Create(Guid.NewGuid(), tenantId, parentProductId, parent.Id, true, 0, null, Now));
        await dbContext.SaveChangesAsync();
        var repository = new StorefrontRepository(dbContext);

        var result = (await repository.GetChildCategoriesAsync(tenantId, parent.Id, CancellationToken.None)).ToList();

        Assert.Collection(
            result,
            first =>
            {
                Assert.Equal(expectedFirst.Id, first.Id);
                Assert.Equal("Jerseys", first.Name);
                Assert.Equal("jerseys", first.Slug);
                Assert.Equal("Official jerseys", first.Description);
                Assert.Equal("/images/jerseys.jpg", first.ImageUrl);
                Assert.Equal(1, first.ItemCount);
                Assert.Equal(1, first.SortOrder);
            },
            second =>
            {
                Assert.Equal(expectedSecond.Id, second.Id);
                Assert.Equal("T-Shirts", second.Name);
                Assert.Equal(1, second.ItemCount);
                Assert.Equal(2, second.SortOrder);
            });
        Assert.DoesNotContain(result, x => x.Id == parent.Id);
        Assert.DoesNotContain(result, x => x.Id == inactiveChild.Id);
        Assert.DoesNotContain(result, x => x.Id == otherParentChild.Id);
        Assert.DoesNotContain(result, x => x.Id == otherTenantChild.Id);
    }

    [Fact]
    public async Task GetFeaturedCategoriesAsync_ReturnsActiveCurrentTenantCategoriesInSortOrder()
    {
        var tenantId = Guid.NewGuid();
        var otherTenantId = Guid.NewGuid();
        await using var dbContext = CreateDbContext();
        var expectedSecond = CreateCategory(tenantId, "SECOND", "Second", 2, CategoryConstants.ActiveStatus);
        var expectedFirst = CreateCategory(tenantId, "FIRST", "First", 1, CategoryConstants.ActiveStatus);
        dbContext.Categories.AddRange(
            expectedSecond,
            expectedFirst,
            CreateCategory(tenantId, "INACTIVE", "Inactive", 0, CategoryConstants.InactiveStatus),
            CreateCategory(otherTenantId, "OTHER", "Other", 0, CategoryConstants.ActiveStatus));
        await dbContext.SaveChangesAsync();
        var repository = new StorefrontRepository(dbContext);

        var result = (await repository.GetFeaturedCategoriesAsync(tenantId, CancellationToken.None)).ToList();

        Assert.Collection(
            result,
            first => Assert.Equal(expectedFirst.Id, first.Id),
            second => Assert.Equal(expectedSecond.Id, second.Id));
    }

    [Fact]
    public async Task GetTenantIdBySlugAsync_ReturnsTrimmedActiveTenantOnly()
    {
        var activeTenantId = Guid.NewGuid();
        await using var dbContext = CreateDbContext();
        dbContext.Tenants.AddRange(
            TenantEntity.Create(activeTenantId, "DEMO", "demo-store", "Demo Store", TenantStatusConstants.Active, "LKR", "Asia/Colombo", null, null, Now),
            TenantEntity.Create(Guid.NewGuid(), "OLD", "old-store", "Old Store", TenantStatusConstants.Suspended, "LKR", "Asia/Colombo", null, null, Now));
        await dbContext.SaveChangesAsync();
        var repository = new StorefrontRepository(dbContext);

        var activeResult = await repository.GetTenantIdBySlugAsync(" demo-store ", CancellationToken.None);
        var inactiveResult = await repository.GetTenantIdBySlugAsync("old-store", CancellationToken.None);

        Assert.Equal(activeTenantId, activeResult);
        Assert.Null(inactiveResult);
    }

    [Fact]
    public async Task GetProductsAsync_ReturnsPagedCurrentTenantActiveSellableProductsForCategoryWithDetails()
    {
        var tenantId = Guid.NewGuid();
        var otherTenantId = Guid.NewGuid();
        var category = CreateCategory(tenantId, "JERSEYS", "Jerseys", 1, CategoryConstants.ActiveStatus);
        var otherCategory = CreateCategory(tenantId, "HEADWEAR", "Headwear", 2, CategoryConstants.ActiveStatus);
        var homeProductId = Guid.NewGuid();
        var kidsProductId = Guid.NewGuid();
        var inactiveProductId = Guid.NewGuid();
        var notSellableProductId = Guid.NewGuid();
        var otherCategoryProductId = Guid.NewGuid();
        var otherTenantProductId = Guid.NewGuid();
        var homeRating = ProductRatingSummary.Create(tenantId, homeProductId);
        Set(homeRating, "AverageRating", 4.8m);
        Set(homeRating, "TotalReviews", 128);
        var homeInventory = InventoryBalance.Create(Guid.NewGuid(), tenantId, Guid.NewGuid(), homeProductId, null, null, Now);
        homeInventory.AdjustQuantities(5m, 0m, 0m, 0m, Now);
        var kidsInventory = InventoryBalance.Create(Guid.NewGuid(), tenantId, Guid.NewGuid(), kidsProductId, null, null, Now);

        await using var dbContext = CreateDbContext();
        dbContext.Categories.AddRange(category, otherCategory);
        dbContext.Products.AddRange(
            CreateProduct(tenantId, homeProductId, "HOME", "Man City Home Jersey 2024/25", true, ProductConstants.ActiveStatus, "Sky Blue"),
            CreateProduct(tenantId, kidsProductId, "KIDS", "Man City Kids Home Jersey 2024/25", true, ProductConstants.ActiveStatus, "Sky Blue"),
            CreateProduct(tenantId, inactiveProductId, "OLD", "Old Jersey", true, ProductConstants.InactiveStatus, "Old"),
            CreateProduct(tenantId, notSellableProductId, "NOSALE", "No Sale Jersey", false, ProductConstants.ActiveStatus, "No Sale"),
            CreateProduct(tenantId, otherCategoryProductId, "CAP", "Cap", true, ProductConstants.ActiveStatus, "Black"),
            CreateProduct(otherTenantId, otherTenantProductId, "OTHER", "Other Tenant Jersey", true, ProductConstants.ActiveStatus, "Other"));
        dbContext.ProductCategories.AddRange(
            ProductCategory.Create(Guid.NewGuid(), tenantId, homeProductId, category.Id, true, 2, null, Now),
            ProductCategory.Create(Guid.NewGuid(), tenantId, kidsProductId, category.Id, true, 1, null, Now),
            ProductCategory.Create(Guid.NewGuid(), tenantId, inactiveProductId, category.Id, true, 0, null, Now),
            ProductCategory.Create(Guid.NewGuid(), tenantId, notSellableProductId, category.Id, true, 0, null, Now),
            ProductCategory.Create(Guid.NewGuid(), tenantId, otherCategoryProductId, otherCategory.Id, true, 0, null, Now),
            ProductCategory.Create(Guid.NewGuid(), otherTenantId, otherTenantProductId, category.Id, true, 0, null, Now));
        dbContext.PriceListItems.AddRange(
            PriceListItem.Create(Guid.NewGuid(), tenantId, Guid.NewGuid(), homeProductId, null, null, 74.99m, null, 1m, Now.AddDays(-2), null, "ACTIVE", null, Now),
            PriceListItem.Create(Guid.NewGuid(), tenantId, Guid.NewGuid(), kidsProductId, null, null, 54.99m, null, 1m, Now.AddDays(-2), null, "ACTIVE", null, Now));
        dbContext.ProductImages.Add(
            ProductImage.Create(Guid.NewGuid(), tenantId, homeProductId, null, null, "home-main", "/images/home.jpg", null, "MAIN", "image/jpeg", null, null, null, null, 1, true, "ACTIVE", null, Now));
        dbContext.ProductRatingSummaries.Add(homeRating);
        dbContext.InventoryBalances.AddRange(homeInventory, kidsInventory);
        await dbContext.SaveChangesAsync();
        var repository = new StorefrontRepository(dbContext);

        var result = await repository.GetProductsAsync(tenantId, category.Id, "price_desc", 1, 20, CancellationToken.None);
        var secondPage = await repository.GetProductsAsync(tenantId, category.Id, "price_desc", 2, 1, CancellationToken.None);

        Assert.Equal(2, result.TotalCount);
        Assert.Equal(1, result.Page);
        Assert.Equal(20, result.PageSize);
        Assert.Collection(
            result.Items,
            first =>
            {
                Assert.Equal(homeProductId, first.Id);
                Assert.Equal("Man City Home Jersey 2024/25", first.Name);
                Assert.Equal("home", first.Slug);
                Assert.Equal("Sky Blue", first.ShortDescription);
                Assert.Equal(74.99m, first.Price);
                Assert.Equal("/images/home.jpg", first.ImageUrl);
                Assert.Equal(4.8m, first.Rating);
                Assert.Equal(128, first.ReviewCount);
                Assert.True(first.IsInStock);
                Assert.Equal("Best Seller", first.Badge);
            },
            second =>
            {
                Assert.Equal(kidsProductId, second.Id);
                Assert.Equal(54.99m, second.Price);
                Assert.Equal("https://via.placeholder.com/300", second.ImageUrl);
                Assert.False(second.IsInStock);
                Assert.Null(second.Badge);
            });
        var pagedItem = Assert.Single(secondPage.Items);
        Assert.Equal(kidsProductId, pagedItem.Id);
        Assert.Equal(2, secondPage.TotalCount);
        Assert.Equal(2, secondPage.Page);
        Assert.Equal(1, secondPage.PageSize);
    }

    [Fact]
    public async Task GetProductDetailAsync_ReturnsCurrentTenantActiveSellableProductWithImagesVariantsOptionsStockAndHighlights()
    {
        var tenantId = Guid.NewGuid();
        var otherTenantId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var returnPolicyId = Guid.NewGuid();
        var smallVariantId = Guid.NewGuid();
        var mediumVariantId = Guid.NewGuid();
        var stockUomId = Guid.NewGuid();
        var salesUomId = Guid.NewGuid();
        var colourOptionId = Guid.NewGuid();
        var sizeOptionId = Guid.NewGuid();
        var skyBlueValueId = Guid.NewGuid();
        var smallValueId = Guid.NewGuid();
        var mediumValueId = Guid.NewGuid();
        var rating = ProductRatingSummary.Create(tenantId, productId);
        Set(rating, "AverageRating", 4.8m);
        Set(rating, "TotalReviews", 128);
        var smallInventory = InventoryBalance.Create(Guid.NewGuid(), tenantId, Guid.NewGuid(), productId, smallVariantId, null, Now);
        smallInventory.AdjustQuantities(5m, 0m, 0m, 0m, Now);
        var mediumInventory = InventoryBalance.Create(Guid.NewGuid(), tenantId, Guid.NewGuid(), productId, mediumVariantId, null, Now);
        var featureDefinition = ProductAttributeDefinition.Create(
            Guid.NewGuid(),
            tenantId,
            "feature-1",
            "Feature",
            "TEXT",
            null,
            false,
            false,
            false,
            "PRODUCT",
            1,
            ProductConstants.ActiveStatus,
            null,
            Now);

        await using var dbContext = CreateDbContext();
        dbContext.ReturnPolicies.Add(ReturnPolicy.Create(
            returnPolicyId,
            tenantId,
            "STANDARD",
            "Standard Returns",
            null,
            30,
            30,
            true,
            true,
            false,
            true,
            ProductConstants.ActiveStatus,
            null,
            Now));
        dbContext.Products.AddRange(
            CreateProduct(
                tenantId,
                productId,
                "HOME",
                "Man City Home Jersey 2024/25",
                true,
                ProductConstants.ActiveStatus,
                "Sky Blue",
                "Show your City pride with the official Manchester City Home Jersey 2024/25.",
                returnPolicyId),
            CreateProduct(tenantId, Guid.NewGuid(), "OLDDETAIL", "Old Detail", true, ProductConstants.InactiveStatus),
            CreateProduct(tenantId, Guid.NewGuid(), "NOSALEDETAIL", "No Sale Detail", false, ProductConstants.ActiveStatus),
            CreateProduct(otherTenantId, Guid.NewGuid(), "OTHERDETAIL", "Other Detail", true, ProductConstants.ActiveStatus));
        dbContext.PriceListItems.AddRange(
            PriceListItem.Create(Guid.NewGuid(), tenantId, Guid.NewGuid(), productId, null, null, 74.99m, null, 1m, Now.AddDays(-2), null, "ACTIVE", null, Now),
            PriceListItem.Create(Guid.NewGuid(), tenantId, Guid.NewGuid(), productId, mediumVariantId, null, 79.99m, null, 1m, Now.AddDays(-2), null, "ACTIVE", null, Now));
        dbContext.ProductImages.AddRange(
            ProductImage.Create(Guid.NewGuid(), tenantId, productId, null, null, "home-alt", "/images/home-alt.jpg", "Side", "MAIN", "image/jpeg", null, null, null, null, 1, false, "ACTIVE", null, Now),
            ProductImage.Create(Guid.NewGuid(), tenantId, productId, null, null, "home-main", "/images/home-main.jpg", "Front", "MAIN", "image/jpeg", null, null, null, null, 2, true, "ACTIVE", null, Now));
        dbContext.ProductRatingSummaries.Add(rating);
        dbContext.ProductOptions.AddRange(
            ProductOption.Create(colourOptionId, tenantId, productId, null, "COLOUR", "Colour", "VARIANT", "SWATCH", true, 1, ProductConstants.ActiveStatus, null, Now),
            ProductOption.Create(sizeOptionId, tenantId, productId, null, "SIZE", "Size", "VARIANT", "BUTTON", true, 2, ProductConstants.ActiveStatus, null, Now));
        dbContext.ProductOptionValues.AddRange(
            ProductOptionValue.Create(skyBlueValueId, tenantId, colourOptionId, null, "SKY_BLUE", "Sky Blue", "Sky Blue", "#8FD3FF", null, 1, ProductConstants.ActiveStatus, null, Now),
            ProductOptionValue.Create(smallValueId, tenantId, sizeOptionId, null, "S", "S", "S", null, null, 1, ProductConstants.ActiveStatus, null, Now),
            ProductOptionValue.Create(mediumValueId, tenantId, sizeOptionId, null, "M", "M", "M", null, null, 2, ProductConstants.ActiveStatus, null, Now));
        dbContext.ProductVariants.AddRange(
            ProductVariant.Create(smallVariantId, tenantId, productId, "HOME-S", "Sky Blue / S", "SKU-HOME-S", stockUomId, salesUomId, true, true, false, ProductConstants.ActiveStatus, null, Now),
            ProductVariant.Create(mediumVariantId, tenantId, productId, "HOME-M", "Sky Blue / M", "SKU-HOME-M", stockUomId, salesUomId, false, true, false, ProductConstants.ActiveStatus, null, Now));
        dbContext.ProductVariantOptionValues.AddRange(
            ProductVariantOptionValue.Create(Guid.NewGuid(), tenantId, productId, smallVariantId, colourOptionId, skyBlueValueId, null, Now),
            ProductVariantOptionValue.Create(Guid.NewGuid(), tenantId, productId, smallVariantId, sizeOptionId, smallValueId, null, Now),
            ProductVariantOptionValue.Create(Guid.NewGuid(), tenantId, productId, mediumVariantId, colourOptionId, skyBlueValueId, null, Now),
            ProductVariantOptionValue.Create(Guid.NewGuid(), tenantId, productId, mediumVariantId, sizeOptionId, mediumValueId, null, Now));
        dbContext.InventoryBalances.AddRange(smallInventory, mediumInventory);
        dbContext.ProductAttributeDefinitions.Add(featureDefinition);
        dbContext.ProductAttributeValues.Add(ProductAttributeValue.Create(
            Guid.NewGuid(),
            tenantId,
            productId,
            null,
            featureDefinition.Id,
            "Lightweight, breathable fabric",
            null,
            null,
            null,
            null,
            ProductConstants.ActiveStatus,
            null,
            Now));
        await dbContext.SaveChangesAsync();
        var repository = new StorefrontRepository(dbContext);

        var result = await repository.GetProductDetailAsync(tenantId, " HOME ", CancellationToken.None);
        var otherTenantResult = await repository.GetProductDetailAsync(otherTenantId, "home", CancellationToken.None);
        var inactiveResult = await repository.GetProductDetailAsync(tenantId, "olddetail", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(productId, result.Id);
        Assert.Equal("Man City Home Jersey 2024/25", result.Name);
        Assert.Equal("home", result.Slug);
        Assert.Equal("Sky Blue", result.ShortDescription);
        Assert.Equal("Show your City pride with the official Manchester City Home Jersey 2024/25.", result.LongDescription);
        Assert.Equal(74.99m, result.Price);
        Assert.Equal(4.8m, result.Rating);
        Assert.Equal(128, result.ReviewCount);
        Assert.True(result.IsInStock);
        Assert.Equal("Best Seller", result.Badge);
        Assert.Collection(
            result.Images,
            first =>
            {
                Assert.Equal("/images/home-main.jpg", first.Url);
                Assert.Equal("Front", first.AltText);
                Assert.True(first.IsPrimary);
            },
            second =>
            {
                Assert.Equal("/images/home-alt.jpg", second.Url);
                Assert.Equal("Side", second.AltText);
                Assert.False(second.IsPrimary);
            });
        var colour = Assert.Single(result.Colours);
        Assert.Equal("Sky Blue", colour.DisplayName);
        Assert.Equal("#8FD3FF", colour.ColorHex);
        Assert.Collection(
            result.Sizes,
            first => Assert.Equal("S", first.DisplayName),
            second => Assert.Equal("M", second.DisplayName));
        Assert.Collection(
            result.Variants,
            first =>
            {
                Assert.Equal(smallVariantId, first.Id);
                Assert.Equal("SKU-HOME-S", first.Sku);
                Assert.Equal("Sky Blue", first.Colour);
                Assert.Equal("S", first.Size);
                Assert.Equal(74.99m, first.Price);
                Assert.True(first.IsDefault);
                Assert.True(first.IsInStock);
            },
            second =>
            {
                Assert.Equal(mediumVariantId, second.Id);
                Assert.Equal("M", second.Size);
                Assert.Equal(79.99m, second.Price);
                Assert.False(second.IsDefault);
                Assert.False(second.IsInStock);
            });
        Assert.Contains("Lightweight, breathable fabric", result.Highlights);
        Assert.Contains("Click & Collect", result.DeliveryInfo);
        Assert.Contains("30", result.ReturnInfo);
        Assert.Null(otherTenantResult);
        Assert.Null(inactiveResult);
    }
    [Fact]
    public async Task GetBestSellersAsync_ReturnsActiveSellableProductsWithCurrentPriceAndPrimaryImage()
    {
        var tenantId = Guid.NewGuid();
        var otherTenantId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var olderPrice = PriceListItem.Create(Guid.NewGuid(), tenantId, Guid.NewGuid(), productId, null, null, 10m, null, 1m, null, null, "ACTIVE", null, Now);
        var latestPrice = PriceListItem.Create(Guid.NewGuid(), tenantId, Guid.NewGuid(), productId, null, null, 12.50m, null, 1m, DateTimeOffset.UtcNow.AddDays(-1), null, "ACTIVE", null, Now);
        var rating = ProductRatingSummary.Create(tenantId, productId);
        Set(rating, "AverageRating", 4.25m);
        Set(rating, "TotalReviews", 7);

        await using var dbContext = CreateDbContext();
        dbContext.Products.AddRange(
            CreateProduct(tenantId, productId, "APPLE", "Apple", true, ProductConstants.ActiveStatus),
            CreateProduct(tenantId, Guid.NewGuid(), "DRAFT", "Draft", true, ProductConstants.InactiveStatus),
            CreateProduct(tenantId, Guid.NewGuid(), "NOSALE", "No Sale", false, ProductConstants.ActiveStatus),
            CreateProduct(otherTenantId, Guid.NewGuid(), "OTHER", "Other", true, ProductConstants.ActiveStatus));
        dbContext.PriceListItems.AddRange(olderPrice, latestPrice);
        dbContext.ProductImages.AddRange(
            ProductImage.Create(Guid.NewGuid(), tenantId, productId, null, null, "apple-main", "/images/apple-main.jpg", null, "MAIN", "image/jpeg", null, null, null, null, 2, true, "ACTIVE", null, Now),
            ProductImage.Create(Guid.NewGuid(), tenantId, productId, null, null, "apple-alt", "/images/apple-alt.jpg", null, "MAIN", "image/jpeg", null, null, null, null, 1, false, "ACTIVE", null, Now));
        dbContext.ProductRatingSummaries.Add(rating);
        await dbContext.SaveChangesAsync();
        var repository = new StorefrontRepository(dbContext);

        var result = (await repository.GetBestSellersAsync(tenantId, CancellationToken.None)).ToList();

        var item = Assert.Single(result);
        Assert.Equal(productId, item.Product.Id);
        Assert.Equal(12.50m, item.SellingPrice);
        Assert.Equal("/images/apple-main.jpg", item.PrimaryImageUrl);
        Assert.Equal(4.25m, item.Rating?.AverageRating);
        Assert.Equal(7, item.Rating?.TotalReviews);
    }

    private static Category CreateCategory(
        Guid tenantId,
        string code,
        string name,
        int sortOrder,
        string status,
        Guid? parentCategoryId = null,
        string? description = null)
    {
        return Category.Create(
            Guid.NewGuid(),
            tenantId,
            Guid.NewGuid(),
            parentCategoryId,
            code,
            name,
            code.ToLowerInvariant(),
            description,
            $"/images/{code.ToLowerInvariant()}.jpg",
            sortOrder,
            status,
            null,
            Now);
    }

    private static Product CreateProduct(
        Guid tenantId,
        Guid productId,
        string code,
        string name,
        bool isSellable,
        string status,
        string? shortDescription = null,
        string? longDescription = null,
        Guid? returnPolicyId = null)
    {
        return Product.Create(productId, tenantId, code, name, code.ToLowerInvariant(), "STANDARD", "SIMPLE", null, null, returnPolicyId, shortDescription, longDescription, isSellable, true, status, null, Now);
    }
    private static void Set<T>(object entity, string propertyName, T value)
    {
        var property = entity.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        property?.SetValue(entity, value);
    }

    private static EPosDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<EPosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new EPosDbContext(options);
    }
}