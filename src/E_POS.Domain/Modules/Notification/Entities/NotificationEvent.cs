using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Notification.Entities;

public class NotificationEvent : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public string EventNumber { get; protected set; } = string.Empty;
    public string IdempotencyKey { get; protected set; } = string.Empty;
    public Guid NotificationEventTypeId { get; protected set; }
}
