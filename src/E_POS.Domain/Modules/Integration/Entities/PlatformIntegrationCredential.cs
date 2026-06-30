using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Integration.Entities;

public class PlatformIntegrationCredential : AuditableEntity
{
    public string CredentialName { get; protected set; } = string.Empty;
    public Guid PlatformIntegrationId { get; protected set; }
    public DateTimeOffset RevokedAt { get; protected set; }
}
