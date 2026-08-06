using E_POS.Application.Modules.Shared.Notification.Channels;
using E_POS.Application.Modules.Shared.Notification.Constants;
using E_POS.Application.Modules.Shared.Notification.Contracts.Repositories;
using E_POS.Application.Modules.Shared.Notification.Contracts.Services;
using E_POS.Application.Modules.Shared.Notification.Dtos;
using Xunit;

namespace E_POS.UnitTests.Shared.Notification;

public sealed class InAppNotificationChannelHandlerTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid CustomerId = Guid.NewGuid();
    private static readonly Guid EventId = Guid.NewGuid();
    private static readonly Guid ChannelId = Guid.NewGuid();
    private static readonly Guid MessageId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 8, 4, 11, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task HandleAsync_CreatesDeliveredMessageAndUnreadInboxItem()
    {
        var repository = new FakeNotificationRepository();
        var handler = new InAppNotificationChannelHandler(repository);

        var result = await handler.HandleAsync(Context(priority: "invalid"), CancellationToken.None);

        Assert.Equal(1, result.CreatedMessageCount);
        Assert.Equal(NotificationChannelTypes.InApp, repository.ChannelType);
        Assert.NotNull(repository.CreatedMessage);
        Assert.Equal("ECOM-ORDER-READY-001-INAPP", repository.CreatedMessage.MessageNumber);
        Assert.Equal("DELIVERED", repository.CreatedMessage.MessageStatus);
        Assert.Equal(NotificationPriorities.Normal, repository.CreatedMessage.Priority);
        Assert.NotNull(repository.CreatedInboxItem);
        Assert.Equal(MessageId, repository.CreatedInboxItem.NotificationMessageId);
        Assert.Equal("UNREAD", repository.CreatedInboxItem.InboxStatus);
    }

    [Fact]
    public async Task HandleAsync_ExistingMessage_DoesNotCreateDuplicateInboxItem()
    {
        var repository = new FakeNotificationRepository { MessageAlreadyExists = true };
        var handler = new InAppNotificationChannelHandler(repository);

        var result = await handler.HandleAsync(Context(), CancellationToken.None);

        Assert.Equal(0, result.CreatedMessageCount);
        Assert.Null(repository.CreatedMessage);
        Assert.Null(repository.CreatedInboxItem);
    }

    private static NotificationChannelContext Context(string priority = NotificationPriorities.High) => new(
        EventId,
        "ECOM-ORDER-READY-001",
        new CreateNotificationEventRequest
        {
            TenantId = TenantId,
            EventCode = "ecommerce.order_ready_for_collection",
            EventName = "Order ready",
            SourceModule = "ECommerce",
            SourceReferenceType = "SALES_ORDER",
            SourceReferenceId = Guid.NewGuid(),
            EventNumber = "ECOM-ORDER-READY-001",
            Priority = priority,
            Recipient = new NotificationRecipientDto
            {
                RecipientType = NotificationRecipientTypes.Customer,
                CustomerId = CustomerId
            },
            Content = new NotificationContentDto
            {
                Title = "Order ready",
                Body = "Your order is ready.",
                ActionUrl = "/orders/1"
            }
        },
        Now);

    private sealed class FakeNotificationRepository : INotificationRepository
    {
        public bool MessageAlreadyExists { get; init; }
        public string? ChannelType { get; private set; }
        public NotificationMessageCreateData? CreatedMessage { get; private set; }
        public NotificationInboxItemCreateData? CreatedInboxItem { get; private set; }

        public Task<NotificationEventTypeInfo> GetOrCreateSystemEventTypeAsync(string eventCode, string eventName, string sourceModule, string defaultPriority, DateTimeOffset now, CancellationToken cancellationToken) =>
            Task.FromResult(new NotificationEventTypeInfo(Guid.NewGuid(), eventCode, defaultPriority));

        public Task<NotificationChannelInfo> GetOrCreateSystemChannelAsync(
            string channelType,
            string channelCode,
            string channelName,
            DateTimeOffset now,
            CancellationToken cancellationToken)
        {
            ChannelType = channelType;
            return Task.FromResult(new NotificationChannelInfo(ChannelId, channelType, channelCode));
        }

        public Task<NotificationEventInfo?> FindEventByNumberAsync(Guid tenantId, string eventNumber, CancellationToken cancellationToken) =>
            Task.FromResult<NotificationEventInfo?>(null);

        public Task<NotificationEventInfo> AddEventAsync(NotificationEventCreateData data, CancellationToken cancellationToken) =>
            Task.FromResult(new NotificationEventInfo(EventId, data.EventNumber, data.EventCode));

        public Task<bool> MessageExistsAsync(Guid tenantId, string messageNumber, CancellationToken cancellationToken) =>
            Task.FromResult(MessageAlreadyExists);

        public Task<NotificationMessageInfo> AddMessageAsync(NotificationMessageCreateData data, CancellationToken cancellationToken)
        {
            CreatedMessage = data;
            return Task.FromResult(new NotificationMessageInfo(MessageId, data.MessageNumber));
        }

        public Task AddInboxItemAsync(NotificationInboxItemCreateData data, CancellationToken cancellationToken)
        {
            CreatedInboxItem = data;
            return Task.CompletedTask;
        }

        public Task<NotificationInboxQueryResult> GetCustomerInboxAsync(Guid tenantId, Guid customerId, int page, int pageSize, CancellationToken cancellationToken) =>
            Task.FromResult(new NotificationInboxQueryResult([], 0));

        public Task<int> GetCustomerUnreadCountAsync(Guid tenantId, Guid customerId, CancellationToken cancellationToken) =>
            Task.FromResult(0);

        public Task<NotificationInboxItemProjection?> MarkCustomerInboxItemReadAsync(Guid tenantId, Guid customerId, Guid inboxItemId, DateTimeOffset now, string? ipAddress, string? userAgent, CancellationToken cancellationToken) =>
            Task.FromResult<NotificationInboxItemProjection?>(null);

        public Task<int> MarkAllCustomerInboxItemsReadAsync(Guid tenantId, Guid customerId, DateTimeOffset now, string? ipAddress, string? userAgent, CancellationToken cancellationToken) =>
            Task.FromResult(0);

        public Task SaveChangesAsync(CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }
}