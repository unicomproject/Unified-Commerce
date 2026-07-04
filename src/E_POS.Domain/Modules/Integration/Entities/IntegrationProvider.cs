using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Integration.Entities;

public class IntegrationProvider : AuditableEntity
{
    public string ProviderCode { get; protected set; } = string.Empty;
    public string ProviderName { get; protected set; } = string.Empty;
    public string ProviderCategory { get; protected set; } = string.Empty;
    public string ProviderType { get; protected set; } = string.Empty;
    public string AuthType { get; protected set; } = string.Empty;
    public string? ApiBaseUrl { get; protected set; }
    public string? DocumentationUrl { get; protected set; }
    public bool SupportsWebhook { get; protected set; }
    public bool SupportsTestMode { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
}
