using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Integration.Entities;

public class PlatformIntegrationWebhookEvent : AuditableEntity
{
    public Guid ExternalEventId { get; protected set; }
    public string IdempotencyKey { get; protected set; } = string.Empty;
    public Guid IntegrationProviderId { get; protected set; }
    public Guid PlatformIntegrationId { get; protected set; }
}
