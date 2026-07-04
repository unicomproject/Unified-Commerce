using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Notification.Entities;

public class NotificationEventType : AuditableEntity
{
    public Guid? TenantId { get; protected set; }
    public string EventCode { get; protected set; } = string.Empty;
    public string EventName { get; protected set; } = string.Empty;
    public string? SourceModule { get; protected set; }
    public string? Description { get; protected set; }
    public string DefaultPriority { get; protected set; } = string.Empty;
    public bool IsSystemEvent { get; protected set; }
    public bool IsEnabled { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
    public Guid? CreatedByPlatformUserId { get; protected set; }
    public Guid? CreatedByTenantUserId { get; protected set; }
    public Guid? UpdatedByPlatformUserId { get; protected set; }
    public Guid? UpdatedByTenantUserId { get; protected set; }
}
