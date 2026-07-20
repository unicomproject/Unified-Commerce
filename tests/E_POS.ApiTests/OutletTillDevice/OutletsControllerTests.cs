using System.Reflection;
using System.Security.Claims;
using E_POS.Api.Common;
using E_POS.Api.Controllers;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Contracts;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace E_POS.ApiTests.OutletTillDevice;

public sealed class OutletsControllerTests
{
    [Fact]
    public async Task Create_WithTenantClaims_ReturnsCreatedAndPassesServerTenantContext()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var response = CreateResponse(Guid.NewGuid());
        var service = new FakeOutletService(ApplicationResult<OutletResponse>.Success(response));
        var controller = CreateController(service);
        SetTenantClaims(controller, tenantId, userId, "tenant.outlets.manage");

        var request = CreateRequest() with
        {
            CollectionEnabled = true,
            PreparationLeadMinutes = 45,
            PickupWindowMinutes = 30,
            CollectionCutoffTime = new TimeOnly(16, 0)
        };

        var result = await controller.Create(request, CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Same(response, created.Value);
        Assert.Equal(tenantId, service.CreateContext?.TenantId);
        Assert.Equal(userId, service.CreateContext?.UserId);
        Assert.Contains("tenant.outlets.manage", service.CreateContext!.Permissions);
        Assert.Equal(45, service.CreateRequest!.PreparationLeadMinutes);
        Assert.Equal(30, service.CreateRequest.PickupWindowMinutes);
        Assert.Equal(new TimeOnly(16, 0), service.CreateRequest.CollectionCutoffTime);
    }

    [Fact]
    public async Task Create_WithoutTenantClaims_ReturnsUnauthorized()
    {
        var service = new FakeOutletService(ApplicationResult<OutletResponse>.Success(CreateResponse(Guid.NewGuid())));
        var controller = CreateController(service);

        var result = await controller.Create(CreateRequest(), CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Null(service.CreateContext);
    }

    [Fact]
    public async Task Create_WhenServiceReturnsPermissionDenied_ReturnsForbidden()
    {
        var service = new FakeOutletService(ApplicationResult<OutletResponse>.Failure(new ApplicationError("outlet.permission_denied", "Permission denied for outlet management.")));
        var controller = CreateController(service);
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), "tenant.dashboard.view");

        var result = await controller.Create(CreateRequest(), CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, objectResult.StatusCode);
    }

    [Fact]
    public async Task List_WithViewPermission_PassesPermissionToService()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var service = new FakeOutletService(ApplicationResult<OutletResponse>.Success(CreateResponse(Guid.NewGuid())));
        var controller = CreateController(service);
        SetTenantClaims(controller, tenantId, userId, "tenant.outlets.view");

        var result = await controller.List(cancellationToken: CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal(tenantId, service.ListContext?.TenantId);
        Assert.Contains("tenant.outlets.view", service.ListContext!.Permissions);
    }
    [Fact]
    public async Task GetCreateOptions_WithManagePermission_ReturnsOk()
    {
        var service = new FakeOutletService(ApplicationResult<OutletResponse>.Success(CreateResponse(Guid.NewGuid())));
        var controller = CreateController(service);
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), "tenant.outlets.manage");

        var result = await controller.GetCreateOptions(CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public void Controller_RequiresTenantOnlyPolicy()
    {
        var authorize = Assert.Single(typeof(OutletsController).GetCustomAttributes<AuthorizeAttribute>());
        Assert.Equal("TenantOnly", authorize.Policy);
    }

    private static OutletsController CreateController(FakeOutletService service)
    {
        var controller = new OutletsController(service, new TenantRequestContextFactory());
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        return controller;
    }

    private static void SetTenantClaims(OutletsController controller, Guid tenantId, Guid userId, string permission)
    {
        controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim("sub", userId.ToString()),
                new Claim("tenant_id", tenantId.ToString()),
                new Claim("permissions", permission)
            ],
            "Test"));
    }

    private static OutletCreateRequest CreateRequest()
    {
        return new OutletCreateRequest(
            "Main Outlet",
            "ACTIVE",
            "STORE",
            "UTC",
            true,
            null,
            null,
            new OutletAddressRequest("1 Main Street", null, "Colombo", null, null, "LK", null, null),
            [],
            false);
    }

    private static OutletResponse CreateResponse(Guid id)
    {
        return new OutletResponse(
            id,
            "MAIN001",
            "Main Outlet",
            "ACTIVE",
            "STORE",
            "UTC",
            true,
            null,
            null,
            new OutletAddressResponse(Guid.NewGuid(), "PHYSICAL", "1 Main Street", null, "Colombo", null, null, "LK", null, null, true, "ACTIVE"),
            [],
            false,
            null,
            null,
            null,
            DateTimeOffset.UtcNow,
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            Guid.NewGuid());
    }

    private sealed class FakeOutletService : IOutletService
    {
        private readonly ApplicationResult<OutletResponse> _createResult;

        public FakeOutletService(ApplicationResult<OutletResponse> createResult)
        {
            _createResult = createResult;
        }

        public TenantRequestContext? CreateContext { get; private set; }
        public OutletCreateRequest? CreateRequest { get; private set; }

        public TenantRequestContext? ListContext { get; private set; }

        public Task<ApplicationResult<OutletCreateOptionsResponse>> GetCreateOptionsAsync(TenantRequestContext context, CancellationToken cancellationToken)
        {
            return Task.FromResult(ApplicationResult<OutletCreateOptionsResponse>.Success(
                new OutletCreateOptionsResponse([], [], [], new OutletCreateDefaultsResponse("LK", "UTC", "ACTIVE"))));
        }

        public Task<ApplicationResult<OutletResponse>> CreateAsync(TenantRequestContext context, OutletCreateRequest request, CancellationToken cancellationToken)
        {
            CreateContext = context;
            CreateRequest = request;
            return Task.FromResult(_createResult);
        }

        public Task<ApplicationResult<OutletListResponse>> ListAsync(TenantRequestContext context, int pageNumber, int pageSize, string? search, CancellationToken cancellationToken)
        {
            ListContext = context;
            return Task.FromResult(ApplicationResult<OutletListResponse>.Success(new OutletListResponse([], pageNumber, pageSize, 0)));
        }

        public Task<ApplicationResult<OutletResponse>> GetByIdAsync(TenantRequestContext context, Guid outletId, CancellationToken cancellationToken)
        {
            return Task.FromResult(ApplicationResult<OutletResponse>.Success(CreateResponse(outletId)));
        }

        public Task<ApplicationResult<OutletResponse>> UpdateAsync(TenantRequestContext context, Guid outletId, OutletUpdateRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult(ApplicationResult<OutletResponse>.Success(CreateResponse(outletId)));
        }

        public Task<ApplicationResult> DeleteAsync(TenantRequestContext context, Guid outletId, CancellationToken cancellationToken)
        {
            return Task.FromResult(ApplicationResult.Success());
        }
    }
}

