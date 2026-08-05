using E_POS.Application.Common.Contracts;
using E_POS.Application.Modules.Shared.Notification.Contracts.Repositories;
using E_POS.Application.Modules.Shared.Notification.Dtos;
using E_POS.Application.Modules.Shared.Notification.Services;
using Xunit;

namespace E_POS.UnitTests.Shared.Notification;

public sealed class NotificationInboxServiceTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid CustomerId = Guid.NewGuid();
    private static readonly Guid InboxItemId = Guid.NewGuid();
    private static readonly Guid MessageId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 8, 4, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task GetCustomerInboxAsync_InvalidContext_DoesNotCallRepository()
    {
        var repository = new FakeNotificationRepository();
        var service = CreateService(repository);

        var result = await service.GetCustomerInboxAsync(Guid.Empty, CustomerId, 1, 20, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("notifications.invalid_customer_context", result.Error.Code);
        Assert.Equal(0, repository.GetInboxCallCount);
    }

    [Fact]
    public async Task GetCustomerInboxAsync_ClampsPagingAndMapsRows()
    {
        var repository = new FakeNotificationRepository
        {
            InboxResult = new NotificationInboxQueryResult(
            [
                new NotificationInboxItemProjection(
                    InboxItemId,
                    MessageId,
                    "ecommerce.order_ready_for_collection",
                    "ECommerce",
                    "SALES_ORDER",
                    Guid.NewGuid(),
                    "Order ready",
                    "Your order is ready.",
                    "/orders/1",
                    "READ",
                    Now.AddMinutes(-5),
                    Now.AddMinutes(-5),
                    Now)
            ], 125)
        };
        var service = CreateService(repository);

        var result = await service.GetCustomerInboxAsync(TenantId, CustomerId, -10, 500, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        var value = result.Value!;
        Assert.Equal(1, repository.Page);
        Assert.Equal(50, repository.PageSize);
        Assert.Equal(3, value.TotalPages);
        var item = Assert.Single(value.Items);
        Assert.Equal(InboxItemId, item.Id);
        Assert.True(item.IsRead);
        Assert.Equal("Order ready", item.Title);
    }

    [Fact]
    public async Task MarkCustomerInboxItemReadAsync_NotFound_ReturnsNotFoundWithoutSaving()
    {
        var repository = new FakeNotificationRepository { MarkReadResult = null };
        var service = CreateService(repository);

        var result = await service.MarkCustomerInboxItemReadAsync(
            TenantId,
            CustomerId,
            InboxItemId,
            " 127.0.0.1 ",
            " test-browser ",
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("notifications.not_found", result.Error.Code);
        Assert.Equal(0, repository.SaveChangesCallCount);
    }

    [Fact]
    public async Task MarkAllCustomerInboxItemsReadAsync_ForwardsAuditContextAndSaves()
    {
        var repository = new FakeNotificationRepository { MarkAllResult = 2 };
        var service = CreateService(repository);

        var result = await service.MarkAllCustomerInboxItemsReadAsync(
            TenantId,
            CustomerId,
            " 127.0.0.1 ",
            " test-browser ",
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        var value = result.Value!;
        Assert.Equal(2, value.UpdatedCount);
        Assert.Equal(Now, value.ReadAt);
        Assert.Equal("127.0.0.1", repository.IpAddress);
        Assert.Equal("test-browser", repository.UserAgent);
        Assert.Equal(1, repository.SaveChangesCallCount);
    }

    private static NotificationInboxService CreateService(FakeNotificationRepository repository) =>
        new(repository, new FakeDateTimeProvider());

    private sealed class FakeDateTimeProvider : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => Now;
    }

    private sealed class FakeNotificationRepository : INotificationRepository
    {
        public NotificationInboxQueryResult InboxResult { get; init; } = new([], 0);
        public NotificationInboxItemProjection? MarkReadResult { get; init; } = new(
            InboxItemId,
            MessageId,
            "ecommerce.order_ready_for_collection",
            "ECommerce",
            "SALES_ORDER",
            Guid.NewGuid(),
            "Order ready",
            "Your order is ready.",
            "/orders/1",
            "READ",
            Now,
            Now,
            Now);
        public int MarkAllResult { get; init; }
        public int GetInboxCallCount { get; private set; }
        public int SaveChangesCallCount { get; private set; }
        public int? Page { get; private set; }
        public int? PageSize { get; private set; }
        public string? IpAddress { get; private set; }
        public string? UserAgent { get; private set; }

        public Task<NotificationEventTypeInfo> GetOrCreateSystemEventTypeAsync(string eventCode, string eventName, string sourceModule, string defaultPriority, DateTimeOffset now, CancellationToken cancellationToken) =>
            Task.FromResult(new NotificationEventTypeInfo(Guid.NewGuid(), eventCode, defaultPriority));

        public Task<NotificationChannelInfo> GetOrCreateSystemChannelAsync(string channelType, string channelCode, string channelName, DateTimeOffset now, CancellationToken cancellationToken) =>
            Task.FromResult(new NotificationChannelInfo(Guid.NewGuid(), channelType, channelCode));

        public Task<NotificationEventInfo?> FindEventByNumberAsync(Guid tenantId, string eventNumber, CancellationToken cancellationToken) =>
            Task.FromResult<NotificationEventInfo?>(null);

        public Task<NotificationEventInfo> AddEventAsync(NotificationEventCreateData data, CancellationToken cancellationToken) =>
            Task.FromResult(new NotificationEventInfo(Guid.NewGuid(), data.EventNumber, data.EventCode));

        public Task<bool> MessageExistsAsync(Guid tenantId, string messageNumber, CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public Task<NotificationMessageInfo> AddMessageAsync(NotificationMessageCreateData data, CancellationToken cancellationToken) =>
            Task.FromResult(new NotificationMessageInfo(Guid.NewGuid(), data.MessageNumber));

        public Task AddInboxItemAsync(NotificationInboxItemCreateData data, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task<NotificationInboxQueryResult> GetCustomerInboxAsync(Guid tenantId, Guid customerId, int page, int pageSize, CancellationToken cancellationToken)
        {
            GetInboxCallCount++;
            Page = page;
            PageSize = pageSize;
            return Task.FromResult(InboxResult);
        }

        public Task<int> GetCustomerUnreadCountAsync(Guid tenantId, Guid customerId, CancellationToken cancellationToken) =>
            Task.FromResult(0);

        public Task<NotificationInboxItemProjection?> MarkCustomerInboxItemReadAsync(
            Guid tenantId,
            Guid customerId,
            Guid inboxItemId,
            DateTimeOffset now,
            string? ipAddress,
            string? userAgent,
            CancellationToken cancellationToken)
        {
            IpAddress = ipAddress;
            UserAgent = userAgent;
            return Task.FromResult(MarkReadResult);
        }

        public Task<int> MarkAllCustomerInboxItemsReadAsync(
            Guid tenantId,
            Guid customerId,
            DateTimeOffset now,
            string? ipAddress,
            string? userAgent,
            CancellationToken cancellationToken)
        {
            IpAddress = ipAddress;
            UserAgent = userAgent;
            return Task.FromResult(MarkAllResult);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveChangesCallCount++;
            return Task.CompletedTask;
        }
    }
}