using System.Security.Claims;
using E_POS.Api.Common;
using E_POS.Api.Controllers;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Shared.Notification.Dtos;
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
        var service = new FakeService(
            getInboxResult: ApplicationResult<PosNotificationInboxResponseDto>.Success(
                new PosNotificationInboxResponseDto([], 2, 1, 20, 0, 0)));
        var controller = Controller(service, "pos.notifications.alerts.view");

        var result = await controller.Get(1, 20, default);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task PermissionDenial_IsForbidden()
    {
        var service = new FakeService(
            getInboxResult: ApplicationResult<PosNotificationInboxResponseDto>.Failure(
                new ApplicationError("pos_notifications.permission_denied", "Denied")));
        var controller = Controller(service, "notifications.view");

        var result = Assert.IsType<ObjectResult>(await controller.Get(1, 20, default));
        Assert.Equal(StatusCodes.Status403Forbidden, result.StatusCode);
    }

    [Fact]
    public async Task MarkRead_Success_ReturnsOk()
    {
        var notificationId = Guid.NewGuid();
        var service = new FakeService(
            markReadResult: ApplicationResult<NotificationMarkReadResponse>.Success(
                new NotificationMarkReadResponse { Id = notificationId, Status = "READ" }));
        var controller = Controller(service, "pos.notifications.alerts.view");

        var result = await controller.MarkRead(notificationId, default);

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal(notificationId, service.LastMarkReadId);
    }

    [Fact]
    public async Task MarkRead_NotFound_Returns404()
    {
        var service = new FakeService(
            markReadResult: ApplicationResult<NotificationMarkReadResponse>.Failure(
                new ApplicationError("notifications.not_found", "Not found")));
        var controller = Controller(service, "pos.notifications.alerts.view");

        var result = await controller.MarkRead(Guid.NewGuid(), default);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task MarkRead_PermissionDenied_IsForbidden()
    {
        var service = new FakeService(
            markReadResult: ApplicationResult<NotificationMarkReadResponse>.Failure(
                new ApplicationError("pos_notifications.permission_denied", "Denied")));
        var controller = Controller(service, "notifications.view");

        var result = Assert.IsType<ObjectResult>(await controller.MarkRead(Guid.NewGuid(), default));
        Assert.Equal(StatusCodes.Status403Forbidden, result.StatusCode);
    }

    [Fact]
    public async Task MarkAllRead_Success_ReturnsOk()
    {
        var service = new FakeService(
            markAllReadResult: ApplicationResult<NotificationMarkAllReadResponse>.Success(
                new NotificationMarkAllReadResponse { UpdatedCount = 4 }));
        var controller = Controller(service, "pos.notifications.alerts.view");

        var result = await controller.MarkAllRead(default);

        Assert.IsType<OkObjectResult>(result);
        Assert.True(service.MarkAllReadCalled);
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
        private readonly ApplicationResult<PosNotificationInboxResponseDto>? _getInboxResult;
        private readonly ApplicationResult<NotificationMarkReadResponse>? _markReadResult;
        private readonly ApplicationResult<NotificationMarkAllReadResponse>? _markAllReadResult;

        public Guid? LastMarkReadId { get; private set; }
        public bool MarkAllReadCalled { get; private set; }

        public FakeService(
            ApplicationResult<PosNotificationInboxResponseDto>? getInboxResult = null,
            ApplicationResult<NotificationMarkReadResponse>? markReadResult = null,
            ApplicationResult<NotificationMarkAllReadResponse>? markAllReadResult = null)
        {
            _getInboxResult = getInboxResult;
            _markReadResult = markReadResult;
            _markAllReadResult = markAllReadResult;
        }

        public Task<ApplicationResult<PosNotificationInboxResponseDto>> GetInboxAsync(
            TenantRequestContext context, int page, int pageSize,
            CancellationToken cancellationToken) => Task.FromResult(_getInboxResult!);

        public Task<ApplicationResult<NotificationMarkReadResponse>> MarkReadAsync(
            TenantRequestContext context, Guid notificationId, string? ipAddress, string? userAgent,
            CancellationToken cancellationToken)
        {
            LastMarkReadId = notificationId;
            return Task.FromResult(_markReadResult!);
        }

        public Task<ApplicationResult<NotificationMarkAllReadResponse>> MarkAllReadAsync(
            TenantRequestContext context, string? ipAddress, string? userAgent,
            CancellationToken cancellationToken)
        {
            MarkAllReadCalled = true;
            return Task.FromResult(_markAllReadResult!);
        }
    }
}
