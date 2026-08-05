using E_POS.Application.Modules.Shared.Notification.Dtos;

namespace E_POS.Application.Modules.Shared.Notification.Contracts.Repositories;

public interface INotificationRepository
{
    Task<NotificationEventTypeInfo> GetOrCreateSystemEventTypeAsync(
        string eventCode,
        string eventName,
        string sourceModule,
        string defaultPriority,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task<NotificationChannelInfo> GetOrCreateSystemChannelAsync(
        string channelType,
        string channelCode,
        string channelName,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task<NotificationEventInfo?> FindEventByNumberAsync(
        Guid tenantId,
        string eventNumber,
        CancellationToken cancellationToken);

    Task<NotificationEventInfo> AddEventAsync(
        NotificationEventCreateData data,
        CancellationToken cancellationToken);

    Task<bool> MessageExistsAsync(
        Guid tenantId,
        string messageNumber,
        CancellationToken cancellationToken);

    Task<NotificationMessageInfo> AddMessageAsync(
        NotificationMessageCreateData data,
        CancellationToken cancellationToken);

    Task AddInboxItemAsync(
        NotificationInboxItemCreateData data,
        CancellationToken cancellationToken);

    Task<NotificationInboxQueryResult> GetCustomerInboxAsync(
        Guid tenantId,
        Guid customerId,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<int> GetCustomerUnreadCountAsync(
        Guid tenantId,
        Guid customerId,
        CancellationToken cancellationToken);

    Task<NotificationInboxItemProjection?> MarkCustomerInboxItemReadAsync(
        Guid tenantId,
        Guid customerId,
        Guid inboxItemId,
        DateTimeOffset now,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken);

    Task<int> MarkAllCustomerInboxItemsReadAsync(
        Guid tenantId,
        Guid customerId,
        DateTimeOffset now,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public sealed record NotificationEventTypeInfo(Guid Id, string EventCode, string DefaultPriority);
public sealed record NotificationChannelInfo(Guid Id, string ChannelType, string ChannelCode);
public sealed record NotificationEventInfo(Guid Id, string EventNumber, string EventCode);
public sealed record NotificationMessageInfo(Guid Id, string MessageNumber);

public sealed record NotificationEventCreateData(
    Guid TenantId,
    Guid NotificationEventTypeId,
    string EventNumber,
    string EventCode,
    string SourceModule,
    string? SourceReferenceType,
    Guid? SourceReferenceId,
    string Priority,
    DateTimeOffset CreatedAt,
    Guid? CreatedByTenantUserId,
    Guid? CreatedByPlatformUserId);

public sealed record NotificationMessageCreateData(
    Guid TenantId,
    Guid NotificationEventId,
    Guid NotificationChannelId,
    string MessageNumber,
    string MessageType,
    string ChannelType,
    NotificationRecipientDto Recipient,
    NotificationContentDto Content,
    string Priority,
    string MessageStatus,
    DateTimeOffset CreatedAt,
    DateTimeOffset? DeliveredAt);

public sealed record NotificationInboxItemCreateData(
    Guid TenantId,
    Guid NotificationMessageId,
    NotificationRecipientDto Recipient,
    NotificationContentDto Content,
    string InboxStatus,
    DateTimeOffset CreatedAt,
    DateTimeOffset? DeliveredAt);

public sealed record NotificationInboxItemProjection(
    Guid Id,
    Guid MessageId,
    string EventCode,
    string? SourceModule,
    string? SourceReferenceType,
    Guid? SourceReferenceId,
    string? TitleText,
    string? BodyText,
    string? LinkUrl,
    string InboxStatus,
    DateTimeOffset CreatedAt,
    DateTimeOffset? DeliveredAt,
    DateTimeOffset? ReadAt);

public sealed record NotificationInboxQueryResult(
    IReadOnlyList<NotificationInboxItemProjection> Items,
    int TotalCount);