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

public sealed class CatalogMediaServiceTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid ProductId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 7, 23, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task UploadProductImageAsync_WithValidPng_CreatesMediaAssetAndProductImage()
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
                AltText: "Main image",
                ImagePurpose: null,
                SortOrder: 2,
                IsPrimaryImage: true),
            new MediaUploadFile(stream, "product.png", "image/png", stream.Length),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Single(storage.Uploads);
        Assert.Single(repository.MediaAssets);
        Assert.Single(repository.ProductImages);

        var mediaAsset = repository.MediaAssets.Single();
        var productImage = repository.ProductImages.Single();

        Assert.Equal(result.Value!.MediaAssetId, mediaAsset.Id);
        Assert.Equal(result.Value.MediaAssetId, productImage.MediaAssetId);
        Assert.Equal(ProductId, productImage.ProductId);
        Assert.Equal("PRODUCT", productImage.ImagePurpose);
        Assert.Equal("image/png", productImage.MimeType);
        Assert.Equal(1, productImage.WidthPx);
        Assert.Equal(1, productImage.HeightPx);
        Assert.True(productImage.IsPrimaryImage);
        Assert.Equal(2, productImage.SortOrder);
        Assert.Equal(storage.Uploads.Single().StorageKey, productImage.ImageStorageKey);
        Assert.Equal(storage.Uploads.Single().PublicUrl, productImage.ImageUrl);
    }

    [Fact]
    public async Task UploadProductImageAsync_WithInvalidMimeType_DoesNotUpload()
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
            new MediaUploadFile(stream, "product.txt", "text/plain", stream.Length),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("media.validation_failed", result.Error.Code);
        Assert.Empty(storage.Uploads);
        Assert.Empty(repository.MediaAssets);
        Assert.Empty(repository.ProductImages);
    }

    [Fact]
    public async Task UploadProductImageAsync_WithoutUpdatePermission_ReturnsPermissionDenied()
    {
        var repository = new FakeCatalogMediaRepository { ProductExists = true };
        var storage = new FakeMediaObjectStorage();
        var service = new CatalogMediaService(repository, storage, new FakeDateTimeProvider());

        await using var stream = new MemoryStream(CreateOnePixelPng());
        var result = await service.UploadProductImageAsync(
            CreateContext([]),
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
        Assert.Equal("media.permission_denied", result.Error.Code);
        Assert.Empty(storage.Uploads);
    }

    private static TenantRequestContext CreateContext(IReadOnlyCollection<string> permissions) =>
        new(TenantId, UserId, permissions);

    private static byte[] CreateOnePixelPng() =>
        Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+/p9sAAAAASUVORK5CYII=");

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
            CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class FakeCatalogMediaRepository : ICatalogMediaRepository
    {
        public bool ProductExists { get; init; }
        public HashSet<Guid> VariantIds { get; } = [];
        public List<MediaAsset> MediaAssets { get; } = [];
        public List<ProductImage> ProductImages { get; } = [];

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
            Task.FromResult<Category?>(null);

        public Task<Brand?> GetBrandForLogoUpdateAsync(
            Guid tenantId,
            Guid brandId,
            CancellationToken cancellationToken) =>
            Task.FromResult<Brand?>(null);

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
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }
}
