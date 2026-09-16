using E_POS.Application.Modules.Shared.Notification.Channels;
using E_POS.Application.Modules.Shared.Notification.Constants;
using E_POS.Application.Modules.Shared.Notification.Contracts.Services;
using E_POS.Application.Modules.Shared.Notification.Dtos;
using Xunit;

namespace E_POS.UnitTests.Shared.Notification;

public sealed class RealtimeNotificationChannelHandlerTests
{
    [Fact]
    public async Task HandleAsync_TenantUser_PublishesCanonicalPayloadToThatUserOnly()
    {
        var publisher = new FakePublisher();
        var handler = new RealtimeNotificationChannelHandler(publisher);
        var tenantUserId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var now = new DateTimeOffset(2026, 9, 9, 12, 0, 0, TimeSpan.Zero);
        var request = new CreateNotificationEventRequest
        {
            TenantId = Guid.NewGuid(),
            EventCode = "ecommerce.order_placed.staff",
            EventName = "E-commerce order placed (staff)",
            SourceModule = "ECommerce",
            SourceReferenceType = "SALES_ORDER",
            SourceReferenceId = orderId,
            EventNumber = "ECOM-STF-1",
            Priority = NotificationPriorities.Normal,
            Recipient = new NotificationRecipientDto
            {
                RecipientType = NotificationRecipientTypes.TenantUser,
                TenantUserId = tenantUserId
            },
            Content = new NotificationContentDto
            {
                Title = "New order placed",
                Body = "Order ORD-000001 has been placed",
                ActionUrl = $"/orders/{orderId:N}"
            }
        };
        var context = new NotificationChannelContext(
            Guid.NewGuid(),
            "ECOM-STF-1",
            request,
            now);

        var result = await handler.HandleAsync(context, CancellationToken.None);

        Assert.Equal(NotificationChannelTypes.Push, handler.ChannelType);
        Assert.Equal(0, result.CreatedMessageCount);
        Assert.Single(publisher.Calls);
        Assert.Equal(tenantUserId, publisher.Calls[0].TenantUserId);
        Assert.Equal("ecommerce.order_placed.staff", publisher.Calls[0].Payload.EventCode);
        Assert.Equal(orderId, publisher.Calls[0].Payload.SourceReferenceId);
    }

    [Fact]
    public async Task HandleAsync_CustomerRecipient_DoesNotPublish()
    {
        var publisher = new FakePublisher();
        var handler = new RealtimeNotificationChannelHandler(publisher);
        var context = new NotificationChannelContext(
            Guid.NewGuid(),
            "ECOM-1",
            new CreateNotificationEventRequest
            {
                TenantId = Guid.NewGuid(),
                EventCode = "ecommerce.order_placed",
                EventName = "E-commerce order placed",
                SourceModule = "ECommerce",
                SourceReferenceType = "SALES_ORDER",
                SourceReferenceId = Guid.NewGuid(),
                EventNumber = "ECOM-1",
                Priority = NotificationPriorities.Normal,
                Recipient = new NotificationRecipientDto
                {
                    RecipientType = NotificationRecipientTypes.Customer,
                    CustomerId = Guid.NewGuid()
                },
                Content = new NotificationContentDto
                {
                    Title = "Order placed",
                    Body = "Thanks"
                }
            },
            DateTimeOffset.UtcNow);

        await handler.HandleAsync(context, CancellationToken.None);

        Assert.Empty(publisher.Calls);
    }

    private sealed class FakePublisher : IRealtimeNotificationPublisher
    {
        public List<(Guid TenantUserId, RealtimeNotificationPayload Payload)> Calls { get; } = [];

        public Task PublishToTenantUserAsync(
            Guid tenantUserId,
            RealtimeNotificationPayload payload,
            CancellationToken cancellationToken)
        {
            Calls.Add((tenantUserId, payload));
            return Task.CompletedTask;
        }
    }
}
