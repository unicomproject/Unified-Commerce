using System.Net;
using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Integration.Entities;

public class PlatformIntegrationWebhookEvent : AuditableEntity
{
    public Guid IntegrationProviderId { get; protected set; }
    public Guid? PlatformIntegrationId { get; protected set; }
    public Guid? TenantId { get; protected set; }
    public string? ExternalEventId { get; protected set; }
    public string EventName { get; protected set; } = string.Empty;
    public string? EventCategory { get; protected set; }
    public bool? SignatureValid { get; protected set; }
    public string EventStatus { get; protected set; } = string.Empty;
    public string? EventPayloadJson { get; protected set; }
    public string? HeadersJson { get; protected set; }
    public IPAddress? SourceIp { get; protected set; }
    public string? ReceivedSignatureHash { get; protected set; }
    public string? IdempotencyKey { get; protected set; }
    public DateTimeOffset ReceivedAt { get; protected set; }
    public DateTimeOffset? ProcessingStartedAt { get; protected set; }
    public DateTimeOffset? ProcessedAt { get; protected set; }
    public string? ProcessingError { get; protected set; }
}
