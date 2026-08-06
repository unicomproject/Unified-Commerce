using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Shared.Notification.Entities;

public class NotificationMessage : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid NotificationEventId { get; protected set; }
    public Guid? NotificationTemplateVersionId { get; protected set; }
    public Guid NotificationChannelId { get; protected set; }
    public string MessageNumber { get; protected set; } = string.Empty;
    public string MessageType { get; protected set; } = string.Empty;
    public string ChannelType { get; protected set; } = string.Empty;
    public string RecipientType { get; protected set; } = string.Empty;
    public Guid? PlatformUserId { get; protected set; }
    public Guid? TenantUserId { get; protected set; }
    public Guid? CustomerId { get; protected set; }
    public string? RecipientName { get; protected set; }
    public string? RecipientEmail { get; protected set; }
    public string? RecipientPhone { get; protected set; }
    public string? RecipientAddressJson { get; protected set; }
    public string? TitleText { get; protected set; }
    public string? BodyTextMapped { get; protected set; }
    public string? BodyHtmlMapped { get; protected set; }
    public string? ActionUrlMapped { get; protected set; }
    public string Priority { get; protected set; } = string.Empty;
    public string MessageStatus { get; protected set; } = string.Empty;
    public DateTimeOffset? ScheduledAt { get; protected set; }
    public DateTimeOffset? SentAt { get; protected set; }
    public DateTimeOffset? DeliveredAt { get; protected set; }
    public DateTimeOffset? FailedAt { get; protected set; }
    public string? FailureReason { get; protected set; }

    public static NotificationMessage Create(
        Guid tenantId,
        Guid notificationEventId,
        Guid notificationChannelId,
        string messageNumber,
        string messageType,
        string channelType,
        string recipientType,
        Guid? platformUserId,
        Guid? tenantUserId,
        Guid? customerId,
        string? recipientName,
        string? recipientEmail,
        string? recipientPhone,
        string? titleText,
        string? bodyText,
        string? actionUrl,
        string priority,
        string messageStatus,
        DateTimeOffset createdAt,
        DateTimeOffset? deliveredAt)
    {
        return new NotificationMessage
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            NotificationEventId = notificationEventId,
            NotificationChannelId = notificationChannelId,
            MessageNumber = messageNumber.Trim().ToUpperInvariant(),
            MessageType = messageType.Trim().ToUpperInvariant(),
            ChannelType = channelType.Trim().ToUpperInvariant(),
            RecipientType = recipientType.Trim().ToUpperInvariant(),
            PlatformUserId = platformUserId,
            TenantUserId = tenantUserId,
            CustomerId = customerId,
            RecipientName = TrimToNull(recipientName),
            RecipientEmail = TrimToNull(recipientEmail),
            RecipientPhone = TrimToNull(recipientPhone),
            TitleText = TrimToNull(titleText),
            BodyTextMapped = TrimToNull(bodyText),
            ActionUrlMapped = TrimToNull(actionUrl),
            Priority = priority.Trim().ToUpperInvariant(),
            MessageStatus = messageStatus.Trim().ToUpperInvariant(),
            CreatedAt = createdAt,
            DeliveredAt = deliveredAt
        };
    }

    private static string? TrimToNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}