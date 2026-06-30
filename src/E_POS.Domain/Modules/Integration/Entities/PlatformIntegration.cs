using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Integration.Entities;

public class PlatformIntegration : AuditableEntity
{
    public string IntegrationCode { get; protected set; } = string.Empty;
    public string CurrencyCode { get; protected set; } = string.Empty;
    public string Name { get; protected set; } = string.Empty;
    public string? Description { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
    public Guid IntegrationProviderId { get; protected set; }
}
