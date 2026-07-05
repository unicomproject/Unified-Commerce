using System.Reflection;
using System.Security.Claims;
using E_POS.Api.Common;
using E_POS.Api.Controllers;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.CatalogProduct.Contracts;
using E_POS.Application.Modules.CatalogProduct.Dtos;
using E_POS.Domain.Modules.CatalogProduct.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace E_POS.ApiTests.CatalogProduct;

public sealed class DepartmentCategoryControllerTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 3, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task DepartmentCreate_WithTenantClaims_ReturnsCreatedAndPassesServerTenantContext()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var response = new DepartmentResponse(Guid.NewGuid(), "GROCERY", "Grocery", DepartmentConstants.ActiveStatus, Now, Now);
        var service = new FakeDepartmentService { CreateResult = ApplicationResult<DepartmentResponse>.Success(response) };
        var controller = CreateDepartmentController(service);
        SetTenantClaims(controller, tenantId, userId, DepartmentConstants.CreatePermission);

        var result = await controller.Create(new DepartmentCreateRequest("GROCERY", "Grocery", null, 0, DepartmentConstants.ActiveStatus), CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Same(response, created.Value);
        Assert.Equal(tenantId, service.Context?.TenantId);
        Assert.Equal(userId, service.Context?.UserId);
        Assert.Contains(DepartmentConstants.CreatePermission, service.Context!.Permissions);
    }

    [Fact]
    public async Task DepartmentList_WithoutTenantClaims_ReturnsUnauthorized()
    {
        var service = new FakeDepartmentService();
        var controller = CreateDepartmentController(service);

        var result = await controller.List(cancellationToken: CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Null(service.Context);
    }

    [Fact]
    public async Task DepartmentCreate_WhenDuplicate_ReturnsConflict()
    {
        var service = new FakeDepartmentService
        {
            CreateResult = ApplicationResult<DepartmentResponse>.Failure(new ApplicationError("department.duplicate_code", "Department code already exists."))
        };
        var controller = CreateDepartmentController(service);
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), DepartmentConstants.CreatePermission);

        var result = await controller.Create(new DepartmentCreateRequest("GROCERY", "Grocery", null, 0, DepartmentConstants.ActiveStatus), CancellationToken.None);

        var conflict = Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal(StatusCodes.Status409Conflict, conflict.StatusCode);
    }

    [Fact]
    public async Task CategoryCreate_WithTenantClaims_ReturnsCreatedAndPassesServerTenantContext()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var response = new CategoryResponse(Guid.NewGuid(), "FOOD", "Food", CategoryConstants.ActiveStatus, null, null, null, 1, Now, Now);
        var service = new FakeCategoryService { CreateResult = ApplicationResult<CategoryResponse>.Success(response) };
        var controller = CreateCategoryController(service);
        SetTenantClaims(controller, tenantId, userId, CategoryConstants.CreatePermission);

        var result = await controller.Create(new CategoryCreateRequest(Guid.Empty, "FOOD", "Food", "food", null, CategoryConstants.ActiveStatus, null, 1), CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Same(response, created.Value);
        Assert.Equal(tenantId, service.Context?.TenantId);
        Assert.Equal(userId, service.Context?.UserId);
        Assert.Contains(CategoryConstants.CreatePermission, service.Context!.Permissions);
    }

    [Fact]
    public async Task CategoryCreate_WhenParentMissing_ReturnsNotFound()
    {
        var service = new FakeCategoryService
        {
            CreateResult = ApplicationResult<CategoryResponse>.Failure(new ApplicationError("category.parent_not_found", "Parent category was not found."))
        };
        var controller = CreateCategoryController(service);
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), CategoryConstants.CreatePermission);

        var result = await controller.Create(new CategoryCreateRequest(Guid.Empty, "MILK", "Milk", "milk", null, CategoryConstants.ActiveStatus, Guid.NewGuid(), 1), CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public void DepartmentsController_RequiresTenantOnlyPolicy()
    {
        var authorize = Assert.Single(typeof(DepartmentsController).GetCustomAttributes<AuthorizeAttribute>());
        Assert.Equal("TenantOnly", authorize.Policy);
    }

    [Fact]
    public void CategoriesController_RequiresTenantOnlyPolicy()
    {
        var authorize = Assert.Single(typeof(CategoriesController).GetCustomAttributes<AuthorizeAttribute>());
        Assert.Equal("TenantOnly", authorize.Policy);
    }

    private static DepartmentsController CreateDepartmentController(FakeDepartmentService service)
    {
        var controller = new DepartmentsController(service, new TenantRequestContextFactory());
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        return controller;
    }

    private static CategoriesController CreateCategoryController(FakeCategoryService service)
    {
        var controller = new CategoriesController(service, new TenantRequestContextFactory());
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        return controller;
    }

    private static void SetTenantClaims(ControllerBase controller, Guid tenantId, Guid userId, string permission)
    {
        controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim("sub", userId.ToString()),
                new Claim("tenant_id", tenantId.ToString()),
                new Claim("permissions", permission)
            ],
            "Test"));
    }

    private sealed class FakeDepartmentService : IDepartmentService
    {
        public TenantRequestContext? Context { get; private set; }
        public ApplicationResult<DepartmentResponse> CreateResult { get; init; } = ApplicationResult<DepartmentResponse>.Failure(new ApplicationError("department.not_configured", "Not configured."));

        public Task<ApplicationResult<DepartmentResponse>> CreateAsync(TenantRequestContext context, DepartmentCreateRequest request, CancellationToken cancellationToken)
        {
            Context = context;
            return Task.FromResult(CreateResult);
        }

        public Task<ApplicationResult<DepartmentListResponse>> ListAsync(TenantRequestContext context, int pageNumber, int pageSize, string? search, CancellationToken cancellationToken)
        {
            Context = context;
            return Task.FromResult(ApplicationResult<DepartmentListResponse>.Success(new DepartmentListResponse([], pageNumber, pageSize, 0)));
        }

        public Task<ApplicationResult<DepartmentResponse>> GetByIdAsync(TenantRequestContext context, Guid departmentId, CancellationToken cancellationToken)
        {
            Context = context;
            return Task.FromResult(CreateResult);
        }

        public Task<ApplicationResult<DepartmentResponse>> UpdateAsync(TenantRequestContext context, Guid departmentId, DepartmentUpdateRequest request, CancellationToken cancellationToken)
        {
            Context = context;
            return Task.FromResult(CreateResult);
        }

        public Task<ApplicationResult> DeleteAsync(TenantRequestContext context, Guid departmentId, CancellationToken cancellationToken)
        {
            Context = context;
            return Task.FromResult(ApplicationResult.Success());
        }
    }

    private sealed class FakeCategoryService : ICategoryService
    {
        public TenantRequestContext? Context { get; private set; }
        public ApplicationResult<CategoryResponse> CreateResult { get; init; } = ApplicationResult<CategoryResponse>.Failure(new ApplicationError("category.not_configured", "Not configured."));

        public Task<ApplicationResult<CategoryResponse>> CreateAsync(TenantRequestContext context, CategoryCreateRequest request, CancellationToken cancellationToken)
        {
            Context = context;
            return Task.FromResult(CreateResult);
        }

        public Task<ApplicationResult<CategoryListResponse>> ListAsync(TenantRequestContext context, int pageNumber, int pageSize, string? search, CancellationToken cancellationToken)
        {
            Context = context;
            return Task.FromResult(ApplicationResult<CategoryListResponse>.Success(new CategoryListResponse([], pageNumber, pageSize, 0)));
        }

        public Task<ApplicationResult<CategoryResponse>> GetByIdAsync(TenantRequestContext context, Guid categoryId, CancellationToken cancellationToken)
        {
            Context = context;
            return Task.FromResult(CreateResult);
        }

        public Task<ApplicationResult<CategoryResponse>> UpdateAsync(TenantRequestContext context, Guid categoryId, CategoryUpdateRequest request, CancellationToken cancellationToken)
        {
            Context = context;
            return Task.FromResult(CreateResult);
        }

        public Task<ApplicationResult> DeleteAsync(TenantRequestContext context, Guid categoryId, CancellationToken cancellationToken)
        {
            Context = context;
            return Task.FromResult(ApplicationResult.Success());
        }
    }
}
