using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Platform.Subscription.Contracts;
using E_POS.Application.Modules.Shared.Media.Contracts;
using E_POS.Application.Modules.Shared.Media.Dtos;
using E_POS.Application.Modules.Tenant.CatalogProduct.Contracts;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.TenantAdmin;
using E_POS.Application.Modules.Tenant.CatalogProduct.Services;
using E_POS.Domain.Modules.Platform.Subscription.Constants;
using E_POS.Domain.Modules.Shared.Media.Entities;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using Xunit;

namespace E_POS.UnitTests.CatalogProduct;

public sealed class CatalogMediaCategoryServiceTests
{
    private static readonly Guid TenantId = Guid.Parse("aaaaaaaa-0000-4000-8000-000000000001");
    private static readonly Guid OtherTenantId = Guid.Parse("aaaaaaaa-9999-4000-8000-000000000001");
    private static readonly Guid UserId = Guid.Parse("bbbbbbbb-0000-4000-8000-000000000001");
    private static readonly Guid CategoryId = Guid.Parse("dddddddd-0000-4000-8000-000000000001");
    private static readonly DateTimeOffset Now = new(2026, 8, 27, 15, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Upload_WithProductCatalogAndUpdatePermission_Succeeds()
    {
        var result = await UploadAsync(CreateContext([CategoryConstants.UpdatePermission]));
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Upload_WithManagePermission_Succeeds()
    {
        var result = await UploadAsync(CreateContext([CategoryConstants.ManagePermission]));
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Upload_WithoutProductCatalog_ReturnsEntitlementDenied()
    {
        var result = await UploadAsync(CreateContext([CategoryConstants.UpdatePermission]), entitlementAllowed: false);
        Assert.Equal("category.entitlement_denied", result.Error.Code);
    }

    [Fact]
    public async Task Upload_WithoutUpdateOrManage_ReturnsPermissionDenied()
    {
        var result = await UploadAsync(CreateContext([CategoryConstants.ViewPermission]));
        Assert.Equal("category.permission_denied", result.Error.Code);
    }

    [Fact]
    public async Task Upload_CrossTenantCategory_ReturnsNotFound()
    {
        var repository = new FakeCatalogMediaRepository { CategoryForImageUpdate = CreateCategory(null) };
        var result = await UploadAsync(
            new TenantRequestContext(OtherTenantId, UserId, [CategoryConstants.UpdatePermission]),
            repository: repository);
        Assert.Equal("category.not_found", result.Error.Code);
    }

    [Fact]
    public async Task Upload_StorageException_ReturnsSafeUnexpectedFailure()
    {
        var result = await UploadAsync(
            CreateContext([CategoryConstants.UpdatePermission]),
            storage: new FakeMediaObjectStorage { ThrowOnUpload = new InvalidOperationException("blob secret=abc connection=xyz") });
        Assert.Equal("media.unexpected_failure", result.Error.Code);
        Assert.DoesNotContain("secret", result.Error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("blob", result.Error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("connection", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Upload_SaveException_ReturnsSafeSaveFailed()
    {
        var repository = new FakeCatalogMediaRepository
        {
            CategoryForImageUpdate = CreateCategory(null),
            SaveChangesThrows = true
        };
        var result = await UploadAsync(CreateContext([CategoryConstants.UpdatePermission]), repository: repository);
        Assert.Equal("media.save_failed", result.Error.Code);
        Assert.Equal("Category image could not be saved.", result.Error.Message);
        Assert.DoesNotContain("boom", result.Error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.False(repository.Saved);
    }

    [Fact]
    public async Task Remove_ExistingImage_ClearsAssetAndMarksOwnedMediaInactive()
    {
        var previousId = Guid.NewGuid();
        var category = CreateCategory(previousId);
        var repository = new FakeCatalogMediaRepository { CategoryForImageUpdate = category };
        var audit = new RecordingCategoryAuditLogger();
        var result = await CreateService(repository, audit: audit).RemoveCategoryImageAsync(
            CreateContext([CategoryConstants.UpdatePermission]),
            CategoryId,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(category.ImageMediaAssetId);
        Assert.Equal([previousId], repository.InactivatedMediaAssetIds);
        Assert.Contains("category.image_removed", audit.Actions);
        Assert.True(repository.Saved);
    }

    [Fact]
    public async Task Remove_WhenNoImage_IsNoOpSuccess()
    {
        var category = CreateCategory(null);
        var repository = new FakeCatalogMediaRepository { CategoryForImageUpdate = category };
        var audit = new RecordingCategoryAuditLogger();
        var result = await CreateService(repository, audit: audit).RemoveCategoryImageAsync(
            CreateContext([CategoryConstants.UpdatePermission]),
            CategoryId,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(category.ImageMediaAssetId);
        Assert.Empty(repository.InactivatedMediaAssetIds);
        Assert.Contains("category.image_removed_noop", audit.Actions);
    }

    [Fact]
    public async Task Remove_WithoutPermission_ReturnsDenied()
    {
        var result = await CreateService(new FakeCatalogMediaRepository { CategoryForImageUpdate = CreateCategory(Guid.NewGuid()) })
            .RemoveCategoryImageAsync(CreateContext([CategoryConstants.ViewPermission]), CategoryId, CancellationToken.None);
        Assert.Equal("category.permission_denied", result.Error.Code);
    }

    [Fact]
    public async Task Remove_WithoutEntitlement_ReturnsDenied()
    {
        var result = await CreateService(
                new FakeCatalogMediaRepository { CategoryForImageUpdate = CreateCategory(Guid.NewGuid()) },
                entitlementAllowed: false)
            .RemoveCategoryImageAsync(CreateContext([CategoryConstants.UpdatePermission]), CategoryId, CancellationToken.None);
        Assert.Equal("category.entitlement_denied", result.Error.Code);
    }

    [Fact]
    public async Task Remove_CrossTenantCategory_ReturnsNotFound()
    {
        var result = await CreateService(new FakeCatalogMediaRepository { CategoryForImageUpdate = CreateCategory(Guid.NewGuid()) })
            .RemoveCategoryImageAsync(
                new TenantRequestContext(OtherTenantId, UserId, [CategoryConstants.UpdatePermission]),
                CategoryId,
                CancellationToken.None);
        Assert.Equal("category.not_found", result.Error.Code);
    }

    [Fact]
    public async Task Remove_CrossTenantMediaReference_DoesNotMarkForeignAssetInactive()
    {
        var foreignMediaId = Guid.NewGuid();
        var category = CreateCategory(foreignMediaId);
        var repository = new FakeCatalogMediaRepository
        {
            CategoryForImageUpdate = category,
            ForeignMediaAssetIds = [foreignMediaId]
        };
        var result = await CreateService(repository).RemoveCategoryImageAsync(
            CreateContext([CategoryConstants.UpdatePermission]),
            CategoryId,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(category.ImageMediaAssetId);
        Assert.Empty(repository.InactivatedMediaAssetIds);
    }

    [Fact]
    public async Task Remove_SaveFailure_DoesNotReportSaved()
    {
        var category = CreateCategory(Guid.NewGuid());
        var repository = new FakeCatalogMediaRepository
        {
            CategoryForImageUpdate = category,
            SaveChangesThrows = true
        };
        var result = await CreateService(repository).RemoveCategoryImageAsync(
            CreateContext([CategoryConstants.UpdatePermission]),
            CategoryId,
            CancellationToken.None);

        Assert.Equal("media.save_failed", result.Error.Code);
        Assert.False(repository.Saved);
        Assert.DoesNotContain("boom", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<ApplicationResult<MediaAssetUploadResponse>> UploadAsync(
        TenantRequestContext context,
        bool entitlementAllowed = true,
        FakeCatalogMediaRepository? repository = null,
        FakeMediaObjectStorage? storage = null)
    {
        repository ??= new FakeCatalogMediaRepository { CategoryForImageUpdate = CreateCategory(null) };
        storage ??= new FakeMediaObjectStorage();
        var service = CreateService(repository, storage, entitlementAllowed);
        await using var stream = new MemoryStream(CreateOnePixelPng());
        return await service.UploadCategoryImageAsync(
            context,
            CategoryId,
            new MediaUploadFile(stream, "category.png", "image/png", stream.Length),
            CancellationToken.None);
    }

    private static CatalogMediaService CreateService(
        FakeCatalogMediaRepository repository,
        FakeMediaObjectStorage? storage = null,
        bool entitlementAllowed = true,
        ICategoryAuditLogger? audit = null) =>
        new(
            repository,
            storage ?? new FakeMediaObjectStorage(),
            new FakeDateTimeProvider(),
            urlResolver: null,
            categoryAccessPolicy: new CategoryAccessPolicy(
                new StubCategoryRepository(),
                new FakeEntitlementEvaluator(entitlementAllowed),
                new FakeDateTimeProvider()),
            categoryAuditLogger: audit);

    private static TenantRequestContext CreateContext(IReadOnlyCollection<string> permissions) =>
        new(TenantId, UserId, permissions);

    private static Category CreateCategory(Guid? imageMediaAssetId)
    {
        var category = Category.Create(
            CategoryId,
            TenantId,
            parentCategoryId: null,
            "APPAREL",
            "Apparel",
            "apparel",
            null,
            1,
            CategoryConstants.ActiveStatus,
            UserId,
            Now);
        if (imageMediaAssetId.HasValue)
        {
            category.UpdateImage(imageMediaAssetId, UserId, Now);
        }

        return category;
    }

    private static byte[] CreateOnePixelPng() =>
        Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+/p9sAAAAASUVORK5CYII=");

    private sealed class FakeDateTimeProvider : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => Now;
    }

    private sealed class FakeEntitlementEvaluator(bool allowed) : ITenantFeatureEntitlementEvaluator
    {
        public Task<TenantFeatureEntitlementEvaluation> EvaluateAsync(
            Guid tenantId,
            string featureCode,
            DateTimeOffset now,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(allowed
                ? TenantFeatureEntitlementEvaluation.Allowed(featureCode, PlatformTenantFeatureCodes.ProductCatalog, false, true, false)
                : TenantFeatureEntitlementEvaluation.Denied(
                    TenantFeatureEntitlementDecision.Disabled,
                    featureCode,
                    PlatformTenantFeatureCodes.ProductCatalog,
                    false,
                    true,
                    false,
                    "disabled"));
        }

        public Task<bool> IsEnabledAsync(Guid tenantId, string featureCode, DateTimeOffset now, CancellationToken cancellationToken) =>
            Task.FromResult(allowed);
    }

    private sealed class RecordingCategoryAuditLogger : ICategoryAuditLogger
    {
        public List<string> Actions { get; } = [];

        public void LogCreated(Guid tenantId, Guid actorTenantUserId, Guid categoryId, string categoryCode, string status) =>
            Actions.Add("category.created");

        public void LogUpdated(Guid tenantId, Guid actorTenantUserId, Guid categoryId, string categoryCode, string status, bool parentChanged, bool statusChanged) =>
            Actions.Add("category.updated");

        public void LogArchived(Guid tenantId, Guid actorTenantUserId, Guid categoryId, string categoryCode) =>
            Actions.Add("category.archived");

        public void LogImageUploaded(Guid tenantId, Guid actorTenantUserId, Guid categoryId, Guid mediaAssetId) =>
            Actions.Add("category.image_uploaded");

        public void LogImageRemoved(Guid tenantId, Guid actorTenantUserId, Guid categoryId, Guid? previousMediaAssetId, bool noOp) =>
            Actions.Add(noOp ? "category.image_removed_noop" : "category.image_removed");
    }

    private sealed class StubCategoryRepository : ICategoryRepository
    {
        public Task<string?> GetTenantStatusAsync(Guid tenantId, CancellationToken cancellationToken) =>
            Task.FromResult<string?>("active");

        public Task<bool> CategoryCodeExistsAsync(Guid tenantId, string categoryCode, Guid? excludeCategoryId, CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public Task<bool> CategoryNameExistsAsync(Guid tenantId, string categoryName, Guid? excludeCategoryId, CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public Task<bool> CategorySlugExistsAsync(Guid tenantId, string categorySlug, Guid? excludeCategoryId, CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public Task<CategoryParentInfo?> GetParentInfoAsync(Guid tenantId, Guid parentCategoryId, CancellationToken cancellationToken) =>
            Task.FromResult<CategoryParentInfo?>(null);

        public Task<bool> WouldCreateParentCycleAsync(Guid tenantId, Guid categoryId, Guid parentCategoryId, CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public Task<int> GetSubtreeRelativeDepthAsync(Guid tenantId, Guid categoryId, CancellationToken cancellationToken) =>
            Task.FromResult(1);

        public Task<bool> HasChildCategoriesAsync(Guid tenantId, Guid categoryId, CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public Task<bool> HasProductLinksAsync(Guid tenantId, Guid categoryId, CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public Task<CategoryListResponse> ListAsync(Guid tenantId, CategoryListQuery query, CancellationToken cancellationToken) =>
            Task.FromResult(new CategoryListResponse([], query.PageNumber, query.PageSize, 0));

        public Task<CategoryTreeResponse> GetTreeAsync(Guid tenantId, CancellationToken cancellationToken) =>
            Task.FromResult(new CategoryTreeResponse([]));

        public Task<CategoryResponse?> GetByIdAsync(Guid tenantId, Guid categoryId, bool includeDeleted, CancellationToken cancellationToken) =>
            Task.FromResult<CategoryResponse?>(null);

        public Task<Category?> GetEditableAsync(Guid tenantId, Guid categoryId, CancellationToken cancellationToken) =>
            Task.FromResult<Category?>(null);

        public Task AddAsync(Category category, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task AddMediaAssetAsync(MediaAsset mediaAsset, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task MarkMediaAssetInactiveAsync(Guid tenantId, Guid mediaAssetId, Guid? updatedByTenantUserId, DateTimeOffset now, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeMediaObjectStorage : IMediaObjectStorage
    {
        public bool IsConfigured { get; init; } = true;
        public Exception? ThrowOnUpload { get; init; }
        public List<MediaObjectUploadResult> Uploads { get; } = [];

        public Task<MediaObjectUploadResult> UploadAsync(MediaObjectUploadRequest request, CancellationToken cancellationToken)
        {
            if (ThrowOnUpload is not null)
            {
                throw ThrowOnUpload;
            }

            var result = new MediaObjectUploadResult("tenant-media", request.StorageKey, $"https://cdn.example.test/{request.StorageKey}");
            Uploads.Add(result);
            return Task.FromResult(result);
        }

        public Task DeleteIfExistsAsync(string containerName, string storageKey, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }

    private sealed class FakeCatalogMediaRepository : ICatalogMediaRepository
    {
        public Category? CategoryForImageUpdate { get; init; }
        public HashSet<Guid> ForeignMediaAssetIds { get; init; } = [];
        public bool SaveChangesThrows { get; init; }
        public bool Saved { get; private set; }
        public List<MediaAsset> MediaAssets { get; } = [];
        public List<Guid> InactivatedMediaAssetIds { get; } = [];

        public Task<bool> ProductExistsAsync(Guid tenantId, Guid productId, CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public Task<Product?> GetProductForUpdateAsync(Guid tenantId, Guid productId, CancellationToken cancellationToken) =>
            Task.FromResult<Product?>(null);

        public Task<bool> ProductVariantExistsAsync(Guid tenantId, Guid productId, Guid productVariantId, CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public Task<Category?> GetCategoryForImageUpdateAsync(Guid tenantId, Guid categoryId, CancellationToken cancellationToken) =>
            Task.FromResult(tenantId == TenantId && categoryId == CategoryId ? CategoryForImageUpdate : null);

        public Task<Brand?> GetBrandForLogoUpdateAsync(Guid tenantId, Guid brandId, CancellationToken cancellationToken) =>
            Task.FromResult<Brand?>(null);

        public Task AddMediaAssetAsync(MediaAsset mediaAsset, CancellationToken cancellationToken)
        {
            MediaAssets.Add(mediaAsset);
            return Task.CompletedTask;
        }

        public Task AddProductImageAsync(ProductImage productImage, CancellationToken cancellationToken) =>
            Task.CompletedTask;

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

        public Task<int> CountActiveProductImagesAsync(Guid tenantId, Guid productId, CancellationToken cancellationToken) =>
            Task.FromResult(0);

        public Task<IReadOnlyList<ProductImage>> GetActiveProductImagesAsync(Guid tenantId, Guid productId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<ProductImage>>([]);

        public Task<ProductImage?> GetProductImageAsync(Guid tenantId, Guid productId, Guid productImageId, CancellationToken cancellationToken) =>
            Task.FromResult<ProductImage?>(null);

        public Task<MediaAsset?> GetMediaAssetAsync(Guid tenantId, Guid mediaAssetId, CancellationToken cancellationToken)
        {
            if (tenantId != TenantId || ForeignMediaAssetIds.Contains(mediaAssetId))
            {
                return Task.FromResult<MediaAsset?>(null);
            }

            var existing = MediaAssets.FirstOrDefault(x => x.Id == mediaAssetId);
            if (existing is not null)
            {
                return Task.FromResult<MediaAsset?>(existing);
            }

            return Task.FromResult<MediaAsset?>(MediaAsset.Create(
                mediaAssetId,
                TenantId,
                "tenant-media",
                "key",
                "https://cdn.example.test/previous.png",
                "previous.png",
                "image/png",
                ".png",
                32,
                1,
                1,
                "checksum",
                "IMAGE",
                "CATEGORY",
                "ACTIVE",
                UserId,
                Now));
        }

        public Task<bool> IsMediaAssetLinkedAsync(Guid tenantId, Guid mediaAssetId, CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public Task<IReadOnlyList<TenantAdminProductImageResponse>> GetProductImageResponsesAsync(
            Guid tenantId,
            Guid productId,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<TenantAdminProductImageResponse>>([]);

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            if (SaveChangesThrows)
            {
                throw new InvalidOperationException("db boom SELECT * FROM media_assets");
            }

            Saved = true;
            return Task.CompletedTask;
        }

        public async Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken)
        {
            await action(cancellationToken);
        }
    }
}
