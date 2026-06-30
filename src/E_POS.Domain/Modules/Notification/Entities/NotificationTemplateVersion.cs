using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Notification.Entities;

public class NotificationTemplateVersion : AuditableEntity
{
    public Guid NotificationTemplateId { get; protected set; }
    public bool IsActiveVersion { get; protected set; }
    public int VersionNumber { get; protected set; }
}
