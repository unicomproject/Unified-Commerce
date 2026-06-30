using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Notification.Entities;

public class NotificationChannel : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public string Name { get; protected set; } = string.Empty;
    public string Status { get; protected set; } = string.Empty;
    public string ChannelType { get; protected set; } = string.Empty;
    public string ChannelCode { get; protected set; } = string.Empty;
    public Guid PlatformIntegrationId { get; protected set; }
}
