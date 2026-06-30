using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Notification.Entities;

public class NotificationDeliveryAttempt : AuditableEntity
{
    public int AttemptNumber { get; protected set; }
    public Guid NotificationChannelId { get; protected set; }
    public Guid NotificationMessageId { get; protected set; }
}
