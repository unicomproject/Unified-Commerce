using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Notification.Entities;

public class NotificationReadReceipt : AuditableEntity
{
    public Guid NotificationInboxItemId { get; protected set; }
    public string ReadSource { get; protected set; } = string.Empty;
}
