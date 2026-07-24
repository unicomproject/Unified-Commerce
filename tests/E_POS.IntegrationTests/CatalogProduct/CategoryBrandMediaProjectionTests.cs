using E_POS.Domain.Modules.Shared.Media.Entities;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using E_POS.Infrastructure.Modules.Tenant.CatalogProduct.Repositories;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace E_POS.IntegrationTests.CatalogProduct;

public sealed class CategoryBrandMediaProjectionTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 24, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task CategoryListAndDetail_UseActiveMedia_FallbackLegacy_AndRejectInactiveOrCrossTenantMedia()
    {
        var tenantId = Guid.NewGuid();
        var otherTenantId = Guid.NewGuid();
        await using var dbContext = CreateDbContext();

        var activeMedia = AddMediaAsset(
            dbContext,
            tenantId,
            "CATEGORY",
            "https://cdn.example.test/category-active.png");
        var inactiveMedia = AddMediaAsset(
            dbContext,
            tenantId,
            "CATEGORY",
            "https://cdn.example.test/category-inactive.png");
        inactiveMedia.MarkInactive(updatedByTenantUserId: null, Now);
        var crossTenantMedia = AddMediaAsset(
            dbContext,
            otherTenantId,
            "CATEGORY",
            "https://cdn.example.test/category-cross-tenant.png");

        var activeCategory = AddCategory(
            dbContext,
            tenantId,
            "ACTIVE-MEDIA",
            "https://legacy.example.test/category-active.png");
        activeCategory.UpdateImage(
            "https://legacy.example.test/category-active.png",
            activeMedia.Id,
            updatedByTenantUserId: null,
            Now);

        var legacyCategory = AddCategory(
            dbContext,
            tenantId,
            "LEGACY",
            "https://legacy.example.test/category-legacy.png");

        var inactiveCategory = AddCategory(
            dbContext,
            tenantId,
            "INACTIVE-MEDIA",
            "https://legacy.example.test/category-must-not-leak.png");
        inactiveCategory.UpdateImage(
            "https://legacy.example.test/category-must-not-leak.png",
            inactiveMedia.Id,
            updatedByTenantUserId: null,
            Now);

        var crossTenantCategory = AddCategory(
            dbContext,
            tenantId,
            "CROSS-TENANT",
            "https://legacy.example.test/category-cross-tenant-must-not-leak.png");
        crossTenantCategory.UpdateImage(
            "https://legacy.example.test/category-cross-tenant-must-not-leak.png",
            crossTenantMedia.Id,
            updatedByTenantUserId: null,
            Now);

        AddCategory(
            dbContext,
            otherTenantId,
            "OTHER-TENANT",
            "https://legacy.example.test/other-tenant-category.png");

        await dbContext.SaveChangesAsync();
        var repository = new CategoryRepository(dbContext);

        var list = await repository.ListAsync(tenantId, 1, 50, null, CancellationToken.None);

        Assert.Equal(4, list.TotalCount);
        var activeItem = Assert.Single(list.Items, item => item.Id == activeCategory.Id);
        Assert.Equal("https://cdn.example.test/category-active.png", activeItem.ImageUrl);
        Assert.Equal(activeMedia.Id, activeItem.ImageMediaAssetId);

        var legacyItem = Assert.Single(list.Items, item => item.Id == legacyCategory.Id);
        Assert.Equal("https://legacy.example.test/category-legacy.png", legacyItem.ImageUrl);
        Assert.Null(legacyItem.ImageMediaAssetId);

        var inactiveItem = Assert.Single(list.Items, item => item.Id == inactiveCategory.Id);
        Assert.Null(inactiveItem.ImageUrl);
        Assert.Null(inactiveItem.ImageMediaAssetId);

        var crossTenantItem = Assert.Single(list.Items, item => item.Id == crossTenantCategory.Id);
        Assert.Null(crossTenantItem.ImageUrl);
        Assert.Null(crossTenantItem.ImageMediaAssetId);
        Assert.DoesNotContain(list.Items, item => item.CategoryCode == "OTHER-TENANT");

        var detail = await repository.GetByIdAsync(
            tenantId,
            activeCategory.Id,
            includeDeleted: false,
            CancellationToken.None);

        Assert.NotNull(detail);
        Assert.Equal("https://cdn.example.test/category-active.png", detail.ImageUrl);
        Assert.Equal(activeMedia.Id, detail.ImageMediaAssetId);
    }

    [Fact]
    public async Task BrandListAndDetail_UseActiveMedia_FallbackLegacy_AndRejectInactiveOrCrossTenantMedia()
    {
        var tenantId = Guid.NewGuid();
        var otherTenantId = Guid.NewGuid();
        await using var dbContext = CreateDbContext();

        var activeMedia = AddMediaAsset(
            dbContext,
            tenantId,
            "BRAND",
            "https://cdn.example.test/brand-active.png");
        var inactiveMedia = AddMediaAsset(
            dbContext,
            tenantId,
            "BRAND",
            "https://cdn.example.test/brand-inactive.png");
        inactiveMedia.MarkInactive(updatedByTenantUserId: null, Now);
        var crossTenantMedia = AddMediaAsset(
            dbContext,
            otherTenantId,
            "BRAND",
            "https://cdn.example.test/brand-cross-tenant.png");

        var activeBrand = AddBrand(
            dbContext,
            tenantId,
            "ACTIVE-MEDIA",
            "https://legacy.example.test/brand-active.png");
        activeBrand.UpdateLogo(
            "https://legacy.example.test/brand-active.png",
            activeMedia.Id,
            updatedByTenantUserId: null,
            Now);

        var legacyBrand = AddBrand(
            dbContext,
            tenantId,
            "LEGACY",
            "https://legacy.example.test/brand-legacy.png");

        var inactiveBrand = AddBrand(
            dbContext,
            tenantId,
            "INACTIVE-MEDIA",
            "https://legacy.example.test/brand-must-not-leak.png");
        inactiveBrand.UpdateLogo(
            "https://legacy.example.test/brand-must-not-leak.png",
            inactiveMedia.Id,
            updatedByTenantUserId: null,
            Now);

        var crossTenantBrand = AddBrand(
            dbContext,
            tenantId,
            "CROSS-TENANT",
            "https://legacy.example.test/brand-cross-tenant-must-not-leak.png");
        crossTenantBrand.UpdateLogo(
            "https://legacy.example.test/brand-cross-tenant-must-not-leak.png",
            crossTenantMedia.Id,
            updatedByTenantUserId: null,
            Now);

        AddBrand(
            dbContext,
            otherTenantId,
            "OTHER-TENANT",
            "https://legacy.example.test/other-tenant-brand.png");

        await dbContext.SaveChangesAsync();
        var repository = new BrandRepository(dbContext);

        var list = await repository.ListAsync(tenantId, 1, 50, null, CancellationToken.None);

        Assert.Equal(4, list.TotalCount);
        var activeItem = Assert.Single(list.Items, item => item.Id == activeBrand.Id);
        Assert.Equal("https://cdn.example.test/brand-active.png", activeItem.LogoUrl);
        Assert.Equal(activeMedia.Id, activeItem.LogoMediaAssetId);

        var legacyItem = Assert.Single(list.Items, item => item.Id == legacyBrand.Id);
        Assert.Equal("https://legacy.example.test/brand-legacy.png", legacyItem.LogoUrl);
        Assert.Null(legacyItem.LogoMediaAssetId);

        var inactiveItem = Assert.Single(list.Items, item => item.Id == inactiveBrand.Id);
        Assert.Null(inactiveItem.LogoUrl);
        Assert.Null(inactiveItem.LogoMediaAssetId);

        var crossTenantItem = Assert.Single(list.Items, item => item.Id == crossTenantBrand.Id);
        Assert.Null(crossTenantItem.LogoUrl);
        Assert.Null(crossTenantItem.LogoMediaAssetId);
        Assert.DoesNotContain(list.Items, item => item.BrandCode == "OTHER-TENANT");

        var detail = await repository.GetByIdAsync(
            tenantId,
            activeBrand.Id,
            includeDeleted: false,
            CancellationToken.None);

        Assert.NotNull(detail);
        Assert.Equal("https://cdn.example.test/brand-active.png", detail.LogoUrl);
        Assert.Equal(activeMedia.Id, detail.LogoMediaAssetId);
    }

    private static Category AddCategory(
        EPosDbContext dbContext,
        Guid tenantId,
        string code,
        string? imageUrl)
    {
        var category = Category.Create(
            Guid.NewGuid(),
            tenantId,
            Guid.NewGuid(),
            parentCategoryId: null,
            code,
            $"{code} Category",
            code.ToLowerInvariant(),
            description: null,
            imageUrl,
            sortOrder: 0,
            CategoryConstants.ActiveStatus,
            createdByTenantUserId: null,
            Now);
        dbContext.Categories.Add(category);
        return category;
    }

    private static Brand AddBrand(
        EPosDbContext dbContext,
        Guid tenantId,
        string code,
        string? logoUrl)
    {
        var brand = Brand.Create(
            Guid.NewGuid(),
            tenantId,
            code,
            $"{code} Brand",
            code.ToLowerInvariant(),
            description: null,
            logoUrl,
            BrandConstants.ActiveStatus,
            createdByTenantUserId: null,
            Now);
        dbContext.Brands.Add(brand);
        return brand;
    }

    private static MediaAsset AddMediaAsset(
        EPosDbContext dbContext,
        Guid tenantId,
        string purpose,
        string publicUrl)
    {
        var mediaAssetId = Guid.NewGuid();
        var mediaAsset = MediaAsset.Create(
            mediaAssetId,
            tenantId,
            "images",
            $"tenants/{tenantId}/{purpose.ToLowerInvariant()}/{mediaAssetId}.png",
            publicUrl,
            "image.png",
            "image/png",
            ".png",
            68,
            1,
            1,
            new string('a', 64),
            "IMAGE",
            purpose,
            "ACTIVE",
            createdByTenantUserId: null,
            Now);
        dbContext.MediaAssets.Add(mediaAsset);
        return mediaAsset;
    }

    private static EPosDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<EPosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new EPosDbContext(options);
    }
}
