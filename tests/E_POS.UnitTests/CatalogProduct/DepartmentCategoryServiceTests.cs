using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.CatalogProduct.Contracts;
using E_POS.Application.Modules.CatalogProduct.Dtos;
using E_POS.Application.Modules.CatalogProduct.Services;
using E_POS.Application.Modules.CatalogProduct.Validators;
using E_POS.Domain.Modules.CatalogProduct.Constants;
using E_POS.Domain.Modules.CatalogProduct.Entities;
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
            new DepartmentCreateRequest("GROCERY", "Grocery", DepartmentConstants.ActiveStatus),
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
            new DepartmentCreateRequest(" grocery ", "Grocery", DepartmentConstants.ActiveStatus),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("GROCERY", repository.AddedDepartment?.DepartmentCode);
        Assert.Equal(TenantId, repository.AddedDepartment?.TenantId);
    }

    [Fact]
    public async Task CategoryCreateAsync_WithRootCategory_PersistsWithoutParent()
    {
        var repository = new FakeCategoryRepository();
        var service = new CategoryService(repository, new CategoryRequestValidator(), new FakeDateTimeProvider());

        var result = await service.CreateAsync(
            CreateContext([CategoryConstants.CreatePermission]),
            new CategoryCreateRequest("FOOD", "Food", CategoryConstants.ActiveStatus, null, 1),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(repository.AddedCategory?.ParentCategoryId);
        Assert.Equal("FOOD", repository.AddedCategory?.CategoryCode);
    }

    [Fact]
    public async Task CategoryCreateAsync_WithMissingParent_ReturnsParentNotFound()
    {
        var repository = new FakeCategoryRepository { ParentExists = false };
        var service = new CategoryService(repository, new CategoryRequestValidator(), new FakeDateTimeProvider());

        var result = await service.CreateAsync(
            CreateContext([CategoryConstants.CreatePermission]),
            new CategoryCreateRequest("MILK", "Milk", CategoryConstants.ActiveStatus, Guid.NewGuid(), 1),
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
            EditableCategory = Category.Create(categoryId, TenantId, "FOOD", "Food", CategoryConstants.ActiveStatus, null, 1, Now)
        };
        var service = new CategoryService(repository, new CategoryRequestValidator(), new FakeDateTimeProvider());

        var result = await service.UpdateAsync(
            CreateContext([CategoryConstants.UpdatePermission]),
            categoryId,
            new CategoryUpdateRequest("FOOD", "Food", CategoryConstants.ActiveStatus, categoryId, 1),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("category.parent_self_reference", result.Error.Code);
    }

    [Fact]
    public async Task CategoryDeleteAsync_WhenChildCategoriesExist_ReturnsConflict()
    {
        var categoryId = Guid.NewGuid();
        var repository = new FakeCategoryRepository
        {
            EditableCategory = Category.Create(categoryId, TenantId, "FOOD", "Food", CategoryConstants.ActiveStatus, null, 1, Now),
            HasChildCategories = true
        };
        var service = new CategoryService(repository, new CategoryRequestValidator(), new FakeDateTimeProvider());

        var result = await service.DeleteAsync(CreateContext([CategoryConstants.DeletePermission]), categoryId, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("category.delete_conflict", result.Error.Code);
    }

    private static TenantRequestContext CreateContext(IReadOnlyCollection<string> permissions)
    {
        return new TenantRequestContext(TenantId, UserId, permissions);
    }

    private sealed class FakeDateTimeProvider : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => Now;
    }

    private sealed class FakeDepartmentRepository : IDepartmentRepository
    {
        public Department? AddedDepartment { get; private set; }

        public Task<bool> DepartmentCodeExistsAsync(Guid tenantId, string departmentCode, Guid? excludeDepartmentId, CancellationToken cancellationToken)
        {
            return Task.FromResult(false);
        }

        public Task<DepartmentListResponse> ListAsync(Guid tenantId, int pageNumber, int pageSize, string? search, CancellationToken cancellationToken)
        {
            return Task.FromResult(new DepartmentListResponse([], pageNumber, pageSize, 0));
        }

        public Task<DepartmentResponse?> GetByIdAsync(Guid tenantId, Guid departmentId, bool includeDeleted, CancellationToken cancellationToken)
        {
            return Task.FromResult<DepartmentResponse?>(new DepartmentResponse(departmentId, AddedDepartment!.DepartmentCode, AddedDepartment.Name, AddedDepartment.Status, AddedDepartment.CreatedAt, AddedDepartment.UpdatedAt));
        }

        public Task<Department?> GetEditableAsync(Guid tenantId, Guid departmentId, CancellationToken cancellationToken)
        {
            return Task.FromResult<Department?>(AddedDepartment);
        }

        public Task AddAsync(Department department, CancellationToken cancellationToken)
        {
            AddedDepartment = department;
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class FakeCategoryRepository : ICategoryRepository
    {
        public bool ParentExists { get; init; } = true;
        public bool HasChildCategories { get; init; }
        public Category? AddedCategory { get; private set; }
        public Category? EditableCategory { get; init; }

        public Task<bool> CategoryCodeExistsAsync(Guid tenantId, string categoryCode, Guid? excludeCategoryId, CancellationToken cancellationToken)
        {
            return Task.FromResult(false);
        }

        public Task<bool> ParentCategoryExistsAsync(Guid tenantId, Guid parentCategoryId, CancellationToken cancellationToken)
        {
            return Task.FromResult(ParentExists);
        }

        public Task<bool> WouldCreateParentCycleAsync(Guid tenantId, Guid categoryId, Guid parentCategoryId, CancellationToken cancellationToken)
        {
            return Task.FromResult(false);
        }

        public Task<bool> HasChildCategoriesAsync(Guid tenantId, Guid categoryId, CancellationToken cancellationToken)
        {
            return Task.FromResult(HasChildCategories);
        }

        public Task<bool> HasProductLinksAsync(Guid tenantId, Guid categoryId, CancellationToken cancellationToken)
        {
            return Task.FromResult(false);
        }

        public Task<CategoryListResponse> ListAsync(Guid tenantId, int pageNumber, int pageSize, string? search, CancellationToken cancellationToken)
        {
            return Task.FromResult(new CategoryListResponse([], pageNumber, pageSize, 0));
        }

        public Task<CategoryResponse?> GetByIdAsync(Guid tenantId, Guid categoryId, bool includeDeleted, CancellationToken cancellationToken)
        {
            var category = AddedCategory ?? EditableCategory;
            return Task.FromResult<CategoryResponse?>(new CategoryResponse(categoryId, category!.CategoryCode, category.Name, category.Status, category.ParentCategoryId, null, null, category.SortOrder, category.CreatedAt, category.UpdatedAt));
        }

        public Task<Category?> GetEditableAsync(Guid tenantId, Guid categoryId, CancellationToken cancellationToken)
        {
            return Task.FromResult(EditableCategory);
        }

        public Task AddAsync(Category category, CancellationToken cancellationToken)
        {
            AddedCategory = category;
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
