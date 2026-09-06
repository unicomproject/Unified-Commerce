using System.Security.Claims;
using E_POS.Api.Common;
using E_POS.Api.Controllers;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.POSOperations.Contracts;
using E_POS.Application.Modules.Tenant.POSOperations.Dtos;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace E_POS.ApiTests.POSOperations;

public sealed class PosNotificationsControllerTests
{
    [Fact]
    public async Task CanonicalPermission_ResultIsReturned()
    {
        var service = new FakeService(ApplicationResult<PosNotificationInboxResponseDto>.Success(
            new PosNotificationInboxResponseDto([], 2, 1, 20, 0, 0)));
        var controller = Controller(service, "pos.notifications.alerts.view");

        var result = await controller.Get(1, 20, default);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task PermissionDenial_IsForbidden()
    {
        var service = new FakeService(ApplicationResult<PosNotificationInboxResponseDto>.Failure(
            new ApplicationError("pos_notifications.permission_denied", "Denied")));
        var controller = Controller(service, "notifications.view");

        var result = Assert.IsType<ObjectResult>(await controller.Get(1, 20, default));
        Assert.Equal(StatusCodes.Status403Forbidden, result.StatusCode);
    }

    private static PosNotificationsController Controller(
        IPosNotificationService service,
        string permission)
    {
        var controller = new PosNotificationsController(service, new TenantRequestContextFactory());
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim("sub", Guid.NewGuid().ToString()),
                new Claim("tenant_id", Guid.NewGuid().ToString()),
                new Claim("permissions", permission)
            ], "Test"));
        return controller;
    }

    private sealed class FakeService : IPosNotificationService
    {
        private readonly ApplicationResult<PosNotificationInboxResponseDto> _result;
        public FakeService(ApplicationResult<PosNotificationInboxResponseDto> result) => _result = result;
        public Task<ApplicationResult<PosNotificationInboxResponseDto>> GetInboxAsync(
            TenantRequestContext context, int page, int pageSize,
            CancellationToken cancellationToken) => Task.FromResult(_result);
    }
}
