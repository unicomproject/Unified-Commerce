using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Shared.Notification.Contracts.Repositories;
using E_POS.Application.Modules.Shared.Notification.Contracts.Services;
using E_POS.Application.Modules.Shared.Notification.Dtos;
using E_POS.Application.Modules.Tenant.POSOperations.Contracts;
using E_POS.Application.Modules.Tenant.POSOperations.Services;
using E_POS.Domain.Modules.Tenant.POSOperations.Constants;
using Xunit;

namespace E_POS.UnitTests.POSOperations;

public sealed class PosNotificationServiceTests
{
    [Fact]
    public async Task MissingCapability_FailsBeforeRepositoryQuery()
    {
        var repository = new FakeRepository();
        var result = await Service(repository).GetInboxAsync(
            Context(PosNotificationSourceAccess.SalesPermission), 1, 20, default);

        Assert.False(result.IsSuccess);
        Assert.Equal("pos_notifications.permission_denied", result.Error.Code);
        Assert.False(repository.Called);
    }

    [Theory]
    [InlineData(PosNotificationSourceAccess.SalesPermission, "POS", "Sales")]
    [InlineData(PosNotificationSourceAccess.OnlineOrderPermission, "ECommerce", null)]
    [InlineData(PosNotificationSourceAccess.ReturnPermission, "Returns", "Refunds")]
    public async Task FeaturePermission_LimitsSourceModules(
        string featurePermission,
        string expected,
        string? secondExpected)
    {
        var repository = new FakeRepository { UnreadCount = 3 };
        var result = await Service(repository).GetInboxAsync(
            Context(PosPermissions.Notifications.View, featurePermission), 1, 20, default);

        Assert.True(result.IsSuccess);
        Assert.Contains(expected, repository.AllowedSources);
        if (secondExpected is not null)
            Assert.Contains(secondExpected, repository.AllowedSources);
        Assert.Equal(3, result.Value!.UnreadCount);
    }

    [Fact]
    public async Task MultiDomainUser_GetsCombinedSourceFilter()
    {
        var repository = new FakeRepository();
        await Service(repository).GetInboxAsync(
            Context(
                PosPermissions.Notifications.View,
                PosNotificationSourceAccess.SalesPermission,
                PosNotificationSourceAccess.OnlineOrderPermission,
                PosNotificationSourceAccess.ReturnPermission),
            1, 20, default);

        Assert.Equal(5, repository.AllowedSources.Count);
    }

    [Fact]
    public async Task LegacyNotificationPermission_DoesNotAuthorizeEndpoint()
    {
        var result = await Service(new FakeRepository()).GetInboxAsync(
            Context("notifications.view", PosNotificationSourceAccess.SalesPermission),
            1, 20, default);
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task MarkReadAsync_MissingPermission_FailsBeforeDelegating()
    {
        var inbox = new FakeInboxService();
        var result = await Service(new FakeRepository(), inbox).MarkReadAsync(
            Context(PosNotificationSourceAccess.SalesPermission), Guid.NewGuid(), null, null, default);

        Assert.False(result.IsSuccess);
        Assert.Equal("pos_notifications.permission_denied", result.Error.Code);
        Assert.False(inbox.MarkReadCalled);
    }

    [Fact]
    public async Task MarkReadAsync_Authorized_DelegatesWithTenantAndUserScope()
    {
        var inbox = new FakeInboxService();
        var context = Context(PosPermissions.Notifications.View);
        var notificationId = Guid.NewGuid();

        var result = await Service(new FakeRepository(), inbox).MarkReadAsync(
            context, notificationId, "203.0.113.1", "test-agent", default);

        Assert.True(result.IsSuccess);
        Assert.True(inbox.MarkReadCalled);
        Assert.Equal(context.TenantId, inbox.LastTenantId);
        Assert.Equal(context.UserId, inbox.LastTenantUserId);
        Assert.Equal(notificationId, inbox.LastInboxItemId);
    }

    [Fact]
    public async Task MarkAllReadAsync_MissingPermission_FailsBeforeDelegating()
    {
        var inbox = new FakeInboxService();
        var result = await Service(new FakeRepository(), inbox).MarkAllReadAsync(
            Context(PosNotificationSourceAccess.SalesPermission), null, null, default);

        Assert.False(result.IsSuccess);
        Assert.Equal("pos_notifications.permission_denied", result.Error.Code);
        Assert.False(inbox.MarkAllReadCalled);
    }

    [Fact]
    public async Task MarkAllReadAsync_Authorized_DelegatesWithTenantAndUserScope()
    {
        var inbox = new FakeInboxService();
        var context = Context(PosPermissions.Notifications.View);

        var result = await Service(new FakeRepository(), inbox).MarkAllReadAsync(
            context, null, null, default);

        Assert.True(result.IsSuccess);
        Assert.True(inbox.MarkAllReadCalled);
        Assert.Equal(context.TenantId, inbox.LastTenantId);
        Assert.Equal(context.UserId, inbox.LastTenantUserId);
    }

    private static PosNotificationService Service(
        IPosNotificationRepository repository, INotificationInboxService? inboxService = null) =>
        new(repository, inboxService ?? new FakeInboxService());

    private static TenantRequestContext Context(params string[] permissions) =>
        new(Guid.NewGuid(), Guid.NewGuid(), permissions);

    private sealed class FakeRepository : IPosNotificationRepository
    {
        public bool Called { get; private set; }
        public int UnreadCount { get; init; }
        public IReadOnlyCollection<string> AllowedSources { get; private set; } = [];

        public Task<NotificationInboxQueryResult> GetTenantUserInboxAsync(
            Guid tenantId, Guid tenantUserId,
            IReadOnlyCollection<string> allowedSourceModules,
            int page, int pageSize, CancellationToken cancellationToken)
        {
            Called = true;
            AllowedSources = allowedSourceModules;
            return Task.FromResult(new NotificationInboxQueryResult([], 0));
        }

        public Task<int> GetTenantUserUnreadCountAsync(
            Guid tenantId, Guid tenantUserId,
            IReadOnlyCollection<string> allowedSourceModules,
            CancellationToken cancellationToken)
        {
            AllowedSources = allowedSourceModules;
            return Task.FromResult(UnreadCount);
        }
    }

    private sealed class FakeInboxService : INotificationInboxService
    {
        public bool MarkReadCalled { get; private set; }
        public bool MarkAllReadCalled { get; private set; }
        public Guid LastTenantId { get; private set; }
        public Guid LastTenantUserId { get; private set; }
        public Guid LastInboxItemId { get; private set; }

        public Task<ApplicationResult<NotificationInboxListResponse>> GetCustomerInboxAsync(
            Guid tenantId, Guid customerId, int page, int pageSize, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ApplicationResult<NotificationUnreadCountResponse>> GetCustomerUnreadCountAsync(
            Guid tenantId, Guid customerId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ApplicationResult<NotificationMarkReadResponse>> MarkCustomerInboxItemReadAsync(
            Guid tenantId, Guid customerId, Guid inboxItemId, string? ipAddress, string? userAgent,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ApplicationResult<NotificationMarkAllReadResponse>> MarkAllCustomerInboxItemsReadAsync(
            Guid tenantId, Guid customerId, string? ipAddress, string? userAgent,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ApplicationResult<NotificationInboxListResponse>> GetTenantUserInboxAsync(
            Guid tenantId, Guid tenantUserId, int page, int pageSize, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ApplicationResult<NotificationUnreadCountResponse>> GetTenantUserUnreadCountAsync(
            Guid tenantId, Guid tenantUserId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ApplicationResult<NotificationMarkReadResponse>> MarkTenantUserInboxItemReadAsync(
            Guid tenantId, Guid tenantUserId, Guid inboxItemId, string? ipAddress, string? userAgent,
            CancellationToken cancellationToken)
        {
            MarkReadCalled = true;
            LastTenantId = tenantId;
            LastTenantUserId = tenantUserId;
            LastInboxItemId = inboxItemId;
            return Task.FromResult(ApplicationResult<NotificationMarkReadResponse>.Success(
                new NotificationMarkReadResponse { Id = inboxItemId, Status = "READ" }));
        }

        public Task<ApplicationResult<NotificationMarkAllReadResponse>> MarkAllTenantUserInboxItemsReadAsync(
            Guid tenantId, Guid tenantUserId, string? ipAddress, string? userAgent,
            CancellationToken cancellationToken)
        {
            MarkAllReadCalled = true;
            LastTenantId = tenantId;
            LastTenantUserId = tenantUserId;
            return Task.FromResult(ApplicationResult<NotificationMarkAllReadResponse>.Success(
                new NotificationMarkAllReadResponse { UpdatedCount = 1 }));
        }
    }
}
