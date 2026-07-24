using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Shared.Media.Contracts;
using E_POS.Application.Modules.Shared.Media.Dtos;
using E_POS.Application.Modules.Tenant.CatalogProduct.Contracts;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos;
using E_POS.Application.Modules.Tenant.CatalogProduct.Services;
using E_POS.Domain.Modules.Shared.Media.Entities;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using Xunit;

namespace E_POS.UnitTests.CatalogProduct;

public sealed class CatalogMediaServicePhase4ETests
{
    private static readonly Guid TenantId = Guid.Parse("aaaaaaaa-0000-4000-8000-000000000001");
    private static readonly Guid UserId = Guid.Parse("bbbbbbbb-0000-4000-8000-000000000001");
    private static readonly Guid ProductId = Guid.Parse("cccccccc-0000-4000-8000-000000000001");
    private static readonly Guid CategoryId = Guid.Parse("dddddddd-0000-4000-8000-000000000001");
    private static readonly Guid BrandId = Guid.Parse("eeeeeeee-0000-4000-8000-000000000001");
    private static readonly DateTimeOffset Now = new(2026, 7, 24, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task UploadProductImageAsync_WithExtensionMismatch_DoesNotUpload()
    {
        var repository = new FakeCatalogMediaRepository { ProductExists = true };
        var storage = new FakeMediaObjectStorage();
        var service = new CatalogMediaService(repository, storage, new FakeDateTimeProvider());

        await using var stream = new MemoryStream(CreateOnePixelPng());
        var result = await service.UploadProductImageAsync(
            CreateContext([ProductConstants.UpdatePermission]),
            ProductId,
            new ProductImageUploadRequest(
                ProductVariantId: null,
                SalesChannelId: null,
                AltText: null,
                ImagePurpose: null,
                SortOrder: null,
                IsPrimaryImage: null),
            new MediaUploadFile(stream, "product.jpg", "image/png", stream.Length),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("media.validation_failed", result.Error.Code);
        Assert.Empty(storage.Uploads);
        Assert.Empty(repository.MediaAssets);
        Assert.Empty(repository.ProductImages);
    }

    [Fact]
    public async Task UploadProductImageAsync_WithEmptyFile_DoesNotUpload()
    {
        var repository = new FakeCatalogMediaRepository { ProductExists = true };
        var storage = new FakeMediaObjectStorage();
        var service = new CatalogMediaService(repository, storage, new FakeDateTimeProvider());

        await using var stream = new MemoryStream();
        var result = await service.UploadProductImageAsync(
            CreateContext([ProductConstants.UpdatePermission]),
            ProductId,
            new ProductImageUploadRequest(
                ProductVariantId: null,
                SalesChannelId: null,
                AltText: null,
                ImagePurpose: null,
                SortOrder: null,
                IsPrimaryImage: null),
            new MediaUploadFile(stream, "product.png", "image/png", stream.Length),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("media.validation_failed", result.Error.Code);
        Assert.Empty(storage.Uploads);
        Assert.Empty(repository.MediaAssets);
        Assert.Empty(repository.ProductImages);
    }

    [Fact]
    public async Task UploadProductImageAsync_WhenStorageIsNotConfigured_DoesNotUpload()
    {
        var repository = new FakeCatalogMediaRepository { ProductExists = true };
        var storage = new FakeMediaObjectStorage { IsConfigured = false };
        var service = new CatalogMediaService(repository, storage, new FakeDateTimeProvider());

        await using var stream = new MemoryStream(CreateOnePixelPng());
        var result = await service.UploadProductImageAsync(
            CreateContext([ProductConstants.UpdatePermission]),
            ProductId,
            new ProductImageUploadRequest(
                ProductVariantId: null,
                SalesChannelId: null,
                AltText: null,
                ImagePurpose: null,
                SortOrder: null,
                IsPrimaryImage: null),
            new MediaUploadFile(stream, "product.png", "image/png", stream.Length),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("media.storage_not_configured", result.Error.Code);
        Assert.Empty(storage.Uploads);
        Assert.Empty(repository.MediaAssets);
        Assert.Empty(repository.ProductImages);
    }

    [Fact]
    public async Task UploadCategoryImageAsync_WithExistingMediaAsset_MarksPreviousMediaInactive()
    {
        var previousMediaAssetId = Guid.Parse("11111111-0000-4000-8000-000000000001");
        var category = CreateCategory(previousMediaAssetId);
        var repository = new FakeCatalogMediaRepository { CategoryForImageUpdate = category };
        var storage = new FakeMediaObjectStorage();
        var service = new CatalogMediaService(repository, storage, new FakeDateTimeProvider());

        await using var stream = new MemoryStream(CreateOnePixelPng());
        var result = await service.UploadCategoryImageAsync(
            CreateContext([CategoryConstants.UpdatePermission]),
            CategoryId,
            new MediaUploadFile(stream, "category.png", "image/png", stream.Length),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(CategoryId, result.Value!.CategoryId);
        Assert.Equal(result.Value.MediaAssetId, category.ImageMediaAssetId);
        Assert.Equal(result.Value.PublicUrl, category.ImageUrl);
        Assert.Equal([previousMediaAssetId], repository.InactivatedMediaAssetIds);
        Assert.Single(repository.MediaAssets);
        Assert.Single(storage.Uploads);
    }

    [Fact]
    public async Task UploadBrandLogoAsync_WithExistingMediaAsset_MarksPreviousMediaInactive()
    {
        var previousMediaAssetId = Guid.Parse("22222222-0000-4000-8000-000000000001");
        var brand = CreateBrand(previousMediaAssetId);
        var repository = new FakeCatalogMediaRepository { BrandForLogoUpdate = brand };
        var storage = new FakeMediaObjectStorage();
        var service = new CatalogMediaService(repository, storage, new FakeDateTimeProvider());

        await using var stream = new MemoryStream(CreateOnePixelPng());
        var result = await service.UploadBrandLogoAsync(
            CreateContext([BrandConstants.UpdatePermission]),
            BrandId,
            new MediaUploadFile(stream, "brand.png", "image/png", stream.Length),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(BrandId, result.Value!.BrandId);
        Assert.Equal(result.Value.MediaAssetId, brand.LogoMediaAssetId);
        Assert.Equal(result.Value.PublicUrl, brand.LogoUrl);
        Assert.Equal([previousMediaAssetId], repository.InactivatedMediaAssetIds);
        Assert.Single(repository.MediaAssets);
        Assert.Single(storage.Uploads);
    }

    private static TenantRequestContext CreateContext(IReadOnlyCollection<string> permissions) =>
        new(TenantId, UserId, permissions);

    private static byte[] CreateOnePixelPng() =>
        Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+/p9sAAAAASUVORK5CYII=");

    private static Category CreateCategory(Guid? imageMediaAssetId)
    {
        var category = Category.Create(
            CategoryId,
            TenantId,
            Guid.Parse("33333333-0000-4000-8000-000000000001"),
            parentCategoryId: null,
            "APPAREL",
            "Apparel",
            "apparel",
            "Apparel",
            imageUrl: null,
            sortOrder: 1,
            "ACTIVE",
            UserId,
            Now);

        if (imageMediaAssetId.HasValue)
        {
            category.UpdateImage(
                "https://legacy.example.test/category.png",
                imageMediaAssetId.Value,
                UserId,
                Now);
        }

        return category;
    }

    private static Brand CreateBrand(Guid? logoMediaAssetId)
    {
        var brand = Brand.Create(
            BrandId,
            TenantId,
            "NIKE",
            "Nike",
            "nike",
            "Brand",
            logoUrl: null,
            "ACTIVE",
            UserId,
            Now);

        if (logoMediaAssetId.HasValue)
        {
            brand.UpdateLogo(
                "https://legacy.example.test/brand.png",
                logoMediaAssetId.Value,
                UserId,
                Now);
        }

        return brand;
    }

    private sealed class FakeDateTimeProvider : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => Now;
    }

    private sealed class FakeMediaObjectStorage : IMediaObjectStorage
    {
        public bool IsConfigured { get; init; } = true;
        public List<MediaObjectUploadResult> Uploads { get; } = [];

        public Task<MediaObjectUploadResult> UploadAsync(
            MediaObjectUploadRequest request,
            CancellationToken cancellationToken)
        {
            var result = new MediaObjectUploadResult(
                "tenant-media",
                request.StorageKey,
                $"https://cdn.example.test/{request.StorageKey}");
            Uploads.Add(result);
            return Task.FromResult(result);
        }

        public Task DeleteIfExistsAsync(
            string containerName,
            string storageKey,
            CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }

    private sealed class FakeCatalogMediaRepository : ICatalogMediaRepository
    {
        public bool ProductExists { get; init; }
        public Category? CategoryForImageUpdate { get; init; }
        public Brand? BrandForLogoUpdate { get; init; }
        public HashSet<Guid> VariantIds { get; } = [];
        public List<MediaAsset> MediaAssets { get; } = [];
        public List<ProductImage> ProductImages { get; } = [];
        public List<Guid> InactivatedMediaAssetIds { get; } = [];

        public Task<bool> ProductExistsAsync(
            Guid tenantId,
            Guid productId,
            CancellationToken cancellationToken) =>
            Task.FromResult(ProductExists && tenantId == TenantId && productId == ProductId);

        public Task<bool> ProductVariantExistsAsync(
            Guid tenantId,
            Guid productId,
            Guid productVariantId,
            CancellationToken cancellationToken) =>
            Task.FromResult(
                ProductExists &&
                tenantId == TenantId &&
                productId == ProductId &&
                VariantIds.Contains(productVariantId));

        public Task<Category?> GetCategoryForImageUpdateAsync(
            Guid tenantId,
            Guid categoryId,
            CancellationToken cancellationToken) =>
            Task.FromResult(tenantId == TenantId && categoryId == CategoryId ? CategoryForImageUpdate : null);

        public Task<Brand?> GetBrandForLogoUpdateAsync(
            Guid tenantId,
            Guid brandId,
            CancellationToken cancellationToken) =>
            Task.FromResult(tenantId == TenantId && brandId == BrandId ? BrandForLogoUpdate : null);

        public Task AddMediaAssetAsync(
            MediaAsset mediaAsset,
            CancellationToken cancellationToken)
        {
            MediaAssets.Add(mediaAsset);
            return Task.CompletedTask;
        }

        public Task AddProductImageAsync(
            ProductImage productImage,
            CancellationToken cancellationToken)
        {
            ProductImages.Add(productImage);
            return Task.CompletedTask;
        }

        public Task MarkMediaAssetInactiveAsync(
            Guid tenantId,
            Guid mediaAssetId,
            Guid? updatedByTenantUserId,
            DateTimeOffset now,
            CancellationToken cancellationToken)
        {
            InactivatedMediaAssetIds.Add(mediaAssetId);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }
}
