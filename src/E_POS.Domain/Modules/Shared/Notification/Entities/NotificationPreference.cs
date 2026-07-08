using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Shared.Notification.Entities;

public class NotificationPreference : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public string RecipientType { get; protected set; } = string.Empty;
    public Guid? PlatformUserId { get; protected set; }
    public Guid? TenantUserId { get; protected set; }
    public Guid? CustomerId { get; protected set; }
    public Guid NotificationEventTypeId { get; protected set; }
    public string ChannelType { get; protected set; } = string.Empty;
    public bool IsEnabled { get; protected set; }
    public DateTimeOffset? MutedUntil { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
}

