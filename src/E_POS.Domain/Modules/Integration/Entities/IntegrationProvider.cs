using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Integration.Entities;

public class IntegrationProvider : AuditableEntity
{
    public string ProviderCode { get; protected set; } = string.Empty;
    public string Name { get; protected set; } = string.Empty;
    public string? Description { get; protected set; }
    public string ProviderCategory { get; protected set; } = string.Empty;
}
