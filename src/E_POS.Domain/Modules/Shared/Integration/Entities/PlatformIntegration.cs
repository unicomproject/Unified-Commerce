using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Shared.Integration.Entities;

public class PlatformIntegration : AuditableEntity
{
    public Guid IntegrationProviderId { get; protected set; }
    public string IntegrationCode { get; protected set; } = string.Empty;
    public string IntegrationName { get; protected set; } = string.Empty;
    public string IntegrationCategory { get; protected set; } = string.Empty;
    public string IntegrationStatus { get; protected set; } = string.Empty;
    public string Environment { get; protected set; } = string.Empty;
    public string? CurrencyCode { get; protected set; }
    public bool IsEnabled { get; protected set; }
    public bool IsDefault { get; protected set; }
    public string? BaseUrl { get; protected set; }
    public string? InboundWebhookUrl { get; protected set; }
    public DateTimeOffset? ConnectedAt { get; protected set; }
    public DateTimeOffset? DisconnectedAt { get; protected set; }
    public DateTimeOffset? LastSuccessfulRequestAt { get; protected set; }
    public DateTimeOffset? LastFailedRequestAt { get; protected set; }
    public string? LastFailureReason { get; protected set; }
    public Guid? CreatedByPlatformUserId { get; protected set; }
}

