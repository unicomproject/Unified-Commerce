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
}

