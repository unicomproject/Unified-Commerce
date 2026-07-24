using E_POS.Domain.Modules.Shared.Media.Entities;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using E_POS.Infrastructure.Modules.Tenant.CatalogProduct.Repositories;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace E_POS.IntegrationTests.CatalogProduct;

public sealed class TenantAdminProductImageProjectionTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 24, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task GetPrimaryImageUrlsAsync_PrefersProductImage_AndFallsBackToVariantAndLegacy()
    {
        var tenantId = Guid.NewGuid();
        var otherTenantId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var variantOnlyProductId = Guid.NewGuid();
        var legacyProductId = Guid.NewGuid();
        var noImageProductId = Guid.NewGuid();
        await using var dbContext = CreateDbContext();

        AddProduct(dbContext, tenantId, productId, "PRODUCT");
        AddProduct(dbContext, tenantId, variantOnlyProductId, "VARIANT");
        AddProduct(dbContext, tenantId, legacyProductId, "LEGACY");
        AddProduct(dbContext, tenantId, noImageProductId, "NO-IMAGE");

        var productAsset = AddMediaAsset(
            dbContext,
            tenantId,
            "https://cdn.example.test/product-primary.png");
        AddProductImage(
            dbContext,
            tenantId,
            productId,
            productAsset.Id,
            productVariantId: null,
            isPrimary: true,
            sortOrder: 10,
            legacyUrl: "https://legacy.example.test/product-primary.png");

        var variantId = Guid.NewGuid();
        var variantAsset = AddMediaAsset(
            dbContext,
            tenantId,
            "https://cdn.example.test/variant-primary.png");
        AddProductImage(
            dbContext,
            tenantId,
            productId,
            variantAsset.Id,
            variantId,
            isPrimary: true,
            sortOrder: 0);

        var variantOnlyAsset = AddMediaAsset(
            dbContext,
            tenantId,
            "https://cdn.example.test/variant-only.png");
        AddProductImage(
            dbContext,
            tenantId,
            variantOnlyProductId,
            variantOnlyAsset.Id,
            Guid.NewGuid(),
            isPrimary: true,
            sortOrder: 0);

        AddProductImage(
            dbContext,
            tenantId,
            legacyProductId,
            mediaAssetId: null,
            productVariantId: null,
            isPrimary: true,
            sortOrder: 0,
            legacyUrl: "https://legacy.example.test/legacy-product.png");

        var inactiveAsset = AddMediaAsset(
            dbContext,
            tenantId,
            "https://cdn.example.test/inactive.png");
        inactiveAsset.MarkInactive(updatedByTenantUserId: null, Now);
        AddProductImage(
            dbContext,
            tenantId,
            legacyProductId,
            inactiveAsset.Id,
            productVariantId: null,
            isPrimary: true,
            sortOrder: -1,
            legacyUrl: "https://legacy.example.test/must-not-leak.png");

        var crossTenantAsset = AddMediaAsset(
            dbContext,
            otherTenantId,
            "https://cdn.example.test/cross-tenant.png");
        AddProductImage(
            dbContext,
            otherTenantId,
            productId,
            crossTenantAsset.Id,
            productVariantId: null,
            isPrimary: true,
            sortOrder: -10);

        await dbContext.SaveChangesAsync();
        var repository = new TenantAdminProductRepository(dbContext);

        var result = await repository.GetPrimaryImageUrlsAsync(
            tenantId,
            [productId, variantOnlyProductId, legacyProductId, noImageProductId],
            CancellationToken.None);

        Assert.Equal("https://cdn.example.test/product-primary.png", result[productId]);
        Assert.Equal("https://cdn.example.test/variant-only.png", result[variantOnlyProductId]);
        Assert.Equal("https://legacy.example.test/legacy-product.png", result[legacyProductId]);
        Assert.DoesNotContain(noImageProductId, result.Keys);
    }

    [Fact]
    public async Task GetDetailAsync_GroupsProductAndVariantImages_AndExcludesInactiveRows()
    {
        var tenantId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        await using var dbContext = CreateDbContext();

        AddProduct(dbContext, tenantId, productId, "DETAIL");
        dbContext.ProductVariants.Add(ProductVariant.Create(
            variantId,
            tenantId,
            productId,
            "DETAIL-VARIANT",
            "Detail Variant",
            "DETAIL-SKU",
            Guid.NewGuid(),
            Guid.NewGuid(),
            true,
            true,
            false,
            ProductConstants.ActiveStatus,
            createdByTenantUserId: null,
            Now));

        var primaryAsset = AddMediaAsset(
            dbContext,
            tenantId,
            "https://cdn.example.test/detail-primary.png");
        var primaryImage = AddProductImage(
            dbContext,
            tenantId,
            productId,
            primaryAsset.Id,
            productVariantId: null,
            isPrimary: true,
            sortOrder: 5,
            legacyUrl: "https://legacy.example.test/detail-primary.png");

        var secondaryImage = AddProductImage(
            dbContext,
            tenantId,
            productId,
            mediaAssetId: null,
            productVariantId: null,
            isPrimary: false,
            sortOrder: 1,
            legacyUrl: "https://legacy.example.test/detail-secondary.png");

        var variantAsset = AddMediaAsset(
            dbContext,
            tenantId,
            "https://cdn.example.test/detail-variant.png");
        var variantImage = AddProductImage(
            dbContext,
            tenantId,
            productId,
            variantAsset.Id,
            variantId,
            isPrimary: true,
            sortOrder: 0);

        var inactiveAsset = AddMediaAsset(
            dbContext,
            tenantId,
            "https://cdn.example.test/detail-inactive.png");
        inactiveAsset.MarkInactive(updatedByTenantUserId: null, Now);
        AddProductImage(
            dbContext,
            tenantId,
            productId,
            inactiveAsset.Id,
            productVariantId: null,
            isPrimary: true,
            sortOrder: -1,
            legacyUrl: "https://legacy.example.test/detail-inactive.png");
        AddProductImage(
            dbContext,
            tenantId,
            productId,
            mediaAssetId: null,
            productVariantId: null,
            isPrimary: true,
            sortOrder: -2,
            legacyUrl: "https://legacy.example.test/inactive-row.png",
            status: ProductConstants.InactiveStatus);

        await dbContext.SaveChangesAsync();
        var repository = new TenantAdminProductRepository(dbContext);

        var result = await repository.GetDetailAsync(tenantId, productId, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("https://cdn.example.test/detail-primary.png", result.ImageUrl);
        Assert.Collection(
            result.Images,
            image =>
            {
                Assert.Equal(primaryImage.Id, image.ProductImageId);
                Assert.Equal(primaryAsset.Id, image.MediaAssetId);
                Assert.Null(image.ProductVariantId);
                Assert.True(image.IsPrimaryImage);
            },
            image =>
            {
                Assert.Equal(secondaryImage.Id, image.ProductImageId);
                Assert.Equal("https://legacy.example.test/detail-secondary.png", image.ImageUrl);
            });

        var variant = Assert.Single(result.Variants);
        var groupedVariantImage = Assert.Single(variant.Images);
        Assert.Equal(variantImage.Id, groupedVariantImage.ProductImageId);
        Assert.Equal(variantId, groupedVariantImage.ProductVariantId);
        Assert.Equal("https://cdn.example.test/detail-variant.png", groupedVariantImage.ImageUrl);
    }

    private static void AddProduct(EPosDbContext dbContext, Guid tenantId, Guid productId, string code)
    {
        dbContext.Products.Add(Product.Create(
            productId,
            tenantId,
            code,
            $"{code} Product",
            $"{code.ToLowerInvariant()}-product",
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
    }

    private static MediaAsset AddMediaAsset(
        EPosDbContext dbContext,
        Guid tenantId,
        string publicUrl)
    {
        var id = Guid.NewGuid();
        var asset = MediaAsset.Create(
            id,
            tenantId,
            "images",
            $"tenants/{tenantId}/products/{id}.png",
            publicUrl,
            "image.png",
            "image/png",
            ".png",
            68,
            1,
            1,
            new string('a', 64),
            "IMAGE",
            "PRODUCT",
            "ACTIVE",
            createdByTenantUserId: null,
            Now);
        dbContext.MediaAssets.Add(asset);
        return asset;
    }

    private static ProductImage AddProductImage(
        EPosDbContext dbContext,
        Guid tenantId,
        Guid productId,
        Guid? mediaAssetId,
        Guid? productVariantId,
        bool isPrimary,
        int sortOrder,
        string? legacyUrl = null,
        string status = ProductConstants.ActiveStatus)
    {
        var id = Guid.NewGuid();
        var image = ProductImage.Create(
            id,
            tenantId,
            productId,
            productVariantId,
            salesChannelId: null,
            $"legacy/{id}.png",
            legacyUrl,
            "Product image",
            "GALLERY",
            "image/png",
            68,
            1,
            1,
            new string('b', 64),
            sortOrder,
            isPrimary,
            status,
            createdByTenantUserId: null,
            Now,
            mediaAssetId);
        dbContext.ProductImages.Add(image);
        return image;
    }

    private static EPosDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<EPosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new EPosDbContext(options);
    }
}
