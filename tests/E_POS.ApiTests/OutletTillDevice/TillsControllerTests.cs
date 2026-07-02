using System.Reflection;
using System.Security.Claims;
using E_POS.Api.Controllers;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.OutletTillDevice.Contracts;
using E_POS.Application.Modules.OutletTillDevice.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace E_POS.ApiTests.OutletTillDevice;

public sealed class TillsControllerTests
{
    [Fact]
    public async Task Create_WithTenantClaims_ReturnsCreatedAndPassesServerTenantContext()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var response = CreateResponse(Guid.NewGuid());
        var service = new FakeTillService(ApplicationResult<TillResponse>.Success(response));
        var controller = CreateController(service);
        SetTenantClaims(controller, tenantId, userId, "tenant.tills.create");

        var result = await controller.Create(CreateRequest(), CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Same(response, created.Value);
        Assert.Equal(tenantId, service.CreateContext?.TenantId);
        Assert.Equal(userId, service.CreateContext?.UserId);
        Assert.Contains("tenant.tills.create", service.CreateContext!.Permissions);
    }

    [Fact]
    public async Task Create_WithoutTenantClaims_ReturnsUnauthorized()
    {
        var service = new FakeTillService(ApplicationResult<TillResponse>.Success(CreateResponse(Guid.NewGuid())));
        var controller = CreateController(service);

        var result = await controller.Create(CreateRequest(), CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Null(service.CreateContext);
    }

    [Fact]
    public async Task Create_WhenServiceReturnsPermissionDenied_ReturnsForbidden()
    {
        var service = new FakeTillService(ApplicationResult<TillResponse>.Failure(new ApplicationError("till.permission_denied", "Permission denied for till management.")));
        var controller = CreateController(service);
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), "tenant.outlets.manage");

        var result = await controller.Create(CreateRequest(), CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, objectResult.StatusCode);
    }

    [Fact]
    public async Task Create_WhenServiceReturnsDuplicateCode_ReturnsConflict()
    {
        var service = new FakeTillService(ApplicationResult<TillResponse>.Failure(new ApplicationError("till.duplicate_code", "Till code already exists for this outlet.")));
        var controller = CreateController(service);
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), "tenant.tills.manage");

        var result = await controller.Create(CreateRequest(), CancellationToken.None);

        var objectResult = Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal(StatusCodes.Status409Conflict, objectResult.StatusCode);
    }

    [Fact]
    public void Controller_RequiresTenantOnlyPolicy()
    {
        var authorize = Assert.Single(typeof(TillsController).GetCustomAttributes<AuthorizeAttribute>());
        Assert.Equal("TenantOnly", authorize.Policy);
    }

    private static TillsController CreateController(FakeTillService service)
    {
        var controller = new TillsController(service);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        return controller;
    }

    private static void SetTenantClaims(TillsController controller, Guid tenantId, Guid userId, string permission)
    {
        controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim("sub", userId.ToString()),
                new Claim("tenant_id", tenantId.ToString()),
                new Claim("permissions", permission)
            ],
            "Test"));
    }

    private static TillCreateRequest CreateRequest()
    {
        return new TillCreateRequest(Guid.NewGuid(), "Main Till", "MAIN-01", "ACTIVE");
    }

    private static TillResponse CreateResponse(Guid id)
    {
        return new TillResponse(id, Guid.NewGuid(), "MAIN", "Main Outlet", "MAIN-01", "Main Till", "ACTIVE", false, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
    }

    private sealed class FakeTillService : ITillService
    {
        private readonly ApplicationResult<TillResponse> _createResult;

        public FakeTillService(ApplicationResult<TillResponse> createResult)
        {
            _createResult = createResult;
        }

        public TenantRequestContext? CreateContext { get; private set; }

        public Task<ApplicationResult<TillResponse>> CreateAsync(TenantRequestContext context, TillCreateRequest request, CancellationToken cancellationToken)
        {
            CreateContext = context;
            return Task.FromResult(_createResult);
        }

        public Task<ApplicationResult<TillListResponse>> ListAsync(TenantRequestContext context, Guid? outletId, int pageNumber, int pageSize, string? search, CancellationToken cancellationToken)
        {
            return Task.FromResult(ApplicationResult<TillListResponse>.Success(new TillListResponse([], pageNumber, pageSize, 0)));
        }

        public Task<ApplicationResult<TillResponse>> GetByIdAsync(TenantRequestContext context, Guid tillId, CancellationToken cancellationToken)
        {
            return Task.FromResult(ApplicationResult<TillResponse>.Success(CreateResponse(tillId)));
        }

        public Task<ApplicationResult<TillResponse>> UpdateAsync(TenantRequestContext context, Guid tillId, TillUpdateRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult(ApplicationResult<TillResponse>.Success(CreateResponse(tillId)));
        }

        public Task<ApplicationResult> DeleteAsync(TenantRequestContext context, Guid tillId, CancellationToken cancellationToken)
        {
            return Task.FromResult(ApplicationResult.Success());
        }
    }
}