using System.Reflection;
using System.Security.Claims;
using E_POS.Api.Common;
using E_POS.Api.Controllers.V1.Tenant.CatalogProduct;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.CatalogProduct.Contracts;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.TenantAdmin;
using E_POS.Application.Modules.Tenant.Inventory.Contracts;
using E_POS.Application.Modules.Tenant.Inventory.Dtos.TenantAdmin;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace E_POS.ApiTests.CatalogProduct;

public sealed class TenantAdminProductsControllerTests
{
    [Fact]
    public async Task GetSummary_WithTenantProductsView_ReturnsOk()
    {
        var summary = new TenantAdminProductSummaryCardsResponse(5, 4, 1, 2);
        var service = new FakeTenantAdminProductService
        {
            SummaryResult = ApplicationResult<TenantAdminProductSummaryCardsResponse>.Success(summary),
        };
        var controller = CreateController(service);
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), TenantAdminProductPermissions.View);

        var result = await controller.GetSummary(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
    }

    [Fact]
    public async Task GetSummary_WithoutTenantClaims_ReturnsUnauthorized()
    {
        var service = new FakeTenantAdminProductService();
        var controller = CreateController(service);

        var result = await controller.GetSummary(CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task GetSummary_WithPermissionDenied_ReturnsForbidden()
    {
        var service = new FakeTenantAdminProductService
        {
            SummaryResult = ApplicationResult<TenantAdminProductSummaryCardsResponse>.Failure(
                new ApplicationError("product.permission_denied", "Permission denied for product management.")),
        };
        var controller = CreateController(service);
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), "catalog.products.create");

        var result = await controller.GetSummary(CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, objectResult.StatusCode);
    }

    [Fact]
    public async Task GetCreateOptions_WithTenantProductsCreate_ReturnsOk()
    {
        var options = new TenantAdminProductCreateOptionsResponse(
            [],
            [],
            [],
            [],
            [],
            [],
            []);
        var service = new FakeTenantAdminProductService
        {
            CreateOptionsResult = ApplicationResult<TenantAdminProductCreateOptionsResponse>.Success(options),
        };
        var controller = CreateController(service);
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), TenantAdminProductPermissions.Create);

        var result = await controller.GetCreateOptions(CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetCreateOptions_WithoutTenantClaims_ReturnsUnauthorized()
    {
        var service = new FakeTenantAdminProductService();
        var controller = CreateController(service);

        var result = await controller.GetCreateOptions(CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task GetCreateOptions_WithPermissionDenied_ReturnsForbidden()
    {
        var service = new FakeTenantAdminProductService
        {
            CreateOptionsResult = ApplicationResult<TenantAdminProductCreateOptionsResponse>.Failure(
                new ApplicationError("product.permission_denied", "Permission denied for product management.")),
        };
        var controller = CreateController(service);
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), TenantAdminProductPermissions.View);

        var result = await controller.GetCreateOptions(CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, objectResult.StatusCode);
    }

    [Fact]
    public void Controller_RequiresTenantOnlyPolicy()
    {
        var authorize = Assert.Single(
            typeof(TenantAdminProductsController).GetCustomAttributes<AuthorizeAttribute>());
        Assert.Equal("TenantOnly", authorize.Policy);
    }

    [Fact]
    public async Task Create_WithTenantProductsCreate_ReturnsCreated()
    {
        var productId = Guid.NewGuid();
        var service = new FakeTenantAdminProductService
        {
            CreateResult = ApplicationResult<TenantAdminProductCreateResponse>.Success(
                new TenantAdminProductCreateResponse(
                    productId,
                    "Sample Product",
                    "SKU-001",
                    "ACTIVE")),
        };
        var controller = CreateController(service);
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), TenantAdminProductPermissions.Create);

        var result = await controller.Create(CreateRequest(), CancellationToken.None);

        var created = Assert.IsType<CreatedResult>(result);
        Assert.Equal(StatusCodes.Status201Created, created.StatusCode);
        Assert.Equal($"/api/v1/tenant-admin/products/{productId}", created.Location);
    }

    [Fact]
    public async Task Create_WithoutTenantClaims_ReturnsUnauthorized()
    {
        var service = new FakeTenantAdminProductService();
        var controller = CreateController(service);

        var result = await controller.Create(CreateRequest(), CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task Create_WithPermissionDenied_ReturnsForbidden()
    {
        var service = new FakeTenantAdminProductService
        {
            CreateResult = ApplicationResult<TenantAdminProductCreateResponse>.Failure(
                new ApplicationError("product.permission_denied", "Permission denied for product management.")),
        };
        var controller = CreateController(service);
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), TenantAdminProductPermissions.View);

        var result = await controller.Create(CreateRequest(), CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, objectResult.StatusCode);
    }

    [Fact]
    public async Task Create_WithValidationFailed_ReturnsBadRequest()
    {
        var service = new FakeTenantAdminProductService
        {
            CreateResult = ApplicationResult<TenantAdminProductCreateResponse>.Failure(
                new ApplicationError(
                    "product.validation_failed",
                    "Product validation failed.",
                    [new ApplicationFieldError("productName", "Product name is required.")])),
        };
        var controller = CreateController(service);
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), TenantAdminProductPermissions.Create);

        var result = await controller.Create(CreateRequest(), CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequest.StatusCode);
    }

    [Fact]
    public async Task Create_WithDuplicateSku_ReturnsConflict()
    {
        var service = new FakeTenantAdminProductService
        {
            CreateResult = ApplicationResult<TenantAdminProductCreateResponse>.Failure(
                new ApplicationError("product.duplicate_sku", "SKU already exists.")),
        };
        var controller = CreateController(service);
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), TenantAdminProductPermissions.Create);

        var result = await controller.Create(CreateRequest(), CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status409Conflict, objectResult.StatusCode);
    }

    [Fact]
    public async Task GetById_WithTenantProductsView_ReturnsOk()
    {
        var productId = Guid.NewGuid();
        var detail = CreateDetailResponse(productId);
        var service = new FakeTenantAdminProductService
        {
            DetailResult = ApplicationResult<TenantAdminProductDetailResponse>.Success(detail),
        };
        var controller = CreateController(service);
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), TenantAdminProductPermissions.View);

        var result = await controller.GetById(productId, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetById_WithTenantProductsDetailsView_ReturnsOk()
    {
        var productId = Guid.NewGuid();
        var service = new FakeTenantAdminProductService
        {
            DetailResult = ApplicationResult<TenantAdminProductDetailResponse>.Success(
                CreateDetailResponse(productId)),
        };
        var controller = CreateController(service);
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), TenantAdminProductPermissions.DetailsView);

        var result = await controller.GetById(productId, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetById_WithoutTenantClaims_ReturnsUnauthorized()
    {
        var service = new FakeTenantAdminProductService();
        var controller = CreateController(service);

        var result = await controller.GetById(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task GetById_WithPermissionDenied_ReturnsForbidden()
    {
        var service = new FakeTenantAdminProductService
        {
            DetailResult = ApplicationResult<TenantAdminProductDetailResponse>.Failure(
                new ApplicationError("product.permission_denied", "Permission denied for product management.")),
        };
        var controller = CreateController(service);
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), TenantAdminProductPermissions.Create);

        var result = await controller.GetById(Guid.NewGuid(), CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, objectResult.StatusCode);
    }

    [Fact]
    public async Task GetById_WhenProductNotFound_ReturnsNotFound()
    {
        var service = new FakeTenantAdminProductService
        {
            DetailResult = ApplicationResult<TenantAdminProductDetailResponse>.Failure(
                new ApplicationError("product.not_found", "Product was not found.")),
        };
        var controller = CreateController(service);
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), TenantAdminProductPermissions.View);

        var result = await controller.GetById(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Update_WithTenantProductsUpdate_ReturnsOk()
    {
        var productId = Guid.NewGuid();
        var service = new FakeTenantAdminProductService
        {
            UpdateResult = ApplicationResult<TenantAdminProductDetailResponse>.Success(
                CreateDetailResponse(productId)),
        };
        var controller = CreateController(service);
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), TenantAdminProductPermissions.Update);

        var result = await controller.Update(productId, CreateRequest(), CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Update_WithoutTenantClaims_ReturnsUnauthorized()
    {
        var service = new FakeTenantAdminProductService();
        var controller = CreateController(service);

        var result = await controller.Update(Guid.NewGuid(), CreateRequest(), CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task Update_WithPermissionDenied_ReturnsForbidden()
    {
        var service = new FakeTenantAdminProductService
        {
            UpdateResult = ApplicationResult<TenantAdminProductDetailResponse>.Failure(
                new ApplicationError("product.permission_denied", "Permission denied for product management.")),
        };
        var controller = CreateController(service);
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), TenantAdminProductPermissions.View);

        var result = await controller.Update(Guid.NewGuid(), CreateRequest(), CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, objectResult.StatusCode);
    }

    [Fact]
    public async Task Update_WithValidationFailed_ReturnsBadRequest()
    {
        var service = new FakeTenantAdminProductService
        {
            UpdateResult = ApplicationResult<TenantAdminProductDetailResponse>.Failure(
                new ApplicationError(
                    "product.validation_failed",
                    "Product validation failed.",
                    [new ApplicationFieldError("productName", "Product name is required.")])),
        };
        var controller = CreateController(service);
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), TenantAdminProductPermissions.Update);

        var result = await controller.Update(Guid.NewGuid(), CreateRequest(), CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Update_WhenProductNotFound_ReturnsNotFound()
    {
        var service = new FakeTenantAdminProductService
        {
            UpdateResult = ApplicationResult<TenantAdminProductDetailResponse>.Failure(
                new ApplicationError("product.not_found", "Product was not found.")),
        };
        var controller = CreateController(service);
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), TenantAdminProductPermissions.Update);

        var result = await controller.Update(Guid.NewGuid(), CreateRequest(), CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task UpdateStatus_WithTenantProductsUpdate_ReturnsOk()
    {
        var productId = Guid.NewGuid();
        var service = new FakeTenantAdminProductService
        {
            StatusUpdateResult = ApplicationResult<TenantAdminProductStatusUpdateResponse>.Success(
                new TenantAdminProductStatusUpdateResponse(productId, "INACTIVE")),
        };
        var controller = CreateController(service);
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), TenantAdminProductPermissions.Update);

        var result = await controller.UpdateStatus(
            productId,
            new TenantAdminProductStatusUpdateRequest { Status = "Inactive" },
            CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task UpdateStatus_WithInvalidStatus_ReturnsBadRequest()
    {
        var service = new FakeTenantAdminProductService
        {
            StatusUpdateResult = ApplicationResult<TenantAdminProductStatusUpdateResponse>.Failure(
                new ApplicationError(
                    "product.validation_failed",
                    "Product validation failed.",
                    [new ApplicationFieldError("status", "Status must be Active or Inactive.")])),
        };
        var controller = CreateController(service);
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), TenantAdminProductPermissions.Update);

        var result = await controller.UpdateStatus(
            Guid.NewGuid(),
            new TenantAdminProductStatusUpdateRequest { Status = "Deleted" },
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task UpdateStatus_WithPermissionDenied_ReturnsForbidden()
    {
        var service = new FakeTenantAdminProductService
        {
            StatusUpdateResult = ApplicationResult<TenantAdminProductStatusUpdateResponse>.Failure(
                new ApplicationError("product.permission_denied", "Permission denied for product management.")),
        };
        var controller = CreateController(service);
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), TenantAdminProductPermissions.View);

        var result = await controller.UpdateStatus(
            Guid.NewGuid(),
            new TenantAdminProductStatusUpdateRequest { Status = "Inactive" },
            CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, objectResult.StatusCode);
    }

    [Fact]
    public async Task UpdateStatus_WhenProductNotFound_ReturnsNotFound()
    {
        var service = new FakeTenantAdminProductService
        {
            StatusUpdateResult = ApplicationResult<TenantAdminProductStatusUpdateResponse>.Failure(
                new ApplicationError("product.not_found", "Product was not found.")),
        };
        var controller = CreateController(service);
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), TenantAdminProductPermissions.Update);

        var result = await controller.UpdateStatus(
            Guid.NewGuid(),
            new TenantAdminProductStatusUpdateRequest { Status = "Inactive" },
            CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Delete_WithTenantProductsDelete_ReturnsOk()
    {
        var productId = Guid.NewGuid();
        var service = new FakeTenantAdminProductService
        {
            DeleteResult = ApplicationResult<TenantAdminProductDeleteResponse>.Success(
                new TenantAdminProductDeleteResponse(productId, "Archived", "INACTIVE")),
        };
        var controller = CreateController(service);
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), TenantAdminProductPermissions.Delete);

        var result = await controller.Delete(productId, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Delete_WithPermissionDenied_ReturnsForbidden()
    {
        var service = new FakeTenantAdminProductService
        {
            DeleteResult = ApplicationResult<TenantAdminProductDeleteResponse>.Failure(
                new ApplicationError("product.permission_denied", "Permission denied for product management.")),
        };
        var controller = CreateController(service);
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), TenantAdminProductPermissions.View);

        var result = await controller.Delete(Guid.NewGuid(), CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, objectResult.StatusCode);
    }

    [Fact]
    public async Task Delete_WhenAlreadyDeleted_ReturnsBadRequest()
    {
        var service = new FakeTenantAdminProductService
        {
            DeleteResult = ApplicationResult<TenantAdminProductDeleteResponse>.Failure(
                new ApplicationError("product.delete_blocked", "Product is already deleted.")),
        };
        var controller = CreateController(service);
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), TenantAdminProductPermissions.Delete);

        var result = await controller.Delete(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Delete_WhenProductNotFound_ReturnsNotFound()
    {
        var service = new FakeTenantAdminProductService
        {
            DeleteResult = ApplicationResult<TenantAdminProductDeleteResponse>.Failure(
                new ApplicationError("product.not_found", "Product was not found.")),
        };
        var controller = CreateController(service);
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), TenantAdminProductPermissions.Delete);

        var result = await controller.Delete(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    private static TenantAdminProductDetailResponse CreateDetailResponse(Guid productId) =>
        new(
            productId,
            "Sample Product",
            "SKU-001",
            null,
            Guid.NewGuid(),
            "Beverages",
            null,
            null,
            "PIECE",
            null,
            null,
            [],
            null,
            10m,
            null,
            null,
            null,
            "ACTIVE",
            false,
            null,
            [],
            [],
            null,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow);

    private static TenantAdminProductCreateRequest CreateRequest() =>
        new()
        {
            ProductName = "Sample Product",
            Sku = "SKU-001",
            CategoryId = Guid.NewGuid(),
            UnitType = "PIECE",
            SellingPrice = 10m,
            Status = "ACTIVE",
        };

    private static TenantAdminProductsController CreateController(FakeTenantAdminProductService service)
    {
        var controller = new TenantAdminProductsController(
            service,
            new FakeTenantAdminInventoryService(),
            new FakeTenantRequestContextFactory());
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext(),
        };
        return controller;
    }

    private static void SetTenantClaims(
        TenantAdminProductsController controller,
        Guid tenantId,
        Guid userId,
        string permission)
    {
        controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim("sub", userId.ToString()),
                new Claim("tenant_id", tenantId.ToString()),
                new Claim("permissions", permission),
            ],
            "Test"));
    }

    [Fact]
    public async Task GetDashboard_WithDashboardPermission_ReturnsOk()
    {
        var dashboard = new TenantAdminProductDashboardResponse(
            DateTimeOffset.UtcNow,
            "USD",
            new TenantAdminProductDashboardSummaryDto(
                new TenantAdminProductDashboardMetricDto(5, 0),
                null,
                null,
                null,
                null,
                null),
            null,
            null);
        var service = new FakeTenantAdminProductService
        {
            DashboardResult = ApplicationResult<TenantAdminProductDashboardResponse>.Success(dashboard),
        };
        var controller = CreateController(service);
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), TenantAdminProductPermissions.DashboardView);

        var result = await controller.GetDashboard(cancellationToken: CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetDashboard_WithPermissionDenied_ReturnsForbidden()
    {
        var service = new FakeTenantAdminProductService
        {
            DashboardResult = ApplicationResult<TenantAdminProductDashboardResponse>.Failure(
                new ApplicationError("product.permission_denied", "Permission denied for product management.")),
        };
        var controller = CreateController(service);
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), TenantAdminProductPermissions.View);

        var result = await controller.GetDashboard(cancellationToken: CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, objectResult.StatusCode);
    }

    private sealed class FakeTenantAdminProductService : ITenantAdminProductService
    {
        public ApplicationResult<TenantAdminProductSummaryCardsResponse> SummaryResult { get; init; } =
            ApplicationResult<TenantAdminProductSummaryCardsResponse>.Success(
                new TenantAdminProductSummaryCardsResponse(0, 0, 0, 0));

        public ApplicationResult<TenantAdminProductCreateOptionsResponse> CreateOptionsResult { get; init; } =
            ApplicationResult<TenantAdminProductCreateOptionsResponse>.Success(
                new TenantAdminProductCreateOptionsResponse([], [], [], [], [], [], []));

        public ApplicationResult<TenantAdminProductCreateResponse> CreateResult { get; init; } =
            ApplicationResult<TenantAdminProductCreateResponse>.Success(
                new TenantAdminProductCreateResponse(Guid.NewGuid(), "Sample Product", "SKU-001", "ACTIVE"));

        public ApplicationResult<TenantAdminProductDetailResponse> DetailResult { get; init; } =
            ApplicationResult<TenantAdminProductDetailResponse>.Success(
                new TenantAdminProductDetailResponse(
                    Guid.NewGuid(),
                    "Sample Product",
                    "SKU-001",
                    null,
                    Guid.NewGuid(),
                    "Beverages",
                    null,
                    null,
                    "PIECE",
                    null,
                    null,
                    [],
                    null,
                    10m,
                    null,
                    null,
                    null,
                    "ACTIVE",
                    false,
                    null,
                    [],
                    [],
                    null,
                    DateTimeOffset.UtcNow,
                    DateTimeOffset.UtcNow));

        public ApplicationResult<TenantAdminProductDetailResponse> UpdateResult { get; init; } =
            ApplicationResult<TenantAdminProductDetailResponse>.Success(
                new TenantAdminProductDetailResponse(
                    Guid.NewGuid(),
                    "Updated Product",
                    "SKU-001",
                    null,
                    Guid.NewGuid(),
                    "Beverages",
                    null,
                    null,
                    "PIECE",
                    null,
                    null,
                    [],
                    null,
                    10m,
                    null,
                    null,
                    null,
                    "ACTIVE",
                    false,
                    null,
                    [],
                    [],
                    null,
                    DateTimeOffset.UtcNow,
                    DateTimeOffset.UtcNow));

        public ApplicationResult<TenantAdminProductStatusUpdateResponse> StatusUpdateResult { get; init; } =
            ApplicationResult<TenantAdminProductStatusUpdateResponse>.Success(
                new TenantAdminProductStatusUpdateResponse(Guid.NewGuid(), "INACTIVE"));

        public ApplicationResult<TenantAdminProductDeleteResponse> DeleteResult { get; init; } =
            ApplicationResult<TenantAdminProductDeleteResponse>.Success(
                new TenantAdminProductDeleteResponse(Guid.NewGuid(), "Deleted", "DELETED"));

        public ApplicationResult<TenantAdminProductDashboardResponse> DashboardResult { get; init; } =
            ApplicationResult<TenantAdminProductDashboardResponse>.Success(
                new TenantAdminProductDashboardResponse(
                    DateTimeOffset.UtcNow,
                    "USD",
                    new TenantAdminProductDashboardSummaryDto(null, null, null, null, null, null),
                    null,
                    null));

        public Task<ApplicationResult<TenantAdminProductDashboardResponse>> GetDashboardAsync(
            TenantRequestContext context,
            TenantAdminProductDashboardQuery query,
            CancellationToken cancellationToken) =>
            Task.FromResult(DashboardResult);

        public Task<ApplicationResult<TenantAdminProductSummaryCardsResponse>> GetSummaryAsync(
            TenantRequestContext context,
            CancellationToken cancellationToken) =>
            Task.FromResult(SummaryResult);

        public Task<ApplicationResult<TenantAdminProductCreateOptionsResponse>> GetCreateOptionsAsync(
            TenantRequestContext context,
            CancellationToken cancellationToken) =>
            Task.FromResult(CreateOptionsResult);

        public Task<ApplicationResult<TenantAdminProductCreateResponse>> CreateAsync(
            TenantRequestContext context,
            TenantAdminProductCreateRequest request,
            CancellationToken cancellationToken) =>
            Task.FromResult(CreateResult);

        public Task<ApplicationResult<TenantAdminProductDetailResponse>> GetByIdAsync(
            TenantRequestContext context,
            Guid productId,
            CancellationToken cancellationToken) =>
            Task.FromResult(DetailResult);

        public Task<ApplicationResult<TenantAdminProductDetailResponse>> UpdateAsync(
            TenantRequestContext context,
            Guid productId,
            TenantAdminProductCreateRequest request,
            CancellationToken cancellationToken) =>
            Task.FromResult(UpdateResult);

        public Task<ApplicationResult<TenantAdminProductStatusUpdateResponse>> UpdateStatusAsync(
            TenantRequestContext context,
            Guid productId,
            TenantAdminProductStatusUpdateRequest request,
            CancellationToken cancellationToken) =>
            Task.FromResult(StatusUpdateResult);

        public Task<ApplicationResult<TenantAdminProductDeleteResponse>> DeleteAsync(
            TenantRequestContext context,
            Guid productId,
            CancellationToken cancellationToken) =>
            Task.FromResult(DeleteResult);

        public Task<ApplicationResult<TenantAdminProductListResponse>> ListAsync(
            TenantRequestContext context,
            string? search,
            int page,
            int pageSize,
            CancellationToken cancellationToken) =>
            Task.FromResult(
                ApplicationResult<TenantAdminProductListResponse>.Success(
                    new TenantAdminProductListResponse(
                        new TenantAdminProductSummaryResponse(0, 0, 0, 0),
                        [],
                        page,
                        pageSize,
                        0)));
    }

    private sealed class FakeTenantAdminInventoryService : ITenantAdminInventoryService
    {
        public Task<ApplicationResult<TenantAdminCurrentStockListResponse>> GetCurrentStockAsync(
            TenantRequestContext context,
            TenantAdminCurrentStockQuery query,
            CancellationToken cancellationToken) =>
            Task.FromResult(ApplicationResult<TenantAdminCurrentStockListResponse>.Failure(
                new ApplicationError("inventory.permission_denied", "Permission denied for inventory management.")));

        public Task<ApplicationResult<TenantAdminCurrentStockSummaryResponse>> GetCurrentStockSummaryAsync(
            TenantRequestContext context,
            Guid? outletId,
            CancellationToken cancellationToken) =>
            Task.FromResult(ApplicationResult<TenantAdminCurrentStockSummaryResponse>.Failure(
                new ApplicationError("inventory.permission_denied", "Permission denied for inventory management.")));

        public Task<ApplicationResult<TenantAdminStockInResponse>> StockInAsync(
            TenantRequestContext context,
            TenantAdminStockInRequest request,
            CancellationToken cancellationToken) =>
            Task.FromResult(ApplicationResult<TenantAdminStockInResponse>.Failure(
                new ApplicationError("inventory.permission_denied", "Permission denied for inventory management.")));

        public Task<ApplicationResult<TenantAdminInventoryVariantLookupResponse>> GetProductVariantsForStockInAsync(
            TenantRequestContext context,
            Guid productId,
            CancellationToken cancellationToken) =>
            Task.FromResult(ApplicationResult<TenantAdminInventoryVariantLookupResponse>.Failure(
                new ApplicationError("inventory.permission_denied", "Permission denied for inventory management.")));
    }

    private sealed class FakeTenantRequestContextFactory : ITenantRequestContextFactory
    {
        public bool TryCreate(ClaimsPrincipal user, out TenantRequestContext context)
        {
            var tenantUserIdValue = user.FindFirstValue("sub");
            var tenantIdValue = user.FindFirstValue("tenant_id");
            var hasTenantUserId = Guid.TryParse(tenantUserIdValue, out var tenantUserId);
            var hasTenantId = Guid.TryParse(tenantIdValue, out var tenantId);

            if (!hasTenantUserId || !hasTenantId)
            {
                context = new TenantRequestContext(Guid.Empty, Guid.Empty, []);
                return false;
            }

            var permissions = user.FindAll("permissions")
                .Select(claim => claim.Value)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .ToArray();

            context = new TenantRequestContext(tenantId, tenantUserId, permissions);
            return true;
        }
    }
}
