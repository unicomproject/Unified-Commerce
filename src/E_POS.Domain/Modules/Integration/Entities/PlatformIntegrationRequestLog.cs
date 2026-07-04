using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Integration.Entities;

public class PlatformIntegrationRequestLog : AuditableEntity
{
    public Guid IntegrationProviderId { get; protected set; }
    public Guid? PlatformIntegrationId { get; protected set; }
    public Guid? TenantId { get; protected set; }
    public string RequestDirection { get; protected set; } = string.Empty;
    public string RequestType { get; protected set; } = string.Empty;
    public string HttpMethod { get; protected set; } = string.Empty;
    public string RequestUrl { get; protected set; } = string.Empty;
    public string? RequestHeadersJson { get; protected set; }
    public string? RequestBodyHash { get; protected set; }
    public int? ResponseStatusCode { get; protected set; }
    public string? ResponseHeadersJson { get; protected set; }
    public string? ResponseBodyHash { get; protected set; }
    public string RequestStatus { get; protected set; } = string.Empty;
    public string? ErrorCode { get; protected set; }
    public string? ErrorMessage { get; protected set; }
    public string? IdempotencyKey { get; protected set; }
    public string? CorrelationId { get; protected set; }
    public DateTimeOffset RequestedAt { get; protected set; }
    public DateTimeOffset? CompletedAt { get; protected set; }
    public int? DurationMs { get; protected set; }
}
