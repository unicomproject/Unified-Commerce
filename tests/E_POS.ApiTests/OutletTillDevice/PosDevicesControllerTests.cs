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

public sealed class PosDevicesControllerTests
{
    [Fact]
    public async Task Create_WithTenantClaims_ReturnsCreatedAndPassesServerTenantContext()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var response = CreateDeviceResponse(Guid.NewGuid());
        var service = new FakePosDeviceService(ApplicationResult<PosDeviceResponse>.Success(response));
        var controller = CreateDeviceController(service);
        SetTenantClaims(controller, tenantId, userId, "tenant.devices.create");

        var result = await controller.Create(CreateDeviceRequest(), CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Same(response, created.Value);
        Assert.Equal(tenantId, service.CreateContext?.TenantId);
        Assert.Equal(userId, service.CreateContext?.UserId);
        Assert.Contains("tenant.devices.create", service.CreateContext!.Permissions);
    }

    [Fact]
    public async Task Create_WithoutTenantClaims_ReturnsUnauthorized()
    {
        var service = new FakePosDeviceService(ApplicationResult<PosDeviceResponse>.Success(CreateDeviceResponse(Guid.NewGuid())));
        var controller = CreateDeviceController(service);

        var result = await controller.Create(CreateDeviceRequest(), CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Null(service.CreateContext);
    }

    [Fact]
    public async Task Create_WhenServiceReturnsDuplicateSerial_ReturnsConflict()
    {
        var service = new FakePosDeviceService(ApplicationResult<PosDeviceResponse>.Failure(new ApplicationError("pos_device.duplicate_serial_number", "POS device serial number already exists.")));
        var controller = CreateDeviceController(service);
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), "tenant.devices.manage");

        var result = await controller.Create(CreateDeviceRequest(), CancellationToken.None);

        Assert.IsType<ConflictObjectResult>(result);
    }

    [Fact]
    public void PosDeviceController_RequiresTenantOnlyPolicy()
    {
        var authorize = Assert.Single(typeof(PosDevicesController).GetCustomAttributes<AuthorizeAttribute>());
        Assert.Equal("TenantOnly", authorize.Policy);
    }

    [Fact]
    public async Task Assign_WithTenantClaims_ReturnsCreatedAndPassesServerTenantContext()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var tillId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var response = CreateAssignmentResponse(tillId, deviceId);
        var service = new FakeTillDeviceAssignmentService(ApplicationResult<TillDeviceAssignmentResponse>.Success(response));
        var controller = CreateAssignmentController(service);
        SetTenantClaims(controller, tenantId, userId, "tenant.tills.manage");

        var result = await controller.Assign(tillId, deviceId, CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Same(response, created.Value);
        Assert.Equal(tenantId, service.AssignContext?.TenantId);
        Assert.Contains("tenant.tills.manage", service.AssignContext!.Permissions);
    }

    [Fact]
    public async Task Assign_WhenServiceReturnsDeviceAlreadyAssigned_ReturnsConflict()
    {
        var service = new FakeTillDeviceAssignmentService(ApplicationResult<TillDeviceAssignmentResponse>.Failure(new ApplicationError("till_device_assignment.device_already_assigned", "POS device is already assigned to another till.")));
        var controller = CreateAssignmentController(service);
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), "tenant.tills.manage");

        var result = await controller.Assign(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<ConflictObjectResult>(result);
    }

    [Fact]
    public void TillDeviceAssignmentsController_RequiresTenantOnlyPolicy()
    {
        var authorize = Assert.Single(typeof(TillDeviceAssignmentsController).GetCustomAttributes<AuthorizeAttribute>());
        Assert.Equal("TenantOnly", authorize.Policy);
    }

    private static PosDevicesController CreateDeviceController(FakePosDeviceService service)
    {
        var controller = new PosDevicesController(service, new TenantRequestContextFactory());
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        return controller;
    }

    private static TillDeviceAssignmentsController CreateAssignmentController(FakeTillDeviceAssignmentService service)
    {
        var controller = new TillDeviceAssignmentsController(service, new TenantRequestContextFactory());
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

    private static PosDeviceCreateRequest CreateDeviceRequest()
    {
        return new PosDeviceCreateRequest(Guid.NewGuid(), "Front Tablet", "SN-001", "ACTIVE");
    }

    private static PosDeviceResponse CreateDeviceResponse(Guid id)
    {
        return new PosDeviceResponse(id, Guid.NewGuid(), "OUT001", "Main Outlet", "DEV001", "Front Tablet", "SN-001", "ACTIVE", null, null, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
    }

    private static TillDeviceAssignmentResponse CreateAssignmentResponse(Guid tillId, Guid deviceId)
    {
        return new TillDeviceAssignmentResponse(Guid.NewGuid(), tillId, "TILL001", "Main Till", deviceId, "DEV001", "Front Tablet", Guid.NewGuid(), "OUT001", "Main Outlet", DateTimeOffset.UtcNow.UtcDateTime.ToString("O"), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
    }

    private sealed class FakePosDeviceService : IPosDeviceService
    {
        private readonly ApplicationResult<PosDeviceResponse> _createResult;

        public FakePosDeviceService(ApplicationResult<PosDeviceResponse> createResult)
        {
            _createResult = createResult;
        }

        public TenantRequestContext? CreateContext { get; private set; }

        public Task<ApplicationResult<PosDeviceResponse>> CreateAsync(TenantRequestContext context, PosDeviceCreateRequest request, CancellationToken cancellationToken)
        {
            CreateContext = context;
            return Task.FromResult(_createResult);
        }

        public Task<ApplicationResult<PosDeviceListResponse>> ListAsync(TenantRequestContext context, Guid? outletId, int pageNumber, int pageSize, string? search, CancellationToken cancellationToken) => Task.FromResult(ApplicationResult<PosDeviceListResponse>.Success(new PosDeviceListResponse([], pageNumber, pageSize, 0)));
        public Task<ApplicationResult<PosDeviceResponse>> GetByIdAsync(TenantRequestContext context, Guid posDeviceId, CancellationToken cancellationToken) => Task.FromResult(ApplicationResult<PosDeviceResponse>.Success(CreateDeviceResponse(posDeviceId)));
        public Task<ApplicationResult<PosDeviceResponse>> UpdateAsync(TenantRequestContext context, Guid posDeviceId, PosDeviceUpdateRequest request, CancellationToken cancellationToken) => Task.FromResult(ApplicationResult<PosDeviceResponse>.Success(CreateDeviceResponse(posDeviceId)));
        public Task<ApplicationResult> DeleteAsync(TenantRequestContext context, Guid posDeviceId, CancellationToken cancellationToken) => Task.FromResult(ApplicationResult.Success());
    }

    private sealed class FakeTillDeviceAssignmentService : ITillDeviceAssignmentService
    {
        private readonly ApplicationResult<TillDeviceAssignmentResponse> _assignResult;

        public FakeTillDeviceAssignmentService(ApplicationResult<TillDeviceAssignmentResponse> assignResult)
        {
            _assignResult = assignResult;
        }

        public TenantRequestContext? AssignContext { get; private set; }

        public Task<ApplicationResult<TillDeviceAssignmentResponse>> AssignAsync(TenantRequestContext context, Guid tillId, Guid posDeviceId, CancellationToken cancellationToken)
        {
            AssignContext = context;
            return Task.FromResult(_assignResult);
        }

        public Task<ApplicationResult<TillDeviceAssignmentListResponse>> ListByTillAsync(TenantRequestContext context, Guid tillId, CancellationToken cancellationToken) => Task.FromResult(ApplicationResult<TillDeviceAssignmentListResponse>.Success(new TillDeviceAssignmentListResponse(tillId, [])));
        public Task<ApplicationResult> RemoveAsync(TenantRequestContext context, Guid tillId, Guid posDeviceId, CancellationToken cancellationToken) => Task.FromResult(ApplicationResult.Success());
    }
}
