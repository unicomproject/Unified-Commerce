using E_POS.Application.Modules.Shared.Notification.Contracts.Repositories;
using E_POS.Application.Modules.Shared.Notification.Dtos;
using E_POS.Domain.Modules.Shared.Notification.Entities;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace E_POS.Infrastructure.Modules.Shared.Notification.Repositories;

public sealed class NotificationRepository : INotificationRepository
{
    private readonly EPosDbContext _dbContext;

    public NotificationRepository(EPosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<NotificationEventTypeInfo> GetOrCreateSystemEventTypeAsync(
        string eventCode,
        string eventName,
        string sourceModule,
        string defaultPriority,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var normalizedCode = eventCode.Trim();
        var existing = await _dbContext.NotificationEventTypes
            .FirstOrDefaultAsync(x =>
                x.TenantId == null &&
                x.EventCode == normalizedCode,
                cancellationToken);

        if (existing is null)
        {
            existing = NotificationEventType.CreateSystem(
                normalizedCode,
                eventName,
                sourceModule,
                defaultPriority,
                now);
            _dbContext.NotificationEventTypes.Add(existing);
        }

        return new NotificationEventTypeInfo(existing.Id, existing.EventCode, existing.DefaultPriority);
    }

    public async Task<NotificationChannelInfo> GetOrCreateSystemChannelAsync(
        string channelType,
        string channelCode,
        string channelName,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var normalizedCode = channelCode.Trim().ToUpperInvariant();
        var existing = await _dbContext.NotificationChannels
            .FirstOrDefaultAsync(x =>
                x.TenantId == null &&
                x.ChannelCode == normalizedCode,
                cancellationToken);

        if (existing is null)
        {
            existing = NotificationChannel.CreateSystem(
                normalizedCode,
                channelName,
                channelType,
                now);
            _dbContext.NotificationChannels.Add(existing);
        }

        return new NotificationChannelInfo(existing.Id, existing.ChannelType, existing.ChannelCode);
    }

    public async Task<NotificationEventInfo?> FindEventByNumberAsync(
        Guid tenantId,
        string eventNumber,
        CancellationToken cancellationToken)
    {
        var normalizedNumber = eventNumber.Trim().ToUpperInvariant();
        return await _dbContext.NotificationEvents
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.EventNumber == normalizedNumber)
            .Select(x => new NotificationEventInfo(x.Id, x.EventNumber, x.EventCode))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<NotificationEventInfo> AddEventAsync(
        NotificationEventCreateData data,
        CancellationToken cancellationToken)
    {
        var notificationEvent = NotificationEvent.CreateProcessed(
            data.TenantId,
            data.NotificationEventTypeId,
            data.EventNumber,
            data.EventCode,
            data.SourceModule,
            data.SourceReferenceType,
            data.SourceReferenceId,
            data.Priority,
            data.CreatedAt,
            data.CreatedByTenantUserId,
            data.CreatedByPlatformUserId);

        _dbContext.NotificationEvents.Add(notificationEvent);
        return Task.FromResult(new NotificationEventInfo(
            notificationEvent.Id,
            notificationEvent.EventNumber,
            notificationEvent.EventCode));
    }

    public Task<bool> MessageExistsAsync(
        Guid tenantId,
        string messageNumber,
        CancellationToken cancellationToken)
    {
        var normalizedNumber = messageNumber.Trim().ToUpperInvariant();
        return _dbContext.NotificationMessages
            .AsNoTracking()
            .AnyAsync(x => x.TenantId == tenantId && x.MessageNumber == normalizedNumber, cancellationToken);
    }

    public Task<NotificationMessageInfo> AddMessageAsync(
        NotificationMessageCreateData data,
        CancellationToken cancellationToken)
    {
        var message = NotificationMessage.Create(
            data.TenantId,
            data.NotificationEventId,
            data.NotificationChannelId,
            data.MessageNumber,
            data.MessageType,
            data.ChannelType,
            data.Recipient.RecipientType,
            data.Recipient.PlatformUserId,
            data.Recipient.TenantUserId,
            data.Recipient.CustomerId,
            data.Recipient.RecipientName,
            data.Recipient.RecipientEmail,
            data.Recipient.RecipientPhone,
            data.Content.Title,
            data.Content.Body,
            data.Content.ActionUrl,
            data.Priority,
            data.MessageStatus,
            data.CreatedAt,
            data.DeliveredAt);

        _dbContext.NotificationMessages.Add(message);
        return Task.FromResult(new NotificationMessageInfo(message.Id, message.MessageNumber));
    }

    public Task AddInboxItemAsync(
        NotificationInboxItemCreateData data,
        CancellationToken cancellationToken)
    {
        var item = NotificationInboxItem.Create(
            data.TenantId,
            data.NotificationMessageId,
            data.Recipient.RecipientType,
            data.Recipient.PlatformUserId,
            data.Recipient.TenantUserId,
            data.Recipient.CustomerId,
            data.Content.Title,
            data.Content.Body,
            data.Content.ActionUrl,
            data.InboxStatus,
            data.CreatedAt,
            data.DeliveredAt);

        _dbContext.NotificationInboxItems.Add(item);
        return Task.CompletedTask;
    }

    public async Task<NotificationInboxQueryResult> GetCustomerInboxAsync(
        Guid tenantId,
        Guid customerId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = CustomerInboxQuery(tenantId, customerId)
            .Where(x => x.InboxStatus != "DELETED");

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new NotificationInboxItemProjection(
                x.Id,
                x.MessageId,
                x.EventCode,
                x.SourceModule,
                x.SourceReferenceType,
                x.SourceReferenceId,
                x.TitleText,
                x.BodyText,
                x.LinkUrl,
                x.InboxStatus,
                x.CreatedAt,
                x.DeliveredAt,
                x.ReadAt))
            .ToListAsync(cancellationToken);

        return new NotificationInboxQueryResult(items, totalCount);
    }

    public Task<int> GetCustomerUnreadCountAsync(
        Guid tenantId,
        Guid customerId,
        CancellationToken cancellationToken) =>
        _dbContext.NotificationInboxItems
            .AsNoTracking()
            .CountAsync(x =>
                x.TenantId == tenantId &&
                x.CustomerId == customerId &&
                x.RecipientType == "CUSTOMER" &&
                x.InboxStatus == "UNREAD",
                cancellationToken);

    public async Task<NotificationInboxItemProjection?> MarkCustomerInboxItemReadAsync(
        Guid tenantId,
        Guid customerId,
        Guid inboxItemId,
        DateTimeOffset now,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken)
    {
        var item = await _dbContext.NotificationInboxItems
            .FirstOrDefaultAsync(x =>
                x.TenantId == tenantId &&
                x.CustomerId == customerId &&
                x.RecipientType == "CUSTOMER" &&
                x.Id == inboxItemId &&
                x.InboxStatus != "DELETED",
                cancellationToken);

        if (item is null)
            return null;

        if (!string.Equals(item.InboxStatus, "READ", StringComparison.OrdinalIgnoreCase))
        {
            item.MarkRead(now, ipAddress, userAgent);
            await AddReadReceiptIfMissingAsync(item, now, ipAddress, userAgent, cancellationToken);
        }

        return await ProjectInboxItemAsync(item, cancellationToken);
    }

    public async Task<int> MarkAllCustomerInboxItemsReadAsync(
        Guid tenantId,
        Guid customerId,
        DateTimeOffset now,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken)
    {
        var items = await _dbContext.NotificationInboxItems
            .Where(x =>
                x.TenantId == tenantId &&
                x.CustomerId == customerId &&
                x.RecipientType == "CUSTOMER" &&
                x.InboxStatus == "UNREAD")
            .ToListAsync(cancellationToken);

        foreach (var item in items)
        {
            item.MarkRead(now, ipAddress, userAgent);
            await AddReadReceiptIfMissingAsync(item, now, ipAddress, userAgent, cancellationToken);
        }

        return items.Count;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        _dbContext.SaveChangesAsync(cancellationToken);

    private async Task AddReadReceiptIfMissingAsync(
        NotificationInboxItem item,
        DateTimeOffset now,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken)
    {
        var exists = await _dbContext.NotificationReadReceipts
            .AnyAsync(x =>
                x.TenantId == item.TenantId &&
                x.NotificationInboxItemId == item.Id &&
                x.CustomerId == item.CustomerId,
                cancellationToken);

        if (exists)
            return;

        _dbContext.NotificationReadReceipts.Add(NotificationReadReceipt.Create(
            item.TenantId,
            item.Id,
            item.NotificationMessageId,
            item.RecipientType,
            item.PlatformUserId,
            item.TenantUserId,
            item.CustomerId,
            now,
            ipAddress,
            userAgent));
    }

    private async Task<NotificationInboxItemProjection> ProjectInboxItemAsync(
        NotificationInboxItem item,
        CancellationToken cancellationToken)
    {
        var message = await _dbContext.NotificationMessages
            .AsNoTracking()
            .Where(x => x.Id == item.NotificationMessageId)
            .Select(x => new { x.Id, x.NotificationEventId })
            .FirstAsync(cancellationToken);

        var notificationEvent = await _dbContext.NotificationEvents
            .AsNoTracking()
            .Where(x => x.Id == message.NotificationEventId)
            .Select(x => new
            {
                x.EventCode,
                x.SourceModule,
                x.SourceReferenceType,
                x.SourceReferenceId
            })
            .FirstAsync(cancellationToken);

        return new NotificationInboxItemProjection(
            item.Id,
            item.NotificationMessageId,
            notificationEvent.EventCode,
            notificationEvent.SourceModule,
            notificationEvent.SourceReferenceType,
            notificationEvent.SourceReferenceId,
            item.TitleText,
            item.BodyText,
            item.LinkUrl,
            item.InboxStatus,
            item.CreatedAt,
            item.DeliveredAt,
            item.ReadAt);
    }

    private IQueryable<CustomerInboxProjection> CustomerInboxQuery(Guid tenantId, Guid customerId) =>
        from inbox in _dbContext.NotificationInboxItems.AsNoTracking()
        join message in _dbContext.NotificationMessages.AsNoTracking()
            on inbox.NotificationMessageId equals message.Id
        join notificationEvent in _dbContext.NotificationEvents.AsNoTracking()
            on message.NotificationEventId equals notificationEvent.Id
        where inbox.TenantId == tenantId &&
              inbox.CustomerId == customerId &&
              inbox.RecipientType == "CUSTOMER"
        select new CustomerInboxProjection
        {
            Id = inbox.Id,
            MessageId = message.Id,
            EventCode = notificationEvent.EventCode,
            SourceModule = notificationEvent.SourceModule,
            SourceReferenceType = notificationEvent.SourceReferenceType,
            SourceReferenceId = notificationEvent.SourceReferenceId,
            TitleText = inbox.TitleText,
            BodyText = inbox.BodyText,
            LinkUrl = inbox.LinkUrl,
            InboxStatus = inbox.InboxStatus,
            CreatedAt = inbox.CreatedAt,
            DeliveredAt = inbox.DeliveredAt,
            ReadAt = inbox.ReadAt
        };

    private sealed class CustomerInboxProjection
    {
        public Guid Id { get; init; }
        public Guid MessageId { get; init; }
        public string EventCode { get; init; } = string.Empty;
        public string? SourceModule { get; init; }
        public string? SourceReferenceType { get; init; }
        public Guid? SourceReferenceId { get; init; }
        public string? TitleText { get; init; }
        public string? BodyText { get; init; }
        public string? LinkUrl { get; init; }
        public string InboxStatus { get; init; } = string.Empty;
        public DateTimeOffset CreatedAt { get; init; }
        public DateTimeOffset? DeliveredAt { get; init; }
        public DateTimeOffset? ReadAt { get; init; }
    }
}