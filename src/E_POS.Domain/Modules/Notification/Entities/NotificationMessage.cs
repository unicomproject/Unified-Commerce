using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Notification.Entities;

public class NotificationMessage : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid PlatformUserId { get; protected set; }
    public Guid TenantUserId { get; protected set; }
    public Guid CustomerId { get; protected set; }
    public string RecipientType { get; protected set; } = string.Empty;
    public string MessageNumber { get; protected set; } = string.Empty;
    public Guid NotificationChannelId { get; protected set; }
    public Guid NotificationEventId { get; protected set; }
    public Guid NotificationTemplateVersionId { get; protected set; }
}
