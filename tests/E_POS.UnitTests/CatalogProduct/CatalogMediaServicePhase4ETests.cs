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
            CreateContext([ProductConstants.MediaManagePermission]),
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
            CreateContext([ProductConstants.MediaManagePermission]),
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
            CreateContext([ProductConstants.MediaManagePermission]),
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
        Assert.Equal(result.Value.PublicUrl, repository.MediaAssets.Single().PublicUrl);
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
        Assert.Equal(result.Value.PublicUrl, repository.MediaAssets.Single().PublicUrl);
        Assert.Equal([previousMediaAssetId], repository.InactivatedMediaAssetIds);
        Assert.Single(repository.MediaAssets);
        Assert.Single(storage.Uploads);
    }

    [Fact]
    public async Task UploadBrandLogoAsync_CreateOnlyCreatorWithoutLogo_AttachesInitialLogo()
    {
        var brand = CreateBrand(null);
        var repository = new FakeCatalogMediaRepository { BrandForLogoUpdate = brand };
        var storage = new FakeMediaObjectStorage();
        var audit = new FakeBrandAuditLogger();
        var service = new CatalogMediaService(repository, storage, new FakeDateTimeProvider(), audit);
        await using var stream = new MemoryStream(CreateOnePixelPng());

        var result = await service.UploadBrandLogoAsync(
            CreateContext([BrandConstants.CreatePermission]), BrandId,
            new MediaUploadFile(stream, "brand.png", "image/png", stream.Length), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(brand.LogoMediaAssetId);
        Assert.Single(repository.MediaAssets);
        Assert.Single(storage.Uploads);
        var auditEvent = Assert.Single(audit.Events);
        Assert.Equal(("InitialBrandLogoAttached", TenantId, UserId, BrandId, 2L), auditEvent);
    }

    [Fact]
    public async Task UploadBrandLogoAsync_ManageWithoutLogo_AttachesInitialLogo()
    {
        var brand = CreateBrand(null);
        var repository = new FakeCatalogMediaRepository { BrandForLogoUpdate = brand };
        var storage = new FakeMediaObjectStorage();
        var audit = new FakeBrandAuditLogger();
        var service = new CatalogMediaService(repository, storage, new FakeDateTimeProvider(), audit);
        await using var stream = new MemoryStream(CreateOnePixelPng());

        var result = await service.UploadBrandLogoAsync(
            CreateContext([BrandConstants.ManagePermission]), BrandId,
            new MediaUploadFile(stream, "brand.png", "image/png", stream.Length), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(brand.LogoMediaAssetId);
    }

    [Fact]
    public async Task UploadBrandLogoAsync_CreateOnlyStorageFailure_CanRetrySameBrandId()
    {
        var brand = CreateBrand(null);
        var repository = new FakeCatalogMediaRepository { BrandForLogoUpdate = brand };
        var storage = new FakeMediaObjectStorage { ThrowOnUpload = true };
        var service = new CatalogMediaService(repository, storage, new FakeDateTimeProvider());

        await using (var firstStream = new MemoryStream(CreateOnePixelPng()))
        {
            var failed = await service.UploadBrandLogoAsync(
                CreateContext([BrandConstants.CreatePermission]), BrandId,
                new MediaUploadFile(firstStream, "brand.png", "image/png", firstStream.Length), CancellationToken.None);

            Assert.True(failed.IsFailure);
            Assert.Equal("media.storage_unavailable", failed.Error.Code);
            Assert.Null(brand.LogoMediaAssetId);
            Assert.Empty(repository.MediaAssets);
        }

        storage.ThrowOnUpload = false;
        await using var retryStream = new MemoryStream(CreateOnePixelPng());
        var retried = await service.UploadBrandLogoAsync(
            CreateContext([BrandConstants.CreatePermission]), BrandId,
            new MediaUploadFile(retryStream, "brand.png", "image/png", retryStream.Length), CancellationToken.None);

        Assert.True(retried.IsSuccess);
        Assert.Equal(BrandId, retried.Value!.BrandId);
        Assert.NotNull(brand.LogoMediaAssetId);
        Assert.Single(repository.MediaAssets);
    }

    [Fact]
    public async Task UploadBrandLogoAsync_CreateOnlyCreatorWithExistingLogo_RejectsReplacement()
    {
        var brand = CreateBrand(Guid.NewGuid());
        var repository = new FakeCatalogMediaRepository { BrandForLogoUpdate = brand };
        var storage = new FakeMediaObjectStorage();
        var audit = new FakeBrandAuditLogger();
        var service = new CatalogMediaService(repository, storage, new FakeDateTimeProvider(), audit);
        await using var stream = new MemoryStream(CreateOnePixelPng());

        var result = await service.UploadBrandLogoAsync(
            CreateContext([BrandConstants.CreatePermission]), BrandId,
            new MediaUploadFile(stream, "brand.png", "image/png", stream.Length), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("media.initial_brand_logo_not_authorized", result.Error.Code);
        Assert.Empty(repository.MediaAssets);
        Assert.Empty(storage.Uploads);
        Assert.Empty(audit.Events);
    }

    [Fact]
    public async Task UploadBrandLogoAsync_CreateOnlyNonCreatorWithoutLogo_RejectsArbitraryBrand()
    {
        var brand = CreateBrand(null, Guid.NewGuid());
        var repository = new FakeCatalogMediaRepository { BrandForLogoUpdate = brand };
        var storage = new FakeMediaObjectStorage();
        var audit = new FakeBrandAuditLogger();
        var service = new CatalogMediaService(repository, storage, new FakeDateTimeProvider(), audit);
        await using var stream = new MemoryStream(CreateOnePixelPng());

        var result = await service.UploadBrandLogoAsync(
            CreateContext([BrandConstants.CreatePermission]), BrandId,
            new MediaUploadFile(stream, "brand.png", "image/png", stream.Length), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("media.initial_brand_logo_not_authorized", result.Error.Code);
        Assert.Empty(repository.MediaAssets);
        Assert.Empty(storage.Uploads);
        Assert.Empty(audit.Events);
    }

    [Theory]
    [InlineData(BrandConstants.UpdatePermission)]
    [InlineData(BrandConstants.ManagePermission)]
    public async Task UploadBrandLogoAsync_UpdateOrManage_ReplacesExistingLogo(string permission)
    {
        var previousMediaAssetId = Guid.NewGuid();
        var brand = CreateBrand(previousMediaAssetId);
        var repository = new FakeCatalogMediaRepository { BrandForLogoUpdate = brand };
        var storage = new FakeMediaObjectStorage();
        var audit = new FakeBrandAuditLogger();
        var service = new CatalogMediaService(repository, storage, new FakeDateTimeProvider(), audit);
        await using var stream = new MemoryStream(CreateOnePixelPng());

        var result = await service.UploadBrandLogoAsync(
            CreateContext([permission]), BrandId,
            new MediaUploadFile(stream, "brand.png", "image/png", stream.Length), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotEqual(previousMediaAssetId, brand.LogoMediaAssetId);
        Assert.Contains(previousMediaAssetId, repository.InactivatedMediaAssetIds);
        var auditEvent = Assert.Single(audit.Events);
        Assert.Equal(("BrandLogoReplaced", TenantId, UserId, BrandId, 2L), auditEvent);
    }

    [Fact]
    public async Task UploadBrandLogoAsync_CreateOnlyOtherTenantBrand_ReturnsNotFound()
    {
        var repository = new FakeCatalogMediaRepository { BrandForLogoUpdate = CreateBrand(null) };
        var storage = new FakeMediaObjectStorage();
        var service = new CatalogMediaService(repository, storage, new FakeDateTimeProvider());
        await using var stream = new MemoryStream(CreateOnePixelPng());
        var otherTenantContext = new TenantRequestContext(Guid.NewGuid(), UserId, [BrandConstants.CreatePermission]);

        var result = await service.UploadBrandLogoAsync(
            otherTenantContext, BrandId,
            new MediaUploadFile(stream, "brand.png", "image/png", stream.Length), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("media.brand_not_found", result.Error.Code);
        Assert.Empty(storage.Uploads);
    }

    [Fact]
    public async Task UploadBrandLogoAsync_CreateOnlyDeletedBrand_ReturnsNotFound()
    {
        var brand = CreateBrand(null);
        brand.SoftDelete(UserId, Now);
        var repository = new FakeCatalogMediaRepository { BrandForLogoUpdate = brand };
        var storage = new FakeMediaObjectStorage();
        var service = new CatalogMediaService(repository, storage, new FakeDateTimeProvider());
        await using var stream = new MemoryStream(CreateOnePixelPng());

        var result = await service.UploadBrandLogoAsync(
            CreateContext([BrandConstants.CreatePermission]), BrandId,
            new MediaUploadFile(stream, "brand.png", "image/png", stream.Length), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("media.brand_not_found", result.Error.Code);
        Assert.Empty(storage.Uploads);
    }

    [Fact]
    public async Task UploadBrandLogoAsync_WithJpeg_Succeeds()
    {
        var repository = new FakeCatalogMediaRepository { BrandForLogoUpdate = CreateBrand(null) };
        var storage = new FakeMediaObjectStorage();
        var service = new CatalogMediaService(repository, storage, new FakeDateTimeProvider());
        await using var stream = new MemoryStream(CreateOnePixelJpeg());

        var result = await service.UploadBrandLogoAsync(
            CreateContext([BrandConstants.UpdatePermission]), BrandId,
            new MediaUploadFile(stream, "brand.jpg", "image/jpeg", stream.Length), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(storage.Uploads);
    }

    [Theory]
    [InlineData("brand.webp", "image/webp")]
    [InlineData("brand.png", "image/webp")]
    public async Task UploadBrandLogoAsync_WithWebP_IsRejected(string fileName, string contentType)
    {
        var repository = new FakeCatalogMediaRepository { BrandForLogoUpdate = CreateBrand(null) };
        var storage = new FakeMediaObjectStorage();
        var service = new CatalogMediaService(repository, storage, new FakeDateTimeProvider());
        await using var stream = new MemoryStream(CreateOnePixelWebP());

        var result = await service.UploadBrandLogoAsync(
            CreateContext([BrandConstants.UpdatePermission]), BrandId,
            new MediaUploadFile(stream, fileName, contentType, stream.Length), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("media.unsupported_media_type", result.Error.Code);
        Assert.Empty(storage.Uploads);
    }

    [Fact]
    public async Task UploadBrandLogoAsync_OverTwoMegabytes_IsRejected()
    {
        var repository = new FakeCatalogMediaRepository { BrandForLogoUpdate = CreateBrand(null) };
        var storage = new FakeMediaObjectStorage();
        var service = new CatalogMediaService(repository, storage, new FakeDateTimeProvider());
        await using var stream = new MemoryStream(CreateOnePixelPng());

        var result = await service.UploadBrandLogoAsync(
            CreateContext([BrandConstants.UpdatePermission]), BrandId,
            new MediaUploadFile(stream, "brand.png", "image/png", 2 * 1024 * 1024 + 1), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("media.file_size_exceeded", result.Error.Code);
        Assert.Empty(storage.Uploads);
    }

    [Fact]
    public async Task UploadBrandLogoAsync_WithoutUpdateOrManagePermission_FailsBeforeMutation()
    {
        var repository = new FakeCatalogMediaRepository { BrandForLogoUpdate = CreateBrand(null) };
        var storage = new FakeMediaObjectStorage();
        var service = new CatalogMediaService(repository, storage, new FakeDateTimeProvider());
        await using var stream = new MemoryStream(CreateOnePixelPng());

        var result = await service.UploadBrandLogoAsync(
            CreateContext([]), BrandId,
            new MediaUploadFile(stream, "brand.png", "image/png", stream.Length), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("media.permission_denied", result.Error.Code);
        Assert.Empty(storage.Uploads);
        Assert.Empty(repository.MediaAssets);
    }

    private static TenantRequestContext CreateContext(IReadOnlyCollection<string> permissions) =>
        new(TenantId, UserId, permissions);

    private static byte[] CreateOnePixelPng() =>
        Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+/p9sAAAAASUVORK5CYII=");

    private static byte[] CreateOnePixelWebP() =>
        Convert.FromBase64String("UklGRiIAAABXRUJQVlA4IBYAAAAwAQCdASoBAAEAAUAmJaQAA3AA/vuUAAA=");

    private static byte[] CreateOnePixelJpeg() =>
        Convert.FromBase64String("/9j/4AAQSkZJRgABAQAAAQABAAD/2wBDAP//////////////////////////////////////////////////////////////////////////////////////2wBDAf//////////////////////////////////////////////////////////////////////////////////////wAARCAABAAEDASIAAhEBAxEB/8QAFQABAQAAAAAAAAAAAAAAAAAAAAf/xAAUEAEAAAAAAAAAAAAAAAAAAAAA/9oADAMBAAIQAxAAAAF//8QAFBABAAAAAAAAAAAAAAAAAAAAAP/aAAgBAQABBQJ//8QAFBEBAAAAAAAAAAAAAAAAAAAAAP/aAAgBAwEBPwF//8QAFBEBAAAAAAAAAAAAAAAAAAAAAP/aAAgBAgEBPwF//8QAFBABAAAAAAAAAAAAAAAAAAAAAP/aAAgBAQAGPwJ//8QAFBABAAAAAAAAAAAAAAAAAAAAAP/aAAgBAQABPyF//9oADAMBAAIAAwAAABD/xAAUEQEAAAAAAAAAAAAAAAAAAAAA/9oACAEDAQE/EB//xAAUEQEAAAAAAAAAAAAAAAAAAAAA/9oACAECAQE/EB//xAAUEAEAAAAAAAAAAAAAAAAAAAAA/9oACAEBAAE/EB//2Q==");

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

    private static Brand CreateBrand(Guid? logoMediaAssetId, Guid? createdByUserId = null)
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
            createdByUserId ?? UserId,
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

    private sealed class FakeBrandAuditLogger : IBrandAuditLogger
    {
        public List<(string EventName, Guid TenantId, Guid UserId, Guid BrandId, long RowVersion)> Events { get; } = [];

        public void LogMutation(string eventName, Guid tenantId, Guid userId, Guid brandId, long rowVersion) =>
            Events.Add((eventName, tenantId, userId, brandId, rowVersion));
    }

    private sealed class FakeMediaObjectStorage : IMediaObjectStorage
    {
        public bool IsConfigured { get; init; } = true;
        public bool ThrowOnUpload { get; set; }
        public List<MediaObjectUploadResult> Uploads { get; } = [];

        public Task<MediaObjectUploadResult> UploadAsync(
            MediaObjectUploadRequest request,
            CancellationToken cancellationToken)
        {
            if (ThrowOnUpload)
            {
                throw new InvalidOperationException("Storage unavailable.");
            }

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
            Task.FromResult(
                tenantId == TenantId &&
                brandId == BrandId &&
                BrandForLogoUpdate?.Status != BrandConstants.DeletedStatus
                    ? BrandForLogoUpdate
                    : null);

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

        public Task<Product?> GetProductForUpdateAsync(
            Guid tenantId,
            Guid productId,
            CancellationToken cancellationToken) =>
            Task.FromResult<Product?>(null);

        public Task<int> CountActiveProductImagesAsync(
            Guid tenantId,
            Guid productId,
            CancellationToken cancellationToken) =>
            Task.FromResult(ProductImages.Count(x => x.Status == "ACTIVE"));

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
            Task.FromResult<IReadOnlyList<TenantAdminProductImageResponse>>([]);

        public Task SaveChangesAsync(CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task ExecuteInTransactionAsync(
            Func<CancellationToken, Task> action,
            CancellationToken cancellationToken) =>
            action(cancellationToken);
    }
}
