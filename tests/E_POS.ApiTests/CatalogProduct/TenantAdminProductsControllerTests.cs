using System.Reflection;
using System.Security.Claims;
using E_POS.Api.Common;
using E_POS.Api.Controllers;
using E_POS.Api.Controllers.V1.Tenant.CatalogProduct;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Shared.Media.Dtos;
using E_POS.Application.Modules.Tenant.CatalogProduct.Contracts;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.TenantAdmin;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace E_POS.ApiTests.CatalogProduct;

public sealed class TenantAdminProductsControllerTests
{
    [Fact]
    public async Task UploadBrandLogo_WithCreatePermission_PassesCreationContextAndReturnsBrand()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var brandId = Guid.NewGuid();
        var brand = new BrandResponse(brandId, "ACME", "Acme", "https://cdn/brand.png", Guid.NewGuid(), "ACTIVE", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
        var mediaService = new FakeCatalogMediaService
        {
            BrandLogoResult = ApplicationResult<MediaAssetUploadResponse>.Success(
                new MediaAssetUploadResponse(Guid.NewGuid(), null, null, null, null, brandId, "media", "key", brand.LogoUrl!, null, brand.LogoUrl!, "brand.png", "image/png", ".png", 68, 1, 1, "hash")),
        };
        var brandService = new FakeBrandService
        {
            DetailResult = ApplicationResult<BrandResponse>.Success(brand),
        };
        var controller = new CatalogMediaController(mediaService, brandService, new TenantRequestContextFactory())
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
        };
        SetTenantClaims(controller, tenantId, userId, BrandConstants.CreatePermission);
        await using var stream = new MemoryStream(CreateOnePixelPng());
        var file = new FormFile(stream, 0, stream.Length, "file", "brand.png") { Headers = new HeaderDictionary(), ContentType = "image/png" };

        var result = await controller.UploadBrandLogo(brandId, file, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal(tenantId, mediaService.BrandLogoContext?.TenantId);
        Assert.Equal(userId, mediaService.BrandLogoContext?.UserId);
        Assert.Contains(BrandConstants.CreatePermission, mediaService.BrandLogoContext!.Permissions);
        Assert.Equal(brandId, mediaService.BrandLogoId);
    }

    [Fact]
    public async Task UploadBrandLogo_WhenInitialCompletionUnauthorized_ReturnsForbiddenStableCode()
    {
        var mediaService = new FakeCatalogMediaService
        {
            BrandLogoResult = ApplicationResult<MediaAssetUploadResponse>.Failure(
                new ApplicationError("media.initial_brand_logo_not_authorized", "Not authorized.")),
        };
        var controller = new CatalogMediaController(mediaService, new FakeBrandService(), new TenantRequestContextFactory())
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
        };
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), BrandConstants.CreatePermission);
        await using var stream = new MemoryStream(CreateOnePixelPng());
        var file = new FormFile(stream, 0, stream.Length, "file", "brand.png") { Headers = new HeaderDictionary(), ContentType = "image/png" };

        var result = await controller.UploadBrandLogo(Guid.NewGuid(), file, CancellationToken.None);

        var forbidden = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, forbidden.StatusCode);
        Assert.Contains("media.initial_brand_logo_not_authorized", forbidden.Value!.ToString());
    }

    private static byte[] CreateOnePixelPng() => Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+/p9sAAAAASUVORK5CYII=");
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
    public async Task List_WithValidFilters_ReturnsOk()
    {
        var categoryId = Guid.NewGuid();
        var brandId = Guid.NewGuid();
        var service = new FakeTenantAdminProductService();
        var controller = CreateController(service);
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), TenantAdminProductPermissions.View);

        var result = await controller.List(
            search: "Jersey",
            categoryId: categoryId,
            brandId: brandId,
            productStatus: "ACTIVE",
            stockStatus: "IN_STOCK",
            page: 2,
            pageSize: 10,
            sortBy: "productName",
            sortDirection: "asc",
            cancellationToken: CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetFilterOptions_WithPermission_ReturnsOk()
    {
        var service = new FakeTenantAdminProductService();
        var controller = CreateController(service);
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), TenantAdminProductPermissions.View);

        var result = await controller.GetFilterOptions(CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
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
            new FakeCatalogMediaService(),
            new FakeTenantRequestContextFactory());
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext(),
        };
        return controller;
    }

    private static void SetTenantClaims(
        ControllerBase controller,
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
                new TenantAdminProductCreateOptionsResponse([], [], [], [], [], [], [], []));

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

        public Task<ApplicationResult<TenantAdminProductFilterOptionsResponse>> GetFilterOptionsAsync(
            TenantRequestContext context,
            CancellationToken cancellationToken) =>
            Task.FromResult(ApplicationResult<TenantAdminProductFilterOptionsResponse>.Success(
                new TenantAdminProductFilterOptionsResponse([], [], [], [])));

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
            Guid? categoryId,
            Guid? brandId,
            string? productStatus,
            string? stockStatus,
            int pageNumber,
            int pageSize,
            string? sortBy,
            string? sortDirection,
            CancellationToken cancellationToken) =>
            Task.FromResult(
                ApplicationResult<TenantAdminProductListResponse>.Success(
                    new TenantAdminProductListResponse(
                        [],
                        pageNumber,
                        pageSize,
                        0,
                        0,
                        false,
                        false,
                        0)));

        public ApplicationResult<ProductDraftResponse> DraftResult { get; init; } =
            ApplicationResult<ProductDraftResponse>.Success(
                new ProductDraftResponse(
                    Guid.NewGuid(),
                    "Draft Product",
                    "DRF-001",
                    "DRAFT",
                    "ACTIVE",
                    1,
                    DateTimeOffset.UtcNow,
                    1,
                    Guid.NewGuid(),
                    null,
                    null,
                    null,
                    true,
                    false,
                    false,
                    false,
                    false,
                    "SIMPLE",
                    false,
                    []));

        public ApplicationResult<ProductSetupWizardDto> SetupResult { get; init; } =
            ApplicationResult<ProductSetupWizardDto>.Success(
                new ProductSetupWizardDto(
                    Guid.NewGuid(),
                    "Draft Product",
                    "DRF-001",
                    "DRAFT",
                    "ACTIVE",
                    1,
                    DateTimeOffset.UtcNow,
                    1,
                    Guid.NewGuid(),
                    null,
                    null,
                    null,
                    true,
                    false,
                    false,
                    false,
                    false,
                    "SIMPLE",
                    false,
                    []));

        public Task<ApplicationResult<ProductDraftResponse>> SaveDraftAsync(
            TenantRequestContext context,
            SaveProductDraftRequest request,
            CancellationToken cancellationToken) =>
            Task.FromResult(DraftResult);

        public Task<ApplicationResult<ProductDraftResponse>> UpdateDraftAsync(
            TenantRequestContext context,
            Guid productId,
            SaveProductDraftRequest request,
            CancellationToken cancellationToken) =>
            Task.FromResult(DraftResult);

        public Task<ApplicationResult<ProductSetupWizardDto>> GetSetupAsync(
            TenantRequestContext context,
            Guid productId,
            CancellationToken cancellationToken) =>
            Task.FromResult(SetupResult);
    }



    private sealed class FakeCatalogMediaService : ICatalogMediaService
    {
        public TenantRequestContext? BrandLogoContext { get; private set; }
        public Guid? BrandLogoId { get; private set; }
        public ApplicationResult<MediaAssetUploadResponse> BrandLogoResult { get; init; } =
            ApplicationResult<MediaAssetUploadResponse>.Failure(new ApplicationError("media.permission_denied", "Permission denied for media upload."));
        public Task<ApplicationResult<MediaAssetUploadResponse>> UploadProductImageAsync(
            TenantRequestContext context,
            Guid productId,
            ProductImageUploadRequest request,
            MediaUploadFile file,
            CancellationToken cancellationToken) =>
            Task.FromResult(ApplicationResult<MediaAssetUploadResponse>.Failure(
                new ApplicationError("media.permission_denied", "Permission denied for media upload.")));

        public Task<ApplicationResult<StagedProductImageResponse>> StageProductImageAsync(
            TenantRequestContext context,
            MediaUploadFile file,
            Guid? uploadSessionId,
            CancellationToken cancellationToken) =>
            Task.FromResult(ApplicationResult<StagedProductImageResponse>.Failure(
                new ApplicationError("media.permission_denied", "Permission denied for media upload.")));

        public Task<ApplicationResult<ProductImagesMutationResponse>> ReorderProductImagesAsync(
            TenantRequestContext context,
            Guid productId,
            ReorderProductImagesRequest request,
            CancellationToken cancellationToken) =>
            Task.FromResult(ApplicationResult<ProductImagesMutationResponse>.Failure(
                new ApplicationError("media.permission_denied", "Permission denied for media upload.")));

        public Task<ApplicationResult<ProductImagesMutationResponse>> DeleteProductImageAsync(
            TenantRequestContext context,
            Guid productId,
            Guid productImageId,
            long? expectedRowVersion,
            CancellationToken cancellationToken) =>
            Task.FromResult(ApplicationResult<ProductImagesMutationResponse>.Failure(
                new ApplicationError("media.permission_denied", "Permission denied for media upload.")));

        public Task<ApplicationResult<ProductImagesMutationResponse>> ReplaceProductImagesAsync(
            TenantRequestContext context,
            Guid productId,
            long expectedRowVersion,
            IReadOnlyList<MediaUploadFile>? files,
            IReadOnlyList<Guid>? stagedMediaAssetIds,
            CancellationToken cancellationToken) =>
            Task.FromResult(ApplicationResult<ProductImagesMutationResponse>.Failure(
                new ApplicationError("media.permission_denied", "Permission denied for media upload.")));

        public Task<ApplicationResult<MediaAssetUploadResponse>> UploadCategoryImageAsync(
            TenantRequestContext context,
            Guid categoryId,
            MediaUploadFile file,
            CancellationToken cancellationToken) =>
            Task.FromResult(ApplicationResult<MediaAssetUploadResponse>.Failure(
                new ApplicationError("media.permission_denied", "Permission denied for media upload.")));

        public Task<ApplicationResult<MediaAssetUploadResponse>> UploadBrandLogoAsync(
            TenantRequestContext context,
            Guid brandId,
            MediaUploadFile file,
            CancellationToken cancellationToken)
        {
            BrandLogoContext = context;
            BrandLogoId = brandId;
            return Task.FromResult(BrandLogoResult);
        }
    }

    private sealed class FakeBrandService : IBrandService
    {
        public ApplicationResult<BrandResponse> DetailResult { get; init; } =
            ApplicationResult<BrandResponse>.Failure(new ApplicationError("brand.not_found", "Brand was not found."));

        public Task<ApplicationResult<BrandResponse>> CreateAsync(TenantRequestContext context, BrandCreateRequest request, CancellationToken cancellationToken) => Task.FromResult(DetailResult);
        public Task<ApplicationResult<BrandListResponse>> ListAsync(TenantRequestContext context, int pageNumber, int pageSize, string? search, CancellationToken cancellationToken) => Task.FromResult(ApplicationResult<BrandListResponse>.Success(new BrandListResponse([], pageNumber, pageSize, 0)));
        public Task<ApplicationResult<BrandResponse>> GetByIdAsync(TenantRequestContext context, Guid brandId, CancellationToken cancellationToken) => Task.FromResult(DetailResult);
        public Task<ApplicationResult<BrandResponse>> GetByIdAfterMutationAsync(TenantRequestContext context, Guid brandId, CancellationToken cancellationToken) => Task.FromResult(DetailResult);
        public Task<ApplicationResult<BrandResponse>> UpdateAsync(TenantRequestContext context, Guid brandId, BrandUpdateRequest request, CancellationToken cancellationToken) => Task.FromResult(DetailResult);
        public Task<ApplicationResult> DeleteAsync(TenantRequestContext context, Guid brandId, CancellationToken cancellationToken) => Task.FromResult(ApplicationResult.Success());
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
