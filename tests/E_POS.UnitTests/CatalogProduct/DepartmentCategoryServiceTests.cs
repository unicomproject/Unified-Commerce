using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Platform.Subscription.Contracts;
using E_POS.Application.Modules.Tenant.CatalogProduct.Contracts;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos;
using E_POS.Application.Modules.Tenant.CatalogProduct.Services;
using E_POS.Application.Modules.Tenant.CatalogProduct.Validators;
using E_POS.Domain.Modules.Platform.Subscription.Constants;
using E_POS.Domain.Modules.Shared.Media.Entities;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using Xunit;

namespace E_POS.UnitTests.CatalogProduct;

public sealed class DepartmentCategoryServiceTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 7, 3, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task DepartmentCreateAsync_WithoutCreateOrManagePermission_ReturnsPermissionDenied()
    {
        var service = new DepartmentService(new FakeDepartmentRepository(), new DepartmentRequestValidator(), new FakeDateTimeProvider());

        var result = await service.CreateAsync(
            CreateContext([]),
            new DepartmentCreateRequest("GROCERY", "Grocery", null, 0, DepartmentConstants.ActiveStatus),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("department.permission_denied", result.Error.Code);
    }

    [Fact]
    public async Task DepartmentCreateAsync_WithCreatePermission_NormalizesCodeAndPersists()
    {
        var repository = new FakeDepartmentRepository();
        var service = new DepartmentService(repository, new DepartmentRequestValidator(), new FakeDateTimeProvider());

        var result = await service.CreateAsync(
            CreateContext([DepartmentConstants.CreatePermission]),
            new DepartmentCreateRequest(" grocery ", "Grocery", null, 0, DepartmentConstants.ActiveStatus),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("GROCERY", repository.AddedDepartment?.DepartmentCode);
        Assert.Equal(TenantId, repository.AddedDepartment?.TenantId);
    }

    [Fact]
    public async Task CategoryCreateAsync_WithRootCategory_PersistsWithoutParent()
    {
        var repository = new FakeCategoryRepository();
        var result = await CreateCategoryService(repository).CreateAsync(
            CreateContext([CategoryConstants.CreatePermission]),
            new CategoryCreateRequest("FOOD", "Food", "food", null, CategoryConstants.ActiveStatus, null, 1),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(repository.AddedCategory?.ParentCategoryId);
        Assert.Equal("FOOD", repository.AddedCategory?.CategoryCode);
    }

    [Fact]
    public async Task CategoryCreateAsync_DoesNotPersistUnmanagedImageUrl()
    {
        var repository = new FakeCategoryRepository();
        var result = await CreateCategoryService(repository).CreateAsync(
            CreateContext([CategoryConstants.CreatePermission]),
            new CategoryCreateRequest("FOOD", "Food", "food", null, CategoryConstants.ActiveStatus, null, 1),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(repository.AddedMediaAssets);
        Assert.Null(repository.AddedCategory?.ImageMediaAssetId);
    }

    [Fact]
    public async Task CategoryCreateAsync_WithMissingParent_ReturnsParentNotFound()
    {
        var repository = new FakeCategoryRepository { ParentInfo = null };
        var result = await CreateCategoryService(repository).CreateAsync(
            CreateContext([CategoryConstants.CreatePermission]),
            new CategoryCreateRequest("MILK", "Milk", "milk", null, CategoryConstants.ActiveStatus, Guid.NewGuid(), 1),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("category.parent_not_found", result.Error.Code);
    }

    [Fact]
    public async Task CategoryUpdateAsync_WhenParentIsSelf_ReturnsSelfReference()
    {
        var categoryId = Guid.NewGuid();
        var repository = new FakeCategoryRepository
        {
            EditableCategory = Category.Create(categoryId, TenantId, null, "FOOD", "Food", "food", null, 1, CategoryConstants.ActiveStatus, UserId, Now)
        };
        var result = await CreateCategoryService(repository).UpdateAsync(
            CreateContext([CategoryConstants.UpdatePermission]),
            categoryId,
            new CategoryUpdateRequest("FOOD", "Food", "food", null, CategoryConstants.ActiveStatus, categoryId, 1),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("category.parent_self_reference", result.Error.Code);
    }

    [Fact]
    public async Task CategoryUpdateAsync_DoesNotClearLinkedMediaViaMasterDataRequest()
    {
        var categoryId = Guid.NewGuid();
        var mediaAssetId = Guid.NewGuid();
        var category = Category.Create(categoryId, TenantId, null, "FOOD", "Food", "food", null, "https://cdn.example.test/category-old.png", 1, CategoryConstants.ActiveStatus, UserId, Now);
        category.UpdateImage("https://cdn.example.test/category-old.png", mediaAssetId, UserId, Now);
        var repository = new FakeCategoryRepository { EditableCategory = category };
        var result = await CreateCategoryService(repository).UpdateAsync(
            CreateContext([CategoryConstants.UpdatePermission]),
            categoryId,
            new CategoryUpdateRequest("FOOD", "Food", "food", null, CategoryConstants.ActiveStatus, null, 1),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(mediaAssetId, category.ImageMediaAssetId);
        Assert.Empty(repository.InactivatedMediaAssetIds);
    }

    [Fact]
    public async Task CategoryDeleteAsync_WithLinkedMediaAsset_MarksMediaInactive()
    {
        var categoryId = Guid.NewGuid();
        var mediaAssetId = Guid.NewGuid();
        var category = Category.Create(categoryId, TenantId, null, "FOOD", "Food", "food", null, "https://cdn.example.test/category.png", 1, CategoryConstants.ActiveStatus, UserId, Now);
        category.UpdateImage("https://cdn.example.test/category.png", mediaAssetId, UserId, Now);
        var repository = new FakeCategoryRepository { EditableCategory = category };
        var result = await CreateCategoryService(repository).DeleteAsync(
            CreateContext([CategoryConstants.DeletePermission]),
            categoryId,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(CategoryConstants.DeletedStatus, category.Status);
        Assert.Equal([mediaAssetId], repository.InactivatedMediaAssetIds);
    }

    [Fact]
    public async Task CategoryDeleteAsync_WhenChildCategoriesExist_ReturnsConflict()
    {
        var categoryId = Guid.NewGuid();
        var repository = new FakeCategoryRepository
        {
            EditableCategory = Category.Create(categoryId, TenantId, null, "FOOD", "Food", "food", null, 1, CategoryConstants.ActiveStatus, UserId, Now),
            HasChildCategories = true
        };
        var result = await CreateCategoryService(repository).DeleteAsync(CreateContext([CategoryConstants.DeletePermission]), categoryId, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("category.delete_conflict", result.Error.Code);
    }

    private static CategoryService CreateCategoryService(FakeCategoryRepository repository) =>
        new(repository, new CategoryRequestValidator(), new FakeDateTimeProvider(), new AlwaysAllowedEntitlementEvaluator(), new NoOpCategoryAuditLogger());

    private static TenantRequestContext CreateContext(IReadOnlyCollection<string> permissions) =>
        new(TenantId, UserId, permissions);

    private sealed class FakeDateTimeProvider : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => Now;
    }

    private sealed class AlwaysAllowedEntitlementEvaluator : ITenantFeatureEntitlementEvaluator
    {
        public Task<TenantFeatureEntitlementEvaluation> EvaluateAsync(Guid tenantId, string featureCode, DateTimeOffset now, CancellationToken cancellationToken) =>
            Task.FromResult(TenantFeatureEntitlementEvaluation.Allowed(featureCode, PlatformTenantFeatureCodes.ProductCatalog, false, true, false));

        public Task<bool> IsEnabledAsync(Guid tenantId, string featureCode, DateTimeOffset now, CancellationToken cancellationToken) =>
            Task.FromResult(true);
    }

    private sealed class NoOpCategoryAuditLogger : ICategoryAuditLogger
    {
        public void LogCreated(Guid tenantId, Guid actorTenantUserId, Guid categoryId, string categoryCode, string status) { }
        public void LogUpdated(Guid tenantId, Guid actorTenantUserId, Guid categoryId, string categoryCode, string status, bool parentChanged, bool statusChanged) { }
        public void LogArchived(Guid tenantId, Guid actorTenantUserId, Guid categoryId, string categoryCode) { }
        public void LogImageUploaded(Guid tenantId, Guid actorTenantUserId, Guid categoryId, Guid mediaAssetId) { }
        public void LogImageRemoved(Guid tenantId, Guid actorTenantUserId, Guid categoryId, Guid? previousMediaAssetId, bool noOp) { }
    }

    private sealed class FakeDepartmentRepository : IDepartmentRepository
    {
        public Department? AddedDepartment { get; private set; }

        public Task<bool> DepartmentCodeExistsAsync(Guid tenantId, string departmentCode, Guid? excludeDepartmentId, CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public Task<DepartmentListResponse> ListAsync(Guid tenantId, int pageNumber, int pageSize, string? search, CancellationToken cancellationToken) =>
            Task.FromResult(new DepartmentListResponse([], pageNumber, pageSize, 0));

        public Task<DepartmentResponse?> GetByIdAsync(Guid tenantId, Guid departmentId, bool includeDeleted, CancellationToken cancellationToken) =>
            Task.FromResult<DepartmentResponse?>(new DepartmentResponse(departmentId, AddedDepartment!.DepartmentCode, AddedDepartment.DepartmentName, AddedDepartment.Status, AddedDepartment.CreatedAt, AddedDepartment.UpdatedAt));

        public Task<Department?> GetEditableAsync(Guid tenantId, Guid departmentId, CancellationToken cancellationToken) =>
            Task.FromResult<Department?>(AddedDepartment);

        public Task AddAsync(Department department, CancellationToken cancellationToken)
        {
            AddedDepartment = department;
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeCategoryRepository : ICategoryRepository
    {
        public CategoryParentInfo? ParentInfo { get; init; }
        public bool HasChildCategories { get; init; }
        public Category? AddedCategory { get; private set; }
        public Category? EditableCategory { get; init; }
        public List<MediaAsset> AddedMediaAssets { get; } = [];
        public List<Guid> InactivatedMediaAssetIds { get; } = [];

        public Task<string?> GetTenantStatusAsync(Guid tenantId, CancellationToken cancellationToken) =>
            Task.FromResult<string?>("active");

        public Task<bool> CategoryCodeExistsAsync(Guid tenantId, string categoryCode, Guid? excludeCategoryId, CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public Task<bool> CategoryNameExistsAsync(Guid tenantId, string categoryName, Guid? excludeCategoryId, CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public Task<bool> CategorySlugExistsAsync(Guid tenantId, string categorySlug, Guid? excludeCategoryId, CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public Task<CategoryParentInfo?> GetParentInfoAsync(Guid tenantId, Guid parentCategoryId, CancellationToken cancellationToken) =>
            Task.FromResult(ParentInfo);

        public Task<bool> WouldCreateParentCycleAsync(Guid tenantId, Guid categoryId, Guid parentCategoryId, CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public Task<int> GetSubtreeRelativeDepthAsync(Guid tenantId, Guid categoryId, CancellationToken cancellationToken) =>
            Task.FromResult(1);

        public Task<bool> HasChildCategoriesAsync(Guid tenantId, Guid categoryId, CancellationToken cancellationToken) =>
            Task.FromResult(HasChildCategories);

        public Task<bool> HasProductLinksAsync(Guid tenantId, Guid categoryId, CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public Task<CategoryListResponse> ListAsync(Guid tenantId, CategoryListQuery query, CancellationToken cancellationToken) =>
            Task.FromResult(new CategoryListResponse([], query.PageNumber, query.PageSize, 0));

        public Task<CategoryTreeResponse> GetTreeAsync(Guid tenantId, CancellationToken cancellationToken) =>
            Task.FromResult(new CategoryTreeResponse([]));

        public Task<CategoryResponse?> GetByIdAsync(Guid tenantId, Guid categoryId, bool includeDeleted, CancellationToken cancellationToken)
        {
            var category = AddedCategory ?? EditableCategory;
            return Task.FromResult<CategoryResponse?>(new CategoryResponse(
                categoryId,
                category!.ParentCategoryId,
                null,
                null,
                category.CategoryCode,
                category.CategoryName,
                category.CategorySlug,
                category.Description,
                category.ImageMediaAssetId,
                null,
                category.Status,
                category.SortOrder,
                category.CreatedAt,
                category.UpdatedAt,
                1,
                category.CategoryName,
                0,
                0,
                false));
        }

        public Task<Category?> GetEditableAsync(Guid tenantId, Guid categoryId, CancellationToken cancellationToken) =>
            Task.FromResult(EditableCategory);

        public Task AddAsync(Category category, CancellationToken cancellationToken)
        {
            AddedCategory = category;
            return Task.CompletedTask;
        }

        public Task AddMediaAssetAsync(MediaAsset mediaAsset, CancellationToken cancellationToken)
        {
            AddedMediaAssets.Add(mediaAsset);
            return Task.CompletedTask;
        }

        public Task MarkMediaAssetInactiveAsync(Guid tenantId, Guid mediaAssetId, Guid? updatedByTenantUserId, DateTimeOffset now, CancellationToken cancellationToken)
        {
            InactivatedMediaAssetIds.Add(mediaAssetId);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
