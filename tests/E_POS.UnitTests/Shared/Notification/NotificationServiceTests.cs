using E_POS.Application.Common.Contracts;
using E_POS.Application.Modules.Shared.Notification.Constants;
using E_POS.Application.Modules.Shared.Notification.Contracts.Repositories;
using E_POS.Application.Modules.Shared.Notification.Contracts.Services;
using E_POS.Application.Modules.Shared.Notification.Dtos;
using E_POS.Application.Modules.Shared.Notification.Services;
using Xunit;

namespace E_POS.UnitTests.Shared.Notification;

public sealed class NotificationServiceTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid CustomerId = Guid.NewGuid();
    private static readonly Guid EventId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 8, 4, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task CreateAsync_InvalidTenant_DoesNotCallRepositoryOrHandlers()
    {
        var repository = new FakeNotificationRepository();
        var handler = new FakeChannelHandler();
        var service = CreateService(repository, handler);
        var request = ValidRequest().WithTenant(Guid.Empty);

        var result = await service.CreateAsync(request, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("notification.invalid_tenant", result.Error.Code);
        Assert.Equal(0, repository.CallCount);
        Assert.Equal(0, handler.CallCount);
    }

    [Fact]
    public async Task CreateAsync_ExistingEventNumber_ReturnsIdempotentResultWithoutCreatingMessages()
    {
        var repository = new FakeNotificationRepository
        {
            ExistingEvent = new NotificationEventInfo(EventId, "ECOM-ORDER-PLACED-001", "ecommerce.order_placed")
        };
        var handler = new FakeChannelHandler();
        var service = CreateService(repository, handler);

        var result = await service.CreateAsync(ValidRequest(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        var value = result.Value!;
        Assert.Equal(EventId, value.EventId);
        Assert.True(value.AlreadyExisted);
        Assert.Equal(0, value.CreatedMessageCount);
        Assert.Equal(0, repository.AddEventCallCount);
        Assert.Equal(0, handler.CallCount);
    }

    [Fact]
    public async Task CreateAsync_ValidCustomerNotification_NormalizesPriorityAndRunsChannelHandlers()
    {
        var repository = new FakeNotificationRepository();
        var handler = new FakeChannelHandler { Result = new NotificationChannelHandleResult(1) };
        var service = CreateService(repository, handler);

        var result = await service.CreateAsync(ValidRequest(priority: "not-valid"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        var value = result.Value!;
        Assert.False(value.AlreadyExisted);
        Assert.Equal(1, value.CreatedMessageCount);
        Assert.Equal("ECOM-ORDER-PLACED-001", repository.EventNumberQueried);
        Assert.Equal(NotificationPriorities.Normal, repository.EventTypeDefaultPriority);
        Assert.Equal(NotificationPriorities.Normal, repository.CreatedEvent?.Priority);
        Assert.Equal(1, handler.CallCount);
        Assert.Equal(EventId, handler.Context?.EventId);
        Assert.Equal("ECOM-ORDER-PLACED-001", handler.Context?.EventNumber);
        Assert.Equal(1, repository.SaveChangesCallCount);
    }

    private static NotificationService CreateService(
        FakeNotificationRepository repository,
        params INotificationChannelHandler[] handlers) =>
        new(repository, handlers, new FakeDateTimeProvider());

    private static CreateNotificationEventRequest ValidRequest(string priority = NotificationPriorities.High) => new()
    {
        TenantId = TenantId,
        EventCode = "ecommerce.order_placed",
        EventName = "Order placed",
        SourceModule = "ECommerce",
        SourceReferenceType = "SALES_ORDER",
        SourceReferenceId = Guid.NewGuid(),
        EventNumber = "ECOM-ORDER-PLACED-001",
        Priority = priority,
        Recipient = new NotificationRecipientDto
        {
            RecipientType = NotificationRecipientTypes.Customer,
            CustomerId = CustomerId
        },
        Content = new NotificationContentDto
        {
            Title = "Order placed",
            Body = "Your order has been placed.",
            ActionUrl = "/orders/1"
        }
    };

    private sealed class FakeDateTimeProvider : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => Now;
    }

    private sealed class FakeChannelHandler : INotificationChannelHandler
    {
        public string ChannelType => NotificationChannelTypes.InApp;
        public NotificationChannelHandleResult Result { get; init; } = NotificationChannelHandleResult.None;
        public int CallCount { get; private set; }
        public NotificationChannelContext? Context { get; private set; }

        public Task<NotificationChannelHandleResult> HandleAsync(
            NotificationChannelContext context,
            CancellationToken cancellationToken)
        {
            CallCount++;
            Context = context;
            return Task.FromResult(Result);
        }
    }

    private sealed class FakeNotificationRepository : INotificationRepository
    {
        public NotificationEventInfo? ExistingEvent { get; init; }
        public int CallCount { get; private set; }
        public int AddEventCallCount { get; private set; }
        public int SaveChangesCallCount { get; private set; }
        public string? EventNumberQueried { get; private set; }
        public string? EventTypeDefaultPriority { get; private set; }
        public NotificationEventCreateData? CreatedEvent { get; private set; }

        public Task<NotificationEventTypeInfo> GetOrCreateSystemEventTypeAsync(
            string eventCode,
            string eventName,
            string sourceModule,
            string defaultPriority,
            DateTimeOffset now,
            CancellationToken cancellationToken)
        {
            CallCount++;
            EventTypeDefaultPriority = defaultPriority;
            return Task.FromResult(new NotificationEventTypeInfo(Guid.NewGuid(), eventCode, defaultPriority));
        }

        public Task<NotificationChannelInfo> GetOrCreateSystemChannelAsync(
            string channelType,
            string channelCode,
            string channelName,
            DateTimeOffset now,
            CancellationToken cancellationToken) =>
            Task.FromResult(new NotificationChannelInfo(Guid.NewGuid(), channelType, channelCode));

        public Task<NotificationEventInfo?> FindEventByNumberAsync(
            Guid tenantId,
            string eventNumber,
            CancellationToken cancellationToken)
        {
            CallCount++;
            EventNumberQueried = eventNumber;
            return Task.FromResult(ExistingEvent);
        }

        public Task<NotificationEventInfo> AddEventAsync(
            NotificationEventCreateData data,
            CancellationToken cancellationToken)
        {
            CallCount++;
            AddEventCallCount++;
            CreatedEvent = data;
            return Task.FromResult(new NotificationEventInfo(EventId, data.EventNumber, data.EventCode));
        }

        public Task<bool> MessageExistsAsync(Guid tenantId, string messageNumber, CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public Task<NotificationMessageInfo> AddMessageAsync(NotificationMessageCreateData data, CancellationToken cancellationToken) =>
            Task.FromResult(new NotificationMessageInfo(Guid.NewGuid(), data.MessageNumber));

        public Task AddInboxItemAsync(NotificationInboxItemCreateData data, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task<NotificationInboxQueryResult> GetCustomerInboxAsync(Guid tenantId, Guid customerId, int page, int pageSize, CancellationToken cancellationToken) =>
            Task.FromResult(new NotificationInboxQueryResult([], 0));

        public Task<int> GetCustomerUnreadCountAsync(Guid tenantId, Guid customerId, CancellationToken cancellationToken) =>
            Task.FromResult(0);

        public Task<NotificationInboxItemProjection?> MarkCustomerInboxItemReadAsync(
            Guid tenantId,
            Guid customerId,
            Guid inboxItemId,
            DateTimeOffset now,
            string? ipAddress,
            string? userAgent,
            CancellationToken cancellationToken) =>
            Task.FromResult<NotificationInboxItemProjection?>(null);

        public Task<int> MarkAllCustomerInboxItemsReadAsync(
            Guid tenantId,
            Guid customerId,
            DateTimeOffset now,
            string? ipAddress,
            string? userAgent,
            CancellationToken cancellationToken) =>
            Task.FromResult(0);

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveChangesCallCount++;
            return Task.CompletedTask;
        }
    }
}

internal static class NotificationTestRequestExtensions
{
    public static CreateNotificationEventRequest WithTenant(
        this CreateNotificationEventRequest request,
        Guid tenantId) => new()
    {
        TenantId = tenantId,
        EventCode = request.EventCode,
        EventName = request.EventName,
        SourceModule = request.SourceModule,
        SourceReferenceType = request.SourceReferenceType,
        SourceReferenceId = request.SourceReferenceId,
        EventNumber = request.EventNumber,
        Priority = request.Priority,
        Recipient = request.Recipient,
        Content = request.Content,
        CreatedByTenantUserId = request.CreatedByTenantUserId,
        CreatedByPlatformUserId = request.CreatedByPlatformUserId
    };
}