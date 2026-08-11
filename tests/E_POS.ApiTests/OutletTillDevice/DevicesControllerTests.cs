using System.Security.Claims;
using E_POS.Api.Common;
using E_POS.Api.Controllers;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Contracts;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Dtos;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace E_POS.ApiTests.OutletTillDevice;

public sealed class DevicesControllerTests
{
    [Fact]
    public async Task ActivateDevice_WithoutTenantClaims_ReturnsUnauthorized()
    {
        var service = new FakeDeviceContextService();
        var controller = CreateController(service);

        var result = await controller.ActivateDevice(ValidRequest(), CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Null(service.ActivationContext);
    }

    [Fact]
    public async Task ActivateDevice_WhenServiceRejectsMissingPermission_ReturnsForbidden()
    {
        var service = new FakeDeviceContextService
        {
            ActivationResult = ApplicationResult<CurrentDeviceResponseDto>.Failure(
                new ApplicationError(
                    "device_context.permission_denied",
                    "You do not have permission to activate POS devices.")),
        };
        var controller = CreateController(service);
        SetTenantClaims(controller, []);

        var result = await controller.ActivateDevice(ValidRequest(), CancellationToken.None);

        var forbidden = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, forbidden.StatusCode);
        Assert.Empty(service.ActivationContext!.Permissions);
    }

    [Fact]
    public async Task ActivateDevice_WithPermission_PassesPermissionToService()
    {
        var service = new FakeDeviceContextService
        {
            ActivationResult = ApplicationResult<CurrentDeviceResponseDto>.Failure(
                new ApplicationError(
                    "device_context.invalid_activation_code",
                    "Activation code is invalid.")),
        };
        var controller = CreateController(service);
        SetTenantClaims(controller, ["tenant.till.manage"]);

        var result = await controller.ActivateDevice(ValidRequest(), CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("tenant.till.manage", service.ActivationContext!.Permissions);
    }

    private static DevicesController CreateController(FakeDeviceContextService service)
    {
        var controller = new DevicesController(service, new TenantRequestContextFactory())
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
        };
        return controller;
    }

    private static void SetTenantClaims(ControllerBase controller, IReadOnlyCollection<string> permissions)
    {
        var claims = new List<Claim>
        {
            new("sub", Guid.NewGuid().ToString()),
            new("tenant_id", Guid.NewGuid().ToString()),
        };
        claims.AddRange(permissions.Select(permission => new Claim("permissions", permission)));
        controller.ControllerContext.HttpContext.User =
            new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
    }

    private static ActivateDeviceRequest ValidRequest() => new(
        "TILL-ACTIVE-POS",
        "device-fingerprint",
        "Front POS Tablet",
        "fixed_pos_tablet",
        "web",
        "1.0.0");

    private sealed class FakeDeviceContextService : IDeviceContextService
    {
        public TenantRequestContext? ActivationContext { get; private set; }
        public ApplicationResult<CurrentDeviceResponseDto> ActivationResult { get; init; } =
            ApplicationResult<CurrentDeviceResponseDto>.Failure(
                new ApplicationError("device_context.activation_failed", "Activation failed."));

        public Task<ApplicationResult<CurrentDeviceResponseDto>> GetCurrentDeviceAsync(
            TenantRequestContext context,
            string? deviceFingerprint,
            CancellationToken cancellationToken) => Task.FromResult(ActivationResult);

        public Task<ApplicationResult<CurrentDeviceResponseDto>> ActivateDeviceAsync(
            TenantRequestContext context,
            ActivateDeviceRequest request,
            CancellationToken cancellationToken)
        {
            ActivationContext = context;
            return Task.FromResult(ActivationResult);
        }

        public Task<ApplicationResult<DeviceHeartbeatResponse>> RecordHeartbeatAsync(
            TenantRequestContext context,
            DeviceHeartbeatRequest request,
            CancellationToken cancellationToken) =>
            Task.FromResult(ApplicationResult<DeviceHeartbeatResponse>.Failure(
                new ApplicationError("device_context.heartbeat_failed", "Heartbeat failed.")));
    }
}
