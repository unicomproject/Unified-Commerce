using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Notification.Entities;

public class NotificationTemplate : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public string TemplateCode { get; protected set; } = string.Empty;
    public string Name { get; protected set; } = string.Empty;
    public string? Description { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
    public string ChannelType { get; protected set; } = string.Empty;
    public string Locale { get; protected set; } = string.Empty;
    public Guid NotificationEventTypeId { get; protected set; }
    public int SortOrder { get; protected set; }
}
