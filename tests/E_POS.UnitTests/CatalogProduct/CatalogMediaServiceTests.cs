using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Shared.Media.Contracts;
using E_POS.Application.Modules.Shared.Media.Dtos;
using E_POS.Application.Modules.Tenant.CatalogProduct.Contracts;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.TenantAdmin;
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
            CreateContext([ProductConstants.MediaManagePermission]),
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
        Assert.Equal("image/png", mediaAsset.MimeType);
        Assert.Equal(1, mediaAsset.WidthPx);
        Assert.Equal(1, mediaAsset.HeightPx);
        Assert.True(productImage.IsPrimaryImage);
        Assert.Equal(2, productImage.SortOrder);
        Assert.Equal(storage.Uploads.Single().StorageKey, mediaAsset.StorageKey);
        Assert.Equal(storage.Uploads.Single().PublicUrl, mediaAsset.PublicUrl);
    }

    [Fact]
    public async Task UploadProductImageAsync_WithInvalidMimeType_DoesNotUpload()
    {
        var repository = new FakeCatalogMediaRepository { ProductExists = true };
        var storage = new FakeMediaObjectStorage();
        var service = new CatalogMediaService(repository, storage, new FakeDateTimeProvider());

        await using var stream = new MemoryStream(CreateOnePixelPng());
        var result = await service.UploadProductImageAsync(
            CreateContext([ProductConstants.MediaManagePermission]),
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
        Assert.Equal("media.unsupported_media_type", result.Error.Code);
        Assert.Empty(storage.Uploads);
        Assert.Empty(repository.MediaAssets);
        Assert.Empty(repository.ProductImages);
    }

    [Fact]
    public async Task UploadProductImageAsync_WithoutMediaManagePermission_ReturnsPermissionDenied()
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

    [Fact]
    public async Task UploadProductImageAsync_WithOnlyProductsUpdatePermission_ReturnsPermissionDenied()
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
            new MediaUploadFile(stream, "product.png", "image/png", stream.Length),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("media.permission_denied", result.Error.Code);
        Assert.Empty(storage.Uploads);
    }

    [Fact]
    public async Task StageProductImageAsync_WithOnlyProductsCreatePermission_ReturnsPermissionDenied()
    {
        var repository = new FakeCatalogMediaRepository();
        var storage = new FakeMediaObjectStorage();
        var service = new CatalogMediaService(repository, storage, new FakeDateTimeProvider());

        await using var stream = new MemoryStream(CreateOnePixelPng());
        var result = await service.StageProductImageAsync(
            CreateContext([ProductConstants.CreatePermission]),
            new MediaUploadFile(stream, "staged.png", "image/png", stream.Length),
            uploadSessionId: null,
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("media.permission_denied", result.Error.Code);
    }

    [Fact]
    public async Task StageProductImageAsync_ValidPng_CreatesStagedAsset()
    {
        var repository = new FakeCatalogMediaRepository();
        var storage = new FakeMediaObjectStorage();
        var service = new CatalogMediaService(repository, storage, new FakeDateTimeProvider());

        await using var stream = new MemoryStream(CreateOnePixelPng());
        var result = await service.StageProductImageAsync(
            CreateContext([ProductConstants.MediaManagePermission]),
            new MediaUploadFile(stream, "staged.png", "image/png", stream.Length),
            uploadSessionId: null,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(repository.MediaAssets);
        var asset = repository.MediaAssets.Single();
        Assert.Equal("STAGED", asset.Status);
        Assert.Equal(ProductConstants.ProductImagePurpose, asset.AssetPurpose);
        Assert.Equal(result.Value!.MediaAssetId, asset.Id);
        Assert.Equal("image/png", result.Value.MimeType);
        Assert.Single(storage.Uploads);
    }

    [Fact]
    public async Task StageProductImageAsync_ValidJpeg_CreatesStagedAsset()
    {
        var repository = new FakeCatalogMediaRepository();
        var storage = new FakeMediaObjectStorage();
        var service = new CatalogMediaService(repository, storage, new FakeDateTimeProvider());

        var jpegBytes = CreateMinimalJpeg();
        await using var stream = new MemoryStream(jpegBytes);
        var result = await service.StageProductImageAsync(
            CreateContext([ProductConstants.MediaManagePermission]),
            new MediaUploadFile(stream, "staged.jpg", "image/jpeg", stream.Length),
            uploadSessionId: null,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(repository.MediaAssets);
        Assert.Equal("STAGED", repository.MediaAssets.Single().Status);
        Assert.Equal("image/jpeg", result.Value!.MimeType);
        Assert.Equal(1, repository.MediaAssets.Single().WidthPx);
        Assert.Equal(1, repository.MediaAssets.Single().HeightPx);
    }

    [Fact]
    public async Task StageProductImageAsync_UnsupportedMime_Rejected()
    {
        var repository = new FakeCatalogMediaRepository();
        var storage = new FakeMediaObjectStorage();
        var service = new CatalogMediaService(repository, storage, new FakeDateTimeProvider());

        await using var stream = new MemoryStream(CreateOnePixelPng());
        var result = await service.StageProductImageAsync(
            CreateContext([ProductConstants.MediaManagePermission]),
            new MediaUploadFile(stream, "staged.gif", "image/gif", stream.Length),
            uploadSessionId: null,
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("media.unsupported_media_type", result.Error.Code);
        Assert.Empty(storage.Uploads);
        Assert.Empty(repository.MediaAssets);
    }

    [Fact]
    public async Task StageProductImageAsync_FileTooLarge_Rejected()
    {
        var repository = new FakeCatalogMediaRepository();
        var storage = new FakeMediaObjectStorage();
        var service = new CatalogMediaService(repository, storage, new FakeDateTimeProvider());

        await using var stream = new MemoryStream(CreateOnePixelPng());
        var result = await service.StageProductImageAsync(
            CreateContext([ProductConstants.MediaManagePermission]),
            new MediaUploadFile(stream, "huge.png", "image/png", Length: 6 * 1024 * 1024),
            uploadSessionId: null,
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("media.file_size_exceeded", result.Error.Code);
        Assert.Empty(storage.Uploads);
        Assert.Empty(repository.MediaAssets);
    }

    [Fact]
    public async Task UploadProductImageAsync_FirstImage_BecomesPrimary()
    {
        var repository = new FakeCatalogMediaRepository { ProductExists = true };
        var storage = new FakeMediaObjectStorage();
        var service = new CatalogMediaService(repository, storage, new FakeDateTimeProvider());

        await using var stream = new MemoryStream(CreateOnePixelPng());
        var result = await service.UploadProductImageAsync(
            CreateContext([ProductConstants.MediaManagePermission]),
            ProductId,
            new ProductImageUploadRequest(
                ProductVariantId: null,
                SalesChannelId: null,
                AltText: null,
                ImagePurpose: null,
                SortOrder: null,
                IsPrimaryImage: null),
            new MediaUploadFile(stream, "first.png", "image/png", stream.Length),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(repository.ProductImages.Single().IsPrimaryImage);
    }

    [Fact]
    public async Task ReorderProductImagesAsync_PersistsSortOrder_WithoutChangingPrimaryUnlessRequested()
    {
        var product = CreateProduct(rowVersion: 1);
        var primaryId = Guid.NewGuid();
        var secondaryId = Guid.NewGuid();
        var repository = new FakeCatalogMediaRepository
        {
            ProductExists = true,
            ProductForUpdate = product,
        };
        repository.ProductImages.Add(CreateProductImage(primaryId, sortOrder: 0, isPrimary: true));
        repository.ProductImages.Add(CreateProductImage(secondaryId, sortOrder: 1, isPrimary: false));
        var service = new CatalogMediaService(repository, new FakeMediaObjectStorage(), new FakeDateTimeProvider());

        var result = await service.ReorderProductImagesAsync(
            CreateContext([ProductConstants.MediaManagePermission]),
            ProductId,
            new ReorderProductImagesRequest(
                ExpectedRowVersion: 1,
                PrimaryProductImageId: null,
                Items:
                [
                    new ReorderProductImageItem(primaryId, 5),
                    new ReorderProductImageItem(secondaryId, 0),
                ]),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(5, repository.ProductImages.Single(x => x.Id == primaryId).SortOrder);
        Assert.Equal(0, repository.ProductImages.Single(x => x.Id == secondaryId).SortOrder);
        Assert.True(repository.ProductImages.Single(x => x.Id == primaryId).IsPrimaryImage);
        Assert.False(repository.ProductImages.Single(x => x.Id == secondaryId).IsPrimaryImage);
        Assert.Equal(2, product.RowVersion);
    }

    [Fact]
    public async Task ReorderProductImagesAsync_SetPrimary_Works()
    {
        var product = CreateProduct(rowVersion: 1);
        var primaryId = Guid.NewGuid();
        var secondaryId = Guid.NewGuid();
        var repository = new FakeCatalogMediaRepository
        {
            ProductExists = true,
            ProductForUpdate = product,
        };
        repository.ProductImages.Add(CreateProductImage(primaryId, sortOrder: 0, isPrimary: true));
        repository.ProductImages.Add(CreateProductImage(secondaryId, sortOrder: 1, isPrimary: false));
        var service = new CatalogMediaService(repository, new FakeMediaObjectStorage(), new FakeDateTimeProvider());

        var result = await service.ReorderProductImagesAsync(
            CreateContext([ProductConstants.MediaManagePermission]),
            ProductId,
            new ReorderProductImagesRequest(
                ExpectedRowVersion: 1,
                PrimaryProductImageId: secondaryId,
                Items:
                [
                    new ReorderProductImageItem(primaryId, 0),
                    new ReorderProductImageItem(secondaryId, 1),
                ]),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(repository.ProductImages.Single(x => x.Id == primaryId).IsPrimaryImage);
        Assert.True(repository.ProductImages.Single(x => x.Id == secondaryId).IsPrimaryImage);
    }

    [Fact]
    public async Task DeleteProductImageAsync_NonPrimary_Works()
    {
        var product = CreateProduct(rowVersion: 1);
        var primaryId = Guid.NewGuid();
        var secondaryId = Guid.NewGuid();
        var repository = new FakeCatalogMediaRepository
        {
            ProductExists = true,
            ProductForUpdate = product,
        };
        repository.ProductImages.Add(CreateProductImage(primaryId, sortOrder: 0, isPrimary: true));
        repository.ProductImages.Add(CreateProductImage(secondaryId, sortOrder: 1, isPrimary: false));
        var service = new CatalogMediaService(repository, new FakeMediaObjectStorage(), new FakeDateTimeProvider());

        var result = await service.DeleteProductImageAsync(
            CreateContext([ProductConstants.MediaManagePermission]),
            ProductId,
            secondaryId,
            expectedRowVersion: 1,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("DELETED", repository.ProductImages.Single(x => x.Id == secondaryId).Status);
        Assert.True(repository.ProductImages.Single(x => x.Id == primaryId).IsPrimaryImage);
        Assert.Equal("ACTIVE", repository.ProductImages.Single(x => x.Id == primaryId).Status);
    }

    [Fact]
    public async Task DeleteProductImageAsync_Primary_PromotesLowestSortOrder()
    {
        var product = CreateProduct(rowVersion: 1);
        var primaryId = Guid.NewGuid();
        var lowSortId = Guid.NewGuid();
        var highSortId = Guid.NewGuid();
        var repository = new FakeCatalogMediaRepository
        {
            ProductExists = true,
            ProductForUpdate = product,
        };
        repository.ProductImages.Add(CreateProductImage(primaryId, sortOrder: 0, isPrimary: true));
        repository.ProductImages.Add(CreateProductImage(highSortId, sortOrder: 5, isPrimary: false));
        repository.ProductImages.Add(CreateProductImage(lowSortId, sortOrder: 2, isPrimary: false));
        var service = new CatalogMediaService(repository, new FakeMediaObjectStorage(), new FakeDateTimeProvider());

        var result = await service.DeleteProductImageAsync(
            CreateContext([ProductConstants.MediaManagePermission]),
            ProductId,
            primaryId,
            expectedRowVersion: 1,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("DELETED", repository.ProductImages.Single(x => x.Id == primaryId).Status);
        Assert.True(repository.ProductImages.Single(x => x.Id == lowSortId).IsPrimaryImage);
        Assert.False(repository.ProductImages.Single(x => x.Id == highSortId).IsPrimaryImage);
    }

    [Fact]
    public async Task ReplaceProductImagesAsync_Success()
    {
        var product = CreateProduct(rowVersion: 1);
        var stagedId = Guid.NewGuid();
        var oldImageId = Guid.NewGuid();
        var repository = new FakeCatalogMediaRepository
        {
            ProductExists = true,
            ProductForUpdate = product,
        };
        repository.ProductImages.Add(CreateProductImage(oldImageId, sortOrder: 0, isPrimary: true));
        repository.MediaAssets.Add(MediaAsset.Create(
            stagedId,
            TenantId,
            "tenant-media",
            $"tenants/{TenantId:D}/products/staged/{stagedId:D}.png",
            "https://cdn.example.test/staged.png",
            "staged.png",
            "image/png",
            ".png",
            68,
            1,
            1,
            "abc",
            "IMAGE",
            ProductConstants.ProductImagePurpose,
            "STAGED",
            UserId,
            Now));
        var service = new CatalogMediaService(repository, new FakeMediaObjectStorage(), new FakeDateTimeProvider());

        var result = await service.ReplaceProductImagesAsync(
            CreateContext([ProductConstants.MediaManagePermission]),
            ProductId,
            expectedRowVersion: 1,
            files: null,
            stagedMediaAssetIds: [stagedId],
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("DELETED", repository.ProductImages.Single(x => x.Id == oldImageId).Status);
        Assert.Contains(repository.ProductImages, x => x.MediaAssetId == stagedId && x.Status == "ACTIVE" && x.IsPrimaryImage);
        Assert.Equal("ACTIVE", repository.MediaAssets.Single(x => x.Id == stagedId).Status);
        Assert.Equal(2, product.RowVersion);
    }

    [Fact]
    public async Task Upload_MaxImages_11thRejected()
    {
        var repository = new FakeCatalogMediaRepository
        {
            ProductExists = true,
            ActiveImageCountOverride = ProductConstants.MaxProductImages,
        };
        var storage = new FakeMediaObjectStorage();
        var service = new CatalogMediaService(repository, storage, new FakeDateTimeProvider());

        await using var stream = new MemoryStream(CreateOnePixelPng());
        var result = await service.UploadProductImageAsync(
            CreateContext([ProductConstants.MediaManagePermission]),
            ProductId,
            new ProductImageUploadRequest(
                ProductVariantId: null,
                SalesChannelId: null,
                AltText: null,
                ImagePurpose: null,
                SortOrder: null,
                IsPrimaryImage: null),
            new MediaUploadFile(stream, "eleventh.png", "image/png", stream.Length),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("media.max_images_exceeded", result.Error.Code);
        Assert.Empty(storage.Uploads);
    }

    [Fact]
    public async Task StageProductImageAsync_WithoutPermission_Returns403()
    {
        var repository = new FakeCatalogMediaRepository();
        var storage = new FakeMediaObjectStorage();
        var service = new CatalogMediaService(repository, storage, new FakeDateTimeProvider());

        await using var stream = new MemoryStream(CreateOnePixelPng());
        var result = await service.StageProductImageAsync(
            CreateContext([]),
            new MediaUploadFile(stream, "staged.png", "image/png", stream.Length),
            uploadSessionId: null,
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("media.permission_denied", result.Error.Code);
        Assert.Empty(storage.Uploads);
    }

    [Fact]
    public async Task ReorderProductImagesAsync_StaleRowVersion_ReturnsConcurrencyConflict()
    {
        var product = CreateProduct(rowVersion: 5);
        var imageId = Guid.NewGuid();
        var repository = new FakeCatalogMediaRepository
        {
            ProductExists = true,
            ProductForUpdate = product,
        };
        repository.ProductImages.Add(CreateProductImage(imageId, sortOrder: 0, isPrimary: true));
        var service = new CatalogMediaService(repository, new FakeMediaObjectStorage(), new FakeDateTimeProvider());

        var result = await service.ReorderProductImagesAsync(
            CreateContext([ProductConstants.MediaManagePermission]),
            ProductId,
            new ReorderProductImagesRequest(
                ExpectedRowVersion: 1,
                PrimaryProductImageId: null,
                Items: [new ReorderProductImageItem(imageId, 1)]),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("media.concurrency_conflict", result.Error.Code);
        Assert.Equal(5, product.RowVersion);
    }

    private static TenantRequestContext CreateContext(IReadOnlyCollection<string> permissions) =>
        new(TenantId, UserId, permissions);

    private static byte[] CreateOnePixelPng() =>
        Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+/p9sAAAAASUVORK5CYII=");

    private static byte[] CreateMinimalJpeg() =>
    [
        0xFF, 0xD8, // SOI
        0xFF, 0xC0, // SOF0
        0x00, 0x0B, // segment length
        0x08, // precision
        0x00, 0x01, // height
        0x00, 0x01, // width
        0x01, // components
        0x01, 0x11, 0x00, // component data
        0xFF, 0xD9 // EOI
    ];

    private static Product CreateProduct(long rowVersion)
    {
        var product = Product.Create(
            ProductId,
            TenantId,
            "PROD-001",
            "Media Product",
            "media-product",
            "STANDARD",
            "SIMPLE",
            businessTypeId: null,
            brandId: null,
            returnPolicyId: null,
            shortDescription: null,
            longDescription: null,
            isSellable: true,
            isTaxable: true,
            ProductConstants.DraftStatus,
            UserId,
            Now);

        while (product.RowVersion < rowVersion)
        {
            product.IncrementRowVersion();
        }

        return product;
    }

    private static ProductImage CreateProductImage(Guid id, int sortOrder, bool isPrimary) =>
        ProductImage.Create(
            id,
            TenantId,
            ProductId,
            productVariantId: null,
            salesChannelId: null,
            mediaAssetId: Guid.NewGuid(),
            altText: null,
            imagePurpose: ProductConstants.ProductImagePurpose,
            sortOrder,
            isPrimary,
            "ACTIVE",
            UserId,
            Now);

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
        public Product? ProductForUpdate { get; init; }
        public int? ActiveImageCountOverride { get; init; }
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
            CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task<Product?> GetProductForUpdateAsync(
            Guid tenantId,
            Guid productId,
            CancellationToken cancellationToken) =>
            Task.FromResult(
                ProductForUpdate is not null &&
                tenantId == TenantId &&
                productId == ProductId
                    ? ProductForUpdate
                    : null);

        public Task<int> CountActiveProductImagesAsync(
            Guid tenantId,
            Guid productId,
            CancellationToken cancellationToken) =>
            Task.FromResult(
                ActiveImageCountOverride ??
                ProductImages.Count(x => x.Status == "ACTIVE"));

        public Task<IReadOnlyList<ProductImage>> GetActiveProductImagesAsync(
            Guid tenantId,
            Guid productId,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<ProductImage>>(
                ProductImages.Where(x => x.Status == "ACTIVE").ToList());

        public Task<ProductImage?> GetProductImageAsync(
            Guid tenantId,
            Guid productId,
            Guid productImageId,
            CancellationToken cancellationToken) =>
            Task.FromResult(ProductImages.FirstOrDefault(x => x.Id == productImageId));

        public Task<MediaAsset?> GetMediaAssetAsync(
            Guid tenantId,
            Guid mediaAssetId,
            CancellationToken cancellationToken) =>
            Task.FromResult(MediaAssets.FirstOrDefault(x => x.Id == mediaAssetId));

        public Task<bool> IsMediaAssetLinkedAsync(
            Guid tenantId,
            Guid mediaAssetId,
            CancellationToken cancellationToken) =>
            Task.FromResult(ProductImages.Any(x => x.MediaAssetId == mediaAssetId && x.Status != "DELETED"));

        public Task<IReadOnlyList<TenantAdminProductImageResponse>> GetProductImageResponsesAsync(
            Guid tenantId,
            Guid productId,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<TenantAdminProductImageResponse>>(
                ProductImages
                    .Where(x => x.Status == "ACTIVE")
                    .OrderBy(x => x.SortOrder)
                    .Select(x => new TenantAdminProductImageResponse(
                        x.Id,
                        x.MediaAssetId,
                        x.ProductVariantId,
                        MediaAssets.FirstOrDefault(m => m.Id == x.MediaAssetId)?.PublicUrl ?? string.Empty,
                        x.AltText,
                        x.ImagePurpose,
                        x.SortOrder,
                        x.IsPrimaryImage))
                    .ToList());

        public Task SaveChangesAsync(CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task ExecuteInTransactionAsync(
            Func<CancellationToken, Task> action,
            CancellationToken cancellationToken) =>
            action(cancellationToken);
    }
}
