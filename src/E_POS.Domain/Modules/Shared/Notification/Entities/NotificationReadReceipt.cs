using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Shared.Notification.Entities;

public class NotificationReadReceipt : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid NotificationInboxItemId { get; protected set; }
    public Guid NotificationMessageId { get; protected set; }
    public string RecipientType { get; protected set; } = string.Empty;
    public Guid? PlatformUserId { get; protected set; }
    public Guid? TenantUserId { get; protected set; }
    public Guid? CustomerId { get; protected set; }
    public DateTimeOffset ReadAt { get; protected set; }
    public string? IpAddress { get; protected set; }
    public string? UserAgent { get; protected set; }

    public static NotificationReadReceipt Create(
        Guid tenantId,
        Guid notificationInboxItemId,
        Guid notificationMessageId,
        string recipientType,
        Guid? platformUserId,
        Guid? tenantUserId,
        Guid? customerId,
        DateTimeOffset readAt,
        string? ipAddress,
        string? userAgent)
    {
        return new NotificationReadReceipt
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            NotificationInboxItemId = notificationInboxItemId,
            NotificationMessageId = notificationMessageId,
            RecipientType = recipientType.Trim().ToUpperInvariant(),
            PlatformUserId = platformUserId,
            TenantUserId = tenantUserId,
            CustomerId = customerId,
            ReadAt = readAt,
            IpAddress = TrimToNull(ipAddress),
            UserAgent = TrimToNull(userAgent)
        };
    }

    private static string? TrimToNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}