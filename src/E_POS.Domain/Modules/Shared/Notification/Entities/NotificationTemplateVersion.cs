using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Shared.Notification.Entities;

public class NotificationTemplateVersion : AuditableEntity
{
    public Guid? TenantId { get; protected set; }
    public Guid NotificationTemplateId { get; protected set; }
    public int VersionNumber { get; protected set; }
    public string? SubjectTemplate { get; protected set; }
    public string? TitleTemplate { get; protected set; }
    public string? BodyTextTemplate { get; protected set; }
    public string? BodyHtmlTemplate { get; protected set; }
    public string? ActionUrlTemplate { get; protected set; }
    public string? ActionLabelTemplate { get; protected set; }
    public string? VariablesSchemaJson { get; protected set; }
    public string? SamplePayloadJson { get; protected set; }
    public bool IsActiveVersion { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
    public Guid? CreatedByPlatformUserId { get; protected set; }
    public Guid? CreatedByTenantUserId { get; protected set; }
}

