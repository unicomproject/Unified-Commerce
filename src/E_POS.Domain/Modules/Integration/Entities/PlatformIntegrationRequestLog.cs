using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Integration.Entities;

public class PlatformIntegrationRequestLog : AuditableEntity
{
    public string Name { get; protected set; } = string.Empty;
    public int DurationMs { get; protected set; }
    public Guid IntegrationProviderId { get; protected set; }
    public Guid PlatformIntegrationId { get; protected set; }
    public int? ResponseStatusCode { get; protected set; }
}
