using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Platform.Subscription.Contracts;
using E_POS.Application.Modules.Tenant.CatalogProduct.Contracts;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos;
using E_POS.Application.Modules.Tenant.CatalogProduct.Services;
using E_POS.Application.Modules.Tenant.CatalogProduct.Validators;
using E_POS.Domain.Modules.Platform.Subscription.Constants;
using E_POS.Domain.Modules.Shared.Media.Entities;
using E_POS.Domain.Modules.Tenant.CatalogProduct;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using Xunit;

namespace E_POS.UnitTests.CatalogProduct;

public sealed class CategoryServiceTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid OtherTenantId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 8, 27, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task CreateRoot_WithoutDepartment_Persists()
    {
        var repository = new FakeCategoryRepository();
        var result = await CreateService(repository).CreateAsync(
            CreateContext([CategoryConstants.CreatePermission]),
            CreateRequest("FOOD", "Food"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(repository.AddedCategory?.ParentCategoryId);
        Assert.Equal("FOOD", repository.AddedCategory?.CategoryCode);
        Assert.Contains("category.created", repository.AuditActions);
    }

    [Fact]
    public async Task CreateChild_WithActiveParent_Persists()
    {
        var parentId = Guid.NewGuid();
        var repository = new FakeCategoryRepository
        {
            ParentInfo = new CategoryParentInfo(parentId, CategoryConstants.ActiveStatus, 1)
        };

        var result = await CreateService(repository).CreateAsync(
            CreateContext([CategoryConstants.CreatePermission]),
            CreateRequest("MILK", "Milk", parentId),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(parentId, repository.AddedCategory?.ParentCategoryId);
    }

    [Fact]
    public async Task Create_WithoutDepartmentId_DoesNotRequireDepartment()
    {
        var result = await CreateService(new FakeCategoryRepository()).CreateAsync(
            CreateContext([CategoryConstants.CreatePermission]),
            CreateRequest("FOOD", "Food"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Create_NormalizesCodeToUppercase()
    {
        var repository = new FakeCategoryRepository();
        await CreateService(repository).CreateAsync(
            CreateContext([CategoryConstants.CreatePermission]),
            CreateRequest(" abc ", "Beverages"),
            CancellationToken.None);

        Assert.Equal("ABC", repository.AddedCategory?.CategoryCode);
    }

    [Fact]
    public async Task Create_DuplicateCode_ReturnsConflict()
    {
        var repository = new FakeCategoryRepository { CodeExists = true };
        var result = await CreateService(repository).CreateAsync(
            CreateContext([CategoryConstants.CreatePermission]),
            CreateRequest("FOOD", "Food"),
            CancellationToken.None);

        Assert.Equal("category.duplicate_code", result.Error.Code);
    }

    [Fact]
    public async Task Create_DuplicateNameCaseInsensitive_ReturnsConflict()
    {
        var repository = new FakeCategoryRepository { NameExists = true };
        var result = await CreateService(repository).CreateAsync(
            CreateContext([CategoryConstants.CreatePermission]),
            CreateRequest("BEV", " beverages "),
            CancellationToken.None);

        Assert.Equal("category.duplicate_name", result.Error.Code);
    }

    [Fact]
    public async Task Create_InactiveParent_ReturnsParentInactive()
    {
        var repository = new FakeCategoryRepository
        {
            ParentInfo = new CategoryParentInfo(Guid.NewGuid(), CategoryConstants.InactiveStatus, 1)
        };

        var result = await CreateService(repository).CreateAsync(
            CreateContext([CategoryConstants.CreatePermission]),
            CreateRequest("MILK", "Milk", Guid.NewGuid()),
            CancellationToken.None);

        Assert.Equal("category.parent_inactive", result.Error.Code);
    }

    [Fact]
    public async Task Create_MissingParent_ReturnsParentNotFound()
    {
        var repository = new FakeCategoryRepository { ParentInfo = null };
        var result = await CreateService(repository).CreateAsync(
            CreateContext([CategoryConstants.CreatePermission]),
            CreateRequest("MILK", "Milk", Guid.NewGuid()),
            CancellationToken.None);

        Assert.Equal("category.parent_not_found", result.Error.Code);
    }

    [Fact]
    public async Task Create_Depth6_ReturnsMaxDepthExceeded()
    {
        var repository = new FakeCategoryRepository
        {
            ParentInfo = new CategoryParentInfo(Guid.NewGuid(), CategoryConstants.ActiveStatus, 5)
        };

        var result = await CreateService(repository).CreateAsync(
            CreateContext([CategoryConstants.CreatePermission]),
            CreateRequest("L6", "Level 6", Guid.NewGuid()),
            CancellationToken.None);

        Assert.Equal("category.max_depth_exceeded", result.Error.Code);
    }

    [Fact]
    public async Task Create_Depth5_Allowed()
    {
        var parentId = Guid.NewGuid();
        var repository = new FakeCategoryRepository
        {
            ParentInfo = new CategoryParentInfo(parentId, CategoryConstants.ActiveStatus, 4)
        };

        var result = await CreateService(repository).CreateAsync(
            CreateContext([CategoryConstants.CreatePermission]),
            CreateRequest("L5", "Level 5", parentId),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Create_CodeLongerThan80_ReturnsValidationFailed()
    {
        var result = await CreateService(new FakeCategoryRepository()).CreateAsync(
            CreateContext([CategoryConstants.CreatePermission]),
            CreateRequest(new string('A', 81), "Food"),
            CancellationToken.None);

        Assert.Equal("category.validation_failed", result.Error.Code);
    }

    [Fact]
    public async Task Create_NameLongerThan150_ReturnsValidationFailed()
    {
        var result = await CreateService(new FakeCategoryRepository()).CreateAsync(
            CreateContext([CategoryConstants.CreatePermission]),
            CreateRequest("FOOD", new string('B', 151)),
            CancellationToken.None);

        Assert.Equal("category.validation_failed", result.Error.Code);
    }

    [Fact]
    public async Task Create_DescriptionLongerThan2000_ReturnsValidationFailed()
    {
        var result = await CreateService(new FakeCategoryRepository()).CreateAsync(
            CreateContext([CategoryConstants.CreatePermission]),
            new CategoryCreateRequest("FOOD", "Food", null, new string('D', 2001), CategoryConstants.ActiveStatus, null, 0),
            CancellationToken.None);

        Assert.Equal("category.validation_failed", result.Error.Code);
    }

    [Fact]
    public async Task Create_NegativeSortOrder_ReturnsValidationFailed()
    {
        var result = await CreateService(new FakeCategoryRepository()).CreateAsync(
            CreateContext([CategoryConstants.CreatePermission]),
            new CategoryCreateRequest("FOOD", "Food", null, null, CategoryConstants.ActiveStatus, null, -1),
            CancellationToken.None);

        Assert.Equal("category.validation_failed", result.Error.Code);
    }

    [Fact]
    public async Task Create_WithoutCreateOrManage_ReturnsPermissionDenied()
    {
        var result = await CreateService(new FakeCategoryRepository()).CreateAsync(
            CreateContext([CategoryConstants.ViewPermission]),
            CreateRequest("FOOD", "Food"),
            CancellationToken.None);

        Assert.Equal("category.permission_denied", result.Error.Code);
    }

    [Fact]
    public async Task Create_WithManageFallback_Succeeds()
    {
        var result = await CreateService(new FakeCategoryRepository()).CreateAsync(
            CreateContext([CategoryConstants.ManagePermission]),
            CreateRequest("FOOD", "Food"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Create_EntitlementDisabled_ReturnsForbidden()
    {
        var result = await CreateService(new FakeCategoryRepository(), entitlementAllowed: false).CreateAsync(
            CreateContext([CategoryConstants.CreatePermission]),
            CreateRequest("FOOD", "Food"),
            CancellationToken.None);

        Assert.Equal("category.entitlement_denied", result.Error.Code);
    }

    [Fact]
    public async Task Create_EntitlementEvaluatorThrows_ReturnsUnexpectedFailure()
    {
        var result = await CreateService(
                new FakeCategoryRepository(),
                entitlementException: new InvalidOperationException("connection string leaked"))
            .CreateAsync(
                CreateContext([CategoryConstants.CreatePermission]),
                CreateRequest("FOOD", "Food"),
                CancellationToken.None);

        Assert.Equal("category.unexpected_failure", result.Error.Code);
        Assert.DoesNotContain("connection string", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Create_CancelledToken_PropagatesCancellation()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            CreateService(new FakeCategoryRepository()).CreateAsync(
                CreateContext([CategoryConstants.CreatePermission]),
                CreateRequest("FOOD", "Food"),
                cts.Token));
    }

    [Fact]
    public async Task Create_InvalidTenantContext_ReturnsInvalidTenant()
    {
        var result = await CreateService(new FakeCategoryRepository()).CreateAsync(
            new TenantRequestContext(Guid.Empty, UserId, [CategoryConstants.CreatePermission]),
            CreateRequest("FOOD", "Food"),
            CancellationToken.None);

        Assert.Equal("category.invalid_tenant_context", result.Error.Code);
    }

    [Fact]
    public async Task Update_UnchangedParentWhenExistingParentInactive_AllowsDescriptionEdit()
    {
        var parentId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var repository = new FakeCategoryRepository
        {
            EditableCategory = CreateCategory(categoryId, parentId)
        };

        var result = await CreateService(repository).UpdateAsync(
            CreateContext([CategoryConstants.UpdatePermission]),
            categoryId,
            new CategoryUpdateRequest("FOOD", "Food", "food", "Updated description", CategoryConstants.ActiveStatus, parentId, 2),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(repository.LastParentValidationCategoryId);
    }

    [Fact]
    public async Task Update_UnchangedParentWhenExistingParentInactive_AllowsNameEdit()
    {
        var parentId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var repository = new FakeCategoryRepository
        {
            EditableCategory = CreateCategory(categoryId, parentId)
        };

        var result = await CreateService(repository).UpdateAsync(
            CreateContext([CategoryConstants.UpdatePermission]),
            categoryId,
            new CategoryUpdateRequest("FOOD", "Renamed Food", "food", null, CategoryConstants.ActiveStatus, parentId, 1),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Update_UnchangedParentWhenExistingParentInactive_AllowsSortOrderEdit()
    {
        var parentId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var repository = new FakeCategoryRepository
        {
            EditableCategory = CreateCategory(categoryId, parentId)
        };

        var result = await CreateService(repository).UpdateAsync(
            CreateContext([CategoryConstants.UpdatePermission]),
            categoryId,
            new CategoryUpdateRequest("FOOD", "Food", "food", null, CategoryConstants.ActiveStatus, parentId, 9),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(9, repository.EditableCategory!.SortOrder);
    }

    [Fact]
    public async Task Update_ReparentToInactiveParent_ReturnsParentInactive()
    {
        var parentId = Guid.NewGuid();
        var inactiveParentId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var repository = new FakeCategoryRepository
        {
            EditableCategory = CreateCategory(categoryId, parentId),
            ParentInfo = new CategoryParentInfo(inactiveParentId, CategoryConstants.InactiveStatus, 1)
        };

        var result = await CreateService(repository).UpdateAsync(
            CreateContext([CategoryConstants.UpdatePermission]),
            categoryId,
            new CategoryUpdateRequest("FOOD", "Food", "food", null, CategoryConstants.ActiveStatus, inactiveParentId, 1),
            CancellationToken.None);

        Assert.Equal("category.parent_inactive", result.Error.Code);
    }

    [Fact]
    public async Task Update_MoveRootUnderInactiveParent_ReturnsParentInactive()
    {
        var inactiveParentId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var repository = new FakeCategoryRepository
        {
            EditableCategory = CreateCategory(categoryId),
            ParentInfo = new CategoryParentInfo(inactiveParentId, CategoryConstants.InactiveStatus, 1)
        };

        var result = await CreateService(repository).UpdateAsync(
            CreateContext([CategoryConstants.UpdatePermission]),
            categoryId,
            new CategoryUpdateRequest("FOOD", "Food", "food", null, CategoryConstants.ActiveStatus, inactiveParentId, 1),
            CancellationToken.None);

        Assert.Equal("category.parent_inactive", result.Error.Code);
    }

    [Fact]
    public async Task Update_SelfParent_ReturnsSelfReference()
    {
        var categoryId = Guid.NewGuid();
        var repository = new FakeCategoryRepository
        {
            EditableCategory = CreateCategory(categoryId)
        };

        var result = await CreateService(repository).UpdateAsync(
            CreateContext([CategoryConstants.UpdatePermission]),
            categoryId,
            new CategoryUpdateRequest("FOOD", "Food", "food", null, CategoryConstants.ActiveStatus, categoryId, 1),
            CancellationToken.None);

        Assert.Equal("category.parent_self_reference", result.Error.Code);
    }

    [Fact]
    public async Task Update_Cycle_ReturnsParentCycle()
    {
        var categoryId = Guid.NewGuid();
        var parentId = Guid.NewGuid();
        var repository = new FakeCategoryRepository
        {
            EditableCategory = CreateCategory(categoryId),
            ParentInfo = new CategoryParentInfo(parentId, CategoryConstants.ActiveStatus, 2),
            WouldCreateCycle = true
        };

        var result = await CreateService(repository).UpdateAsync(
            CreateContext([CategoryConstants.UpdatePermission]),
            categoryId,
            new CategoryUpdateRequest("FOOD", "Food", "food", null, CategoryConstants.ActiveStatus, parentId, 1),
            CancellationToken.None);

        Assert.Equal("category.parent_cycle", result.Error.Code);
    }

    [Fact]
    public async Task Update_ReparentSubtreeExceedingDepth_ReturnsMaxDepthExceeded()
    {
        var categoryId = Guid.NewGuid();
        var newParentId = Guid.NewGuid();
        var repository = new FakeCategoryRepository
        {
            EditableCategory = CreateCategory(categoryId),
            ParentInfo = new CategoryParentInfo(newParentId, CategoryConstants.ActiveStatus, 3),
            SubtreeRelativeDepth = 3
        };

        var result = await CreateService(repository).UpdateAsync(
            CreateContext([CategoryConstants.UpdatePermission]),
            categoryId,
            new CategoryUpdateRequest("FOOD", "Food", "food", null, CategoryConstants.ActiveStatus, newParentId, 1),
            CancellationToken.None);

        Assert.Equal("category.max_depth_exceeded", result.Error.Code);
    }

    [Fact]
    public async Task Update_DuplicateChecksExcludeOwnId()
    {
        var categoryId = Guid.NewGuid();
        var repository = new FakeCategoryRepository
        {
            EditableCategory = CreateCategory(categoryId)
        };

        var result = await CreateService(repository).UpdateAsync(
            CreateContext([CategoryConstants.UpdatePermission]),
            categoryId,
            new CategoryUpdateRequest("FOOD", "Food", "food", null, CategoryConstants.ActiveStatus, null, 1),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(categoryId, repository.LastCodeExcludeId);
        Assert.Equal(categoryId, repository.LastNameExcludeId);
    }

    [Fact]
    public async Task Delete_SoftDeletes()
    {
        var categoryId = Guid.NewGuid();
        var category = CreateCategory(categoryId);
        var repository = new FakeCategoryRepository { EditableCategory = category };

        var result = await CreateService(repository).DeleteAsync(
            CreateContext([CategoryConstants.DeletePermission]),
            categoryId,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(CategoryConstants.DeletedStatus, category.Status);
        Assert.Contains("category.archived", repository.AuditActions);
    }

    [Fact]
    public async Task Delete_BlockedByChild_ReturnsConflict()
    {
        var categoryId = Guid.NewGuid();
        var repository = new FakeCategoryRepository
        {
            EditableCategory = CreateCategory(categoryId),
            HasChildCategories = true
        };

        var result = await CreateService(repository).DeleteAsync(
            CreateContext([CategoryConstants.DeletePermission]),
            categoryId,
            CancellationToken.None);

        Assert.Equal("category.delete_conflict", result.Error.Code);
    }

    [Fact]
    public async Task Delete_BlockedByProductMapping_ReturnsConflict()
    {
        var categoryId = Guid.NewGuid();
        var repository = new FakeCategoryRepository
        {
            EditableCategory = CreateCategory(categoryId),
            HasProductLinks = true
        };

        var result = await CreateService(repository).DeleteAsync(
            CreateContext([CategoryConstants.DeletePermission]),
            categoryId,
            CancellationToken.None);

        Assert.Equal("category.delete_conflict", result.Error.Code);
    }

    [Fact]
    public async Task List_ViewPermission_Succeeds()
    {
        var result = await CreateService(new FakeCategoryRepository()).ListAsync(
            CreateContext([CategoryConstants.ViewPermission]),
            new CategoryListQuery(),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task List_ConflictingRootOnlyAndParent_ReturnsValidationFailed()
    {
        var result = await CreateService(new FakeCategoryRepository()).ListAsync(
            CreateContext([CategoryConstants.ViewPermission]),
            new CategoryListQuery(ParentCategoryId: Guid.NewGuid(), RootOnly: true),
            CancellationToken.None);

        Assert.Equal("category.validation_failed", result.Error.Code);
    }

    [Fact]
    public void NormalizeCode_TrimsAndUppercases()
    {
        Assert.Equal("ABC", CategoryConstants.NormalizeCode("abc"));
        Assert.Equal("ABC", CategoryConstants.NormalizeCode("ABC"));
        Assert.Equal("ABC", CategoryConstants.NormalizeCode(" ABC "));
    }

    [Fact]
    public void NormalizeNameForComparison_IsLowerTrimmed()
    {
        Assert.Equal("beverages", CategoryConstants.NormalizeNameForComparison("Beverages"));
        Assert.Equal("beverages", CategoryConstants.NormalizeNameForComparison(" beverages "));
        Assert.Equal("beverages", CategoryConstants.NormalizeNameForComparison("BEVERAGES"));
    }

    [Fact]
    public void Hierarchy_WouldExceedMaxDepth_MatchesCanonicalRule()
    {
        Assert.False(CategoryHierarchy.WouldExceedMaxDepth(4, 1));
        Assert.True(CategoryHierarchy.WouldExceedMaxDepth(3, 3));
        Assert.True(CategoryHierarchy.WouldExceedMaxDepth(5, 1));
    }

    private static CategoryService CreateService(
        FakeCategoryRepository repository,
        bool entitlementAllowed = true,
        Exception? entitlementException = null) =>
        new(
            repository,
            new CategoryRequestValidator(),
            new FakeDateTimeProvider(),
            new FakeEntitlementEvaluator(entitlementAllowed, entitlementException),
            repository);

    private static TenantRequestContext CreateContext(IReadOnlyCollection<string> permissions) =>
        new(TenantId, UserId, permissions);

    private static CategoryCreateRequest CreateRequest(string code, string name, Guid? parentId = null) =>
        new(code, name, null, null, CategoryConstants.ActiveStatus, parentId, 1);

    private static Category CreateCategory(Guid id, Guid? parentId = null) =>
        Category.Create(id, TenantId, parentId, "FOOD", "Food", "food", null, 1, CategoryConstants.ActiveStatus, UserId, Now);

    private sealed class FakeDateTimeProvider : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => Now;
    }

    private sealed class FakeEntitlementEvaluator(bool allowed, Exception? exception = null) : ITenantFeatureEntitlementEvaluator
    {
        public Task<TenantFeatureEntitlementEvaluation> EvaluateAsync(
            Guid tenantId,
            string featureCode,
            DateTimeOffset now,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (exception is not null)
            {
                throw exception;
            }

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

    private sealed class FakeCategoryRepository : ICategoryRepository, ICategoryAuditLogger
    {
        public bool CodeExists { get; init; }
        public bool NameExists { get; init; }
        public bool SlugExists { get; init; }
        public bool HasChildCategories { get; init; }
        public bool HasProductLinks { get; init; }
        public bool WouldCreateCycle { get; init; }
        public int SubtreeRelativeDepth { get; init; } = 1;
        public CategoryParentInfo? ParentInfo { get; init; }
        public Category? AddedCategory { get; private set; }
        public Guid? LastParentValidationCategoryId { get; private set; }

        public Category? EditableCategory { get; init; }
        public Guid? LastCodeExcludeId { get; private set; }
        public Guid? LastNameExcludeId { get; private set; }
        public List<string> AuditActions { get; } = [];
        public List<MediaAsset> AddedMediaAssets { get; } = [];
        public List<Guid> InactivatedMediaAssetIds { get; } = [];

        public Task<string?> GetTenantStatusAsync(Guid tenantId, CancellationToken cancellationToken) =>
            Task.FromResult<string?>(tenantId == OtherTenantId ? null : "active");

        public Task<bool> CategoryCodeExistsAsync(Guid tenantId, string categoryCode, Guid? excludeCategoryId, CancellationToken cancellationToken)
        {
            LastCodeExcludeId = excludeCategoryId;
            return Task.FromResult(CodeExists);
        }

        public Task<bool> CategoryNameExistsAsync(Guid tenantId, string categoryName, Guid? excludeCategoryId, CancellationToken cancellationToken)
        {
            LastNameExcludeId = excludeCategoryId;
            return Task.FromResult(NameExists);
        }

        public Task<bool> CategorySlugExistsAsync(Guid tenantId, string categorySlug, Guid? excludeCategoryId, CancellationToken cancellationToken) =>
            Task.FromResult(SlugExists);

        public Task<CategoryParentInfo?> GetParentInfoAsync(Guid tenantId, Guid parentCategoryId, CancellationToken cancellationToken)
        {
            LastParentValidationCategoryId = parentCategoryId;
            return Task.FromResult(ParentInfo);
        }

        public Task<bool> WouldCreateParentCycleAsync(Guid tenantId, Guid categoryId, Guid parentCategoryId, CancellationToken cancellationToken) =>
            Task.FromResult(WouldCreateCycle);

        public Task<int> GetSubtreeRelativeDepthAsync(Guid tenantId, Guid categoryId, CancellationToken cancellationToken) =>
            Task.FromResult(SubtreeRelativeDepth);

        public Task<bool> HasChildCategoriesAsync(Guid tenantId, Guid categoryId, CancellationToken cancellationToken) =>
            Task.FromResult(HasChildCategories);

        public Task<bool> HasProductLinksAsync(Guid tenantId, Guid categoryId, CancellationToken cancellationToken) =>
            Task.FromResult(HasProductLinks);

        public Task<CategoryListResponse> ListAsync(Guid tenantId, CategoryListQuery query, CancellationToken cancellationToken) =>
            Task.FromResult(new CategoryListResponse([], query.PageNumber, query.PageSize, 0));

        public Task<CategoryTreeResponse> GetTreeAsync(Guid tenantId, CancellationToken cancellationToken) =>
            Task.FromResult(new CategoryTreeResponse([]));

        public Task<CategoryResponse?> GetByIdAsync(Guid tenantId, Guid categoryId, bool includeDeleted, CancellationToken cancellationToken)
        {
            var category = AddedCategory ?? EditableCategory;
            if (category is null)
            {
                return Task.FromResult<CategoryResponse?>(null);
            }

            return Task.FromResult<CategoryResponse?>(new CategoryResponse(
                category.Id,
                category.ParentCategoryId,
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

        public void LogCreated(Guid tenantId, Guid actorTenantUserId, Guid categoryId, string categoryCode, string status) =>
            AuditActions.Add("category.created");

        public void LogUpdated(Guid tenantId, Guid actorTenantUserId, Guid categoryId, string categoryCode, string status, bool parentChanged, bool statusChanged) =>
            AuditActions.Add(parentChanged ? "category.parent_moved" : "category.updated");

        public void LogArchived(Guid tenantId, Guid actorTenantUserId, Guid categoryId, string categoryCode) =>
            AuditActions.Add("category.archived");

        public void LogImageUploaded(Guid tenantId, Guid actorTenantUserId, Guid categoryId, Guid mediaAssetId) =>
            AuditActions.Add("category.image_uploaded");

        public void LogImageRemoved(Guid tenantId, Guid actorTenantUserId, Guid categoryId, Guid? previousMediaAssetId, bool noOp) =>
            AuditActions.Add(noOp ? "category.image_removed_noop" : "category.image_removed");
    }
}
