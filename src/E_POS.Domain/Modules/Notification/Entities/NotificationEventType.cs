using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Notification.Entities;

public class NotificationEventType : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public string Name { get; protected set; } = string.Empty;
    public string? Description { get; protected set; }
    public string DefaultPriority { get; protected set; } = string.Empty;
    public string EventCode { get; protected set; } = string.Empty;
    public int SortOrder { get; protected set; }
}
