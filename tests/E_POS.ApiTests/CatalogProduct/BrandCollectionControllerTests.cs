using System.Reflection;
using System.Security.Claims;
using E_POS.Api.Common;
using E_POS.Api.Controllers;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.CatalogProduct.Contracts;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace E_POS.ApiTests.CatalogProduct;

public sealed class BrandCollectionControllerTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 3, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task BrandCreate_WithTenantClaims_ReturnsCreatedAndPassesServerTenantContext()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var response = new BrandResponse(Guid.NewGuid(), "ACME", "Acme", BrandConstants.ActiveStatus, Now, Now);
        var service = new FakeBrandService { CreateResult = ApplicationResult<BrandResponse>.Success(response) };
        var controller = CreateBrandController(service);
        SetTenantClaims(controller, tenantId, userId, BrandConstants.CreatePermission);

        var result = await controller.Create(new BrandCreateRequest("ACME", "Acme", null, null, null, BrandConstants.ActiveStatus), CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Same(response, created.Value);
        Assert.Equal(tenantId, service.Context?.TenantId);
        Assert.Equal(userId, service.Context?.UserId);
        Assert.Contains(BrandConstants.CreatePermission, service.Context!.Permissions);
    }

    [Fact]
    public async Task BrandCreate_WhenDuplicate_ReturnsConflict()
    {
        var service = new FakeBrandService
        {
            CreateResult = ApplicationResult<BrandResponse>.Failure(new ApplicationError("brand.duplicate_code", "Brand code already exists."))
        };
        var controller = CreateBrandController(service);
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), BrandConstants.CreatePermission);

        var result = await controller.Create(new BrandCreateRequest("ACME", "Acme", null, null, null, BrandConstants.ActiveStatus), CancellationToken.None);

        Assert.IsType<ConflictObjectResult>(result);
    }

    [Fact]
    public async Task BrandList_WithoutTenantClaims_ReturnsUnauthorized()
    {
        var service = new FakeBrandService();
        var controller = CreateBrandController(service);

        var result = await controller.List(cancellationToken: CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Null(service.Context);
    }

    [Fact]
    public async Task CollectionCreate_WithTenantClaims_ReturnsCreatedAndPassesServerTenantContext()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var response = new CollectionResponse(Guid.NewGuid(), "SUMMER", "Summer", CollectionConstants.ActiveStatus, Now, Now);
        var service = new FakeCollectionService { CreateResult = ApplicationResult<CollectionResponse>.Success(response) };
        var controller = CreateCollectionController(service);
        SetTenantClaims(controller, tenantId, userId, CollectionConstants.CreatePermission);

        var result = await controller.Create(new CollectionCreateRequest("SUMMER", "Summer", null, null, "STANDARD", null, null, 0, CollectionConstants.ActiveStatus), CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Same(response, created.Value);
        Assert.Equal(tenantId, service.Context?.TenantId);
        Assert.Equal(userId, service.Context?.UserId);
        Assert.Contains(CollectionConstants.CreatePermission, service.Context!.Permissions);
    }

    [Fact]
    public async Task CollectionDelete_WhenProductsLinked_ReturnsConflict()
    {
        var service = new FakeCollectionService
        {
            DeleteResult = ApplicationResult.Failure(new ApplicationError("collection.delete_conflict", "Collection cannot be deleted while products are linked."))
        };
        var controller = CreateCollectionController(service);
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), CollectionConstants.DeletePermission);

        var result = await controller.Delete(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<ConflictObjectResult>(result);
    }

    [Fact]
    public void BrandsController_RequiresTenantOnlyPolicy()
    {
        var authorize = Assert.Single(typeof(BrandsController).GetCustomAttributes<AuthorizeAttribute>());
        Assert.Equal("TenantOnly", authorize.Policy);
    }

    [Fact]
    public void CollectionsController_RequiresTenantOnlyPolicy()
    {
        var authorize = Assert.Single(typeof(CollectionsController).GetCustomAttributes<AuthorizeAttribute>());
        Assert.Equal("TenantOnly", authorize.Policy);
    }

    private static BrandsController CreateBrandController(FakeBrandService service)
    {
        var controller = new BrandsController(service, new TenantRequestContextFactory());
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        return controller;
    }

    private static CollectionsController CreateCollectionController(FakeCollectionService service)
    {
        var controller = new CollectionsController(service, new TenantRequestContextFactory());
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

    private sealed class FakeBrandService : IBrandService
    {
        public TenantRequestContext? Context { get; private set; }
        public ApplicationResult<BrandResponse> CreateResult { get; init; } = ApplicationResult<BrandResponse>.Failure(new ApplicationError("brand.not_configured", "Not configured."));

        public Task<ApplicationResult<BrandResponse>> CreateAsync(TenantRequestContext context, BrandCreateRequest request, CancellationToken cancellationToken)
        {
            Context = context;
            return Task.FromResult(CreateResult);
        }

        public Task<ApplicationResult<BrandListResponse>> ListAsync(TenantRequestContext context, int pageNumber, int pageSize, string? search, CancellationToken cancellationToken)
        {
            Context = context;
            return Task.FromResult(ApplicationResult<BrandListResponse>.Success(new BrandListResponse([], pageNumber, pageSize, 0)));
        }

        public Task<ApplicationResult<BrandResponse>> GetByIdAsync(TenantRequestContext context, Guid brandId, CancellationToken cancellationToken)
        {
            Context = context;
            return Task.FromResult(CreateResult);
        }

        public Task<ApplicationResult<BrandResponse>> UpdateAsync(TenantRequestContext context, Guid brandId, BrandUpdateRequest request, CancellationToken cancellationToken)
        {
            Context = context;
            return Task.FromResult(CreateResult);
        }

        public Task<ApplicationResult> DeleteAsync(TenantRequestContext context, Guid brandId, CancellationToken cancellationToken)
        {
            Context = context;
            return Task.FromResult(ApplicationResult.Success());
        }
    }

    private sealed class FakeCollectionService : ICollectionService
    {
        public TenantRequestContext? Context { get; private set; }
        public ApplicationResult<CollectionResponse> CreateResult { get; init; } = ApplicationResult<CollectionResponse>.Failure(new ApplicationError("collection.not_configured", "Not configured."));
        public ApplicationResult DeleteResult { get; init; } = ApplicationResult.Success();

        public Task<ApplicationResult<CollectionResponse>> CreateAsync(TenantRequestContext context, CollectionCreateRequest request, CancellationToken cancellationToken)
        {
            Context = context;
            return Task.FromResult(CreateResult);
        }

        public Task<ApplicationResult<CollectionListResponse>> ListAsync(TenantRequestContext context, int pageNumber, int pageSize, string? search, CancellationToken cancellationToken)
        {
            Context = context;
            return Task.FromResult(ApplicationResult<CollectionListResponse>.Success(new CollectionListResponse([], pageNumber, pageSize, 0)));
        }

        public Task<ApplicationResult<CollectionResponse>> GetByIdAsync(TenantRequestContext context, Guid collectionId, CancellationToken cancellationToken)
        {
            Context = context;
            return Task.FromResult(CreateResult);
        }

        public Task<ApplicationResult<CollectionResponse>> UpdateAsync(TenantRequestContext context, Guid collectionId, CollectionUpdateRequest request, CancellationToken cancellationToken)
        {
            Context = context;
            return Task.FromResult(CreateResult);
        }

        public Task<ApplicationResult> DeleteAsync(TenantRequestContext context, Guid collectionId, CancellationToken cancellationToken)
        {
            Context = context;
            return Task.FromResult(DeleteResult);
        }
    }
}

