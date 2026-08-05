using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Shared.Notification.Entities;

public class NotificationInboxItem : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid NotificationMessageId { get; protected set; }
    public string RecipientType { get; protected set; } = string.Empty;
    public Guid? PlatformUserId { get; protected set; }
    public Guid? TenantUserId { get; protected set; }
    public Guid? CustomerId { get; protected set; }
    public string? TitleText { get; protected set; }
    public string? BodyText { get; protected set; }
    public string? LinkUrl { get; protected set; }
    public string InboxStatus { get; protected set; } = string.Empty;
    public DateTimeOffset? DeliveredAt { get; protected set; }
    public DateTimeOffset? ArchivedAt { get; protected set; }
    public DateTimeOffset? ReadAt { get; protected set; }
    public string? IpAddress { get; protected set; }
    public string? UserAgent { get; protected set; }

    public static NotificationInboxItem Create(
        Guid tenantId,
        Guid notificationMessageId,
        string recipientType,
        Guid? platformUserId,
        Guid? tenantUserId,
        Guid? customerId,
        string? titleText,
        string? bodyText,
        string? linkUrl,
        string inboxStatus,
        DateTimeOffset createdAt,
        DateTimeOffset? deliveredAt)
    {
        return new NotificationInboxItem
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            NotificationMessageId = notificationMessageId,
            RecipientType = recipientType.Trim().ToUpperInvariant(),
            PlatformUserId = platformUserId,
            TenantUserId = tenantUserId,
            CustomerId = customerId,
            TitleText = TrimToNull(titleText),
            BodyText = TrimToNull(bodyText),
            LinkUrl = TrimToNull(linkUrl),
            InboxStatus = inboxStatus.Trim().ToUpperInvariant(),
            CreatedAt = createdAt,
            DeliveredAt = deliveredAt
        };
    }

    public void MarkRead(DateTimeOffset readAt, string? ipAddress, string? userAgent)
    {
        if (string.Equals(InboxStatus, "READ", StringComparison.OrdinalIgnoreCase))
            return;

        InboxStatus = "READ";
        ReadAt = readAt;
        IpAddress = TrimToNull(ipAddress);
        UserAgent = TrimToNull(userAgent);
    }

    private static string? TrimToNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}