using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Notification.Entities;

public class NotificationInboxItem : AuditableEntity
{
    public string InboxStatus { get; protected set; } = string.Empty;
    public Guid NotificationMessageId { get; protected set; }
}
