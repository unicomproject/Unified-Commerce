using System.Reflection;
using System.Security.Claims;
using E_POS.Api.Common;
using E_POS.Api.Controllers;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.CatalogProduct.Contracts;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace E_POS.ApiTests.CatalogProduct;

public sealed class PosProductsControllerTests
{
    [Fact]
    public async Task ListProducts_WithValidClaims_ReturnsOk()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var products = new List<PosProductSummaryResponseDto>
        {
            new(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "Team Jersey",
                "Official team jersey",
                "https://example.com/jersey.png",
                Guid.NewGuid(),
                "Apparel",
                12000,
                true,
                "in_stock",
                12m)
        };

        var service = new FakePosProductCatalogService
        {
            ListProductsResult = ApplicationResult<IReadOnlyList<PosProductSummaryResponseDto>>.Success(products),
        };
        var controller = CreateController(service);
        SetTenantClaims(controller, tenantId, userId, "products.view");

        var result = await controller.ListProducts(deviceId, null, null, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
        Assert.Equal(tenantId, service.Context?.TenantId);
        Assert.Equal(deviceId, service.DeviceId);
    }

    [Fact]
    public async Task ListProducts_WhenPermissionDenied_ReturnsForbidden()
    {
        var service = new FakePosProductCatalogService
        {
            ListProductsResult = ApplicationResult<IReadOnlyList<PosProductSummaryResponseDto>>.Failure(
                new ApplicationError("pos_products.permission_denied", "You do not have permission to view POS products.")),
        };
        var controller = CreateController(service);
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), "pos.home.view");

        var result = await controller.ListProducts(Guid.NewGuid(), null, null, CancellationToken.None);

        Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, ((ObjectResult)result).StatusCode);
    }

    [Fact]
    public async Task ListProducts_WhenDeviceNotFound_ReturnsNotFound()
    {
        var service = new FakePosProductCatalogService
        {
            ListProductsResult = ApplicationResult<IReadOnlyList<PosProductSummaryResponseDto>>.Failure(
                new ApplicationError("pos_products.device_not_found", "POS device could not be found.")),
        };
        var controller = CreateController(service);
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), "products.view");

        var result = await controller.ListProducts(Guid.NewGuid(), null, null, CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task ListCategories_WithValidClaims_ReturnsOk()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var categories = new List<PosCatalogCategoryResponseDto>
        {
            new(Guid.NewGuid(), "Apparel"),
            new(Guid.NewGuid(), "Footwear"),
        };

        var service = new FakePosProductCatalogService
        {
            ListCategoriesResult = ApplicationResult<IReadOnlyList<PosCatalogCategoryResponseDto>>.Success(categories),
        };
        var controller = CreateController(service);
        SetTenantClaims(controller, tenantId, userId, "products.view");

        var result = await controller.ListCategories(deviceId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
        Assert.Equal(tenantId, service.Context?.TenantId);
        Assert.Equal(deviceId, service.DeviceId);
    }

    [Fact]
    public void Controller_RequiresTenantOnlyPolicy()
    {
        var authorize = Assert.Single(typeof(PosProductsController).GetCustomAttributes<AuthorizeAttribute>());
        Assert.Equal("TenantOnly", authorize.Policy);
    }

    private static PosProductsController CreateController(FakePosProductCatalogService service)
    {
        var controller = new PosProductsController(service, new TenantRequestContextFactory());
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        return controller;
    }

    private static void SetTenantClaims(PosProductsController controller, Guid tenantId, Guid userId, string permission)
    {
        controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim("sub", userId.ToString()),
                new Claim("tenant_id", tenantId.ToString()),
                new Claim("permissions", permission)
            ],
            "Test"));
    }

    private sealed class FakePosProductCatalogService : IPosProductCatalogService
    {
        public ApplicationResult<IReadOnlyList<PosProductSummaryResponseDto>> ListProductsResult { get; init; } =
            ApplicationResult<IReadOnlyList<PosProductSummaryResponseDto>>.Failure(
                new ApplicationError("pos_products.list_failed", "POS products could not be loaded."));

        public ApplicationResult<IReadOnlyList<PosCatalogCategoryResponseDto>> ListCategoriesResult { get; init; } =
            ApplicationResult<IReadOnlyList<PosCatalogCategoryResponseDto>>.Failure(
                new ApplicationError("pos_catalog.categories_failed", "POS catalog categories could not be loaded."));

        public TenantRequestContext? Context { get; private set; }
        public Guid? DeviceId { get; private set; }
        public string? Search { get; private set; }

        public Task<ApplicationResult<IReadOnlyList<PosProductSummaryResponseDto>>> ListProductsAsync(
            TenantRequestContext context,
            Guid? deviceId,
            Guid? categoryId,
            string? search,
            CancellationToken cancellationToken)
        {
            Context = context;
            DeviceId = deviceId;
            Search = search;
            return Task.FromResult(ListProductsResult);
        }

        public Task<ApplicationResult<IReadOnlyList<PosCatalogCategoryResponseDto>>> ListCategoriesAsync(
            TenantRequestContext context,
            Guid? deviceId,
            CancellationToken cancellationToken)
        {
            Context = context;
            DeviceId = deviceId;
            return Task.FromResult(ListCategoriesResult);
        }
    }
}
