using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Notification.Entities;

public class NotificationTemplate : AuditableEntity
{
    public Guid? TenantId { get; protected set; }
    public Guid NotificationEventTypeId { get; protected set; }
    public string TemplateCode { get; protected set; } = string.Empty;
    public string TemplateName { get; protected set; } = string.Empty;
    public string TemplateScope { get; protected set; } = string.Empty;
    public string ChannelType { get; protected set; } = string.Empty;
    public string? Locale { get; protected set; }
    public string? Description { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
    public Guid? CreatedByPlatformUserId { get; protected set; }
    public Guid? CreatedByTenantUserId { get; protected set; }
    public Guid? UpdatedByPlatformUserId { get; protected set; }
    public Guid? UpdatedByTenantUserId { get; protected set; }
    public DateTimeOffset? ArchivedAt { get; protected set; }
    public Guid? ArchivedByPlatformUserId { get; protected set; }
    public Guid? ArchivedByTenantUserId { get; protected set; }
}
