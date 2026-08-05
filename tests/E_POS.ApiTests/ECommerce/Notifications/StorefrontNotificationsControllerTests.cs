using System.Reflection;
using System.Security.Claims;
using E_POS.Api.Controllers.V1.ECommerce.Notifications;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Shared.Notification.Contracts.Services;
using E_POS.Application.Modules.Shared.Notification.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace E_POS.ApiTests.ECommerce.Notifications;

public sealed class StorefrontNotificationsControllerTests
{
    [Fact]
    public async Task Get_AuthenticatedCustomer_UsesOnlyJwtTenantAndCustomerClaims()
    {
        var tenantId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var service = new FakeNotificationInboxService
        {
            InboxResult = ApplicationResult<NotificationInboxListResponse>.Success(new NotificationInboxListResponse())
        };
        var controller = CreateController(service, tenantId, customerId);

        var result = await controller.Get(2, 25, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal(tenantId, service.TenantId);
        Assert.Equal(customerId, service.CustomerId);
        Assert.Equal(2, service.Page);
        Assert.Equal(25, service.PageSize);
    }

    [Fact]
    public async Task Get_MissingCustomerClaims_ReturnsUnauthorizedWithoutCallingService()
    {
        var service = new FakeNotificationInboxService();
        var controller = CreateController(service);

        var result = await controller.Get(1, 20, CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal(0, service.CallCount);
    }

    [Fact]
    public async Task MarkRead_WhenServiceReturnsNotFound_ReturnsNotFound()
    {
        var service = new FakeNotificationInboxService
        {
            MarkReadResult = ApplicationResult<NotificationMarkReadResponse>.Failure(
                new ApplicationError("notifications.not_found", "Notification was not found."))
        };
        var controller = CreateController(service, Guid.NewGuid(), Guid.NewGuid());

        var result = await controller.MarkRead(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GetUnreadCount_AuthenticatedCustomer_ForwardsJwtContext()
    {
        var tenantId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var service = new FakeNotificationInboxService
        {
            UnreadCountResult = ApplicationResult<NotificationUnreadCountResponse>.Success(new NotificationUnreadCountResponse { UnreadCount = 3 })
        };
        var controller = CreateController(service, tenantId, customerId);

        var result = await controller.GetUnreadCount(CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal(tenantId, service.TenantId);
        Assert.Equal(customerId, service.CustomerId);
    }

    [Fact]
    public void Controller_RequiresCustomerOnlyPolicyAndExpectedRoutes()
    {
        var authorize = Assert.Single(
            typeof(StorefrontNotificationsController).GetCustomAttributes<AuthorizeAttribute>());
        Assert.Equal("CustomerOnly", authorize.Policy);

        var route = Assert.Single(
            typeof(StorefrontNotificationsController).GetCustomAttributes<RouteAttribute>());
        Assert.Equal("api/v1/ecommerce/storefront/notifications", route.Template);

        Assert.Single(typeof(StorefrontNotificationsController)
            .GetMethod(nameof(StorefrontNotificationsController.Get))!
            .GetCustomAttributes<HttpGetAttribute>());
        Assert.Equal(
            "unread-count",
            Assert.Single(typeof(StorefrontNotificationsController)
                .GetMethod(nameof(StorefrontNotificationsController.GetUnreadCount))!
                .GetCustomAttributes<HttpGetAttribute>()).Template);
        Assert.Equal(
            "{notificationId:guid}/read",
            Assert.Single(typeof(StorefrontNotificationsController)
                .GetMethod(nameof(StorefrontNotificationsController.MarkRead))!
                .GetCustomAttributes<HttpPutAttribute>()).Template);
        Assert.Equal(
            "read-all",
            Assert.Single(typeof(StorefrontNotificationsController)
                .GetMethod(nameof(StorefrontNotificationsController.MarkAllRead))!
                .GetCustomAttributes<HttpPutAttribute>()).Template);
    }

    private static StorefrontNotificationsController CreateController(
        FakeNotificationInboxService service,
        Guid? tenantId = null,
        Guid? customerId = null)
    {
        var context = new DefaultHttpContext();
        if (tenantId.HasValue && customerId.HasValue)
        {
            context.User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim("tenant_id", tenantId.Value.ToString()),
                new Claim("sub", customerId.Value.ToString()),
                new Claim("identity_type", "customer")
            ], "Test"));
        }

        return new StorefrontNotificationsController(service)
        {
            ControllerContext = new ControllerContext { HttpContext = context }
        };
    }

    private sealed class FakeNotificationInboxService : INotificationInboxService
    {
        public ApplicationResult<NotificationInboxListResponse> InboxResult { get; init; } =
            ApplicationResult<NotificationInboxListResponse>.Success(new NotificationInboxListResponse());
        public ApplicationResult<NotificationUnreadCountResponse> UnreadCountResult { get; init; } =
            ApplicationResult<NotificationUnreadCountResponse>.Success(new NotificationUnreadCountResponse());
        public ApplicationResult<NotificationMarkReadResponse> MarkReadResult { get; init; } =
            ApplicationResult<NotificationMarkReadResponse>.Success(new NotificationMarkReadResponse());
        public ApplicationResult<NotificationMarkAllReadResponse> MarkAllReadResult { get; init; } =
            ApplicationResult<NotificationMarkAllReadResponse>.Success(new NotificationMarkAllReadResponse());
        public int CallCount { get; private set; }
        public Guid? TenantId { get; private set; }
        public Guid? CustomerId { get; private set; }
        public int? Page { get; private set; }
        public int? PageSize { get; private set; }

        public Task<ApplicationResult<NotificationInboxListResponse>> GetCustomerInboxAsync(
            Guid tenantId,
            Guid customerId,
            int page,
            int pageSize,
            CancellationToken cancellationToken)
        {
            Capture(tenantId, customerId);
            Page = page;
            PageSize = pageSize;
            return Task.FromResult(InboxResult);
        }

        public Task<ApplicationResult<NotificationUnreadCountResponse>> GetCustomerUnreadCountAsync(
            Guid tenantId,
            Guid customerId,
            CancellationToken cancellationToken)
        {
            Capture(tenantId, customerId);
            return Task.FromResult(UnreadCountResult);
        }

        public Task<ApplicationResult<NotificationMarkReadResponse>> MarkCustomerInboxItemReadAsync(
            Guid tenantId,
            Guid customerId,
            Guid inboxItemId,
            string? ipAddress,
            string? userAgent,
            CancellationToken cancellationToken)
        {
            Capture(tenantId, customerId);
            return Task.FromResult(MarkReadResult);
        }

        public Task<ApplicationResult<NotificationMarkAllReadResponse>> MarkAllCustomerInboxItemsReadAsync(
            Guid tenantId,
            Guid customerId,
            string? ipAddress,
            string? userAgent,
            CancellationToken cancellationToken)
        {
            Capture(tenantId, customerId);
            return Task.FromResult(MarkAllReadResult);
        }

        private void Capture(Guid tenantId, Guid customerId)
        {
            CallCount++;
            TenantId = tenantId;
            CustomerId = customerId;
        }
    }
}