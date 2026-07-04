using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Notification.Entities;

public class NotificationDeliveryAttempt : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid NotificationMessageId { get; protected set; }
    public int AttemptNumber { get; protected set; }
    public string ChannelType { get; protected set; } = string.Empty;
    public string? ProviderName { get; protected set; }
    public string? ProviderMessageId { get; protected set; }
    public string? RequestPayloadJson { get; protected set; }
    public string? ResponsePayloadJson { get; protected set; }
    public int? ResponseStatusCode { get; protected set; }
    public string? ErrorCode { get; protected set; }
    public string? ErrorMessage { get; protected set; }
    public DateTimeOffset AttemptedAt { get; protected set; }
}
