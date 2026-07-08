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

public sealed class UnitOfMeasuresControllerTests
{
    [Fact]
    public async Task List_WithTenantClaims_ReturnsOkAndPassesServerTenantContext()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var response = new UnitOfMeasureListResponse([]);
        var service = new FakeUnitOfMeasureService(ApplicationResult<UnitOfMeasureListResponse>.Success(response));
        var controller = CreateController(service);
        SetTenantClaims(controller, tenantId, userId, "catalog.products.view");

        var result = await controller.List(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(response, ok.Value);
        Assert.Equal(tenantId, service.Context?.TenantId);
        Assert.Equal(userId, service.Context?.UserId);
        Assert.Contains("catalog.products.view", service.Context!.Permissions);
    }

    [Fact]
    public async Task List_WithoutTenantClaims_ReturnsUnauthorized()
    {
        var service = new FakeUnitOfMeasureService(ApplicationResult<UnitOfMeasureListResponse>.Success(new UnitOfMeasureListResponse([])));
        var controller = CreateController(service);

        var result = await controller.List(CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Null(service.Context);
    }

    [Fact]
    public async Task List_WhenServiceReturnsPermissionDenied_ReturnsForbidden()
    {
        var service = new FakeUnitOfMeasureService(ApplicationResult<UnitOfMeasureListResponse>.Failure(new ApplicationError("unit_of_measure.permission_denied", "Permission denied.")));
        var controller = CreateController(service);
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), "catalog.products.view");

        var result = await controller.List(CancellationToken.None);

        Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, ((ObjectResult)result).StatusCode);
    }

    [Fact]
    public void UnitOfMeasuresController_RequiresTenantOnlyPolicy()
    {
        var authorize = Assert.Single(typeof(UnitOfMeasuresController).GetCustomAttributes<AuthorizeAttribute>());
        Assert.Equal("TenantOnly", authorize.Policy);
    }

    private static UnitOfMeasuresController CreateController(FakeUnitOfMeasureService service)
    {
        var controller = new UnitOfMeasuresController(service, new TenantRequestContextFactory());
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

    private sealed class FakeUnitOfMeasureService : IUnitOfMeasureService
    {
        private readonly ApplicationResult<UnitOfMeasureListResponse> _result;

        public FakeUnitOfMeasureService(ApplicationResult<UnitOfMeasureListResponse> result)
        {
            _result = result;
        }

        public TenantRequestContext? Context { get; private set; }

        public Task<ApplicationResult<UnitOfMeasureListResponse>> ListAsync(TenantRequestContext context, CancellationToken cancellationToken)
        {
            Context = context;
            return Task.FromResult(_result);
        }
    }
}
