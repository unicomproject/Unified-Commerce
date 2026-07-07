using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;

public class TenantDomain : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public string DomainName { get; protected set; } = string.Empty;
    public string DomainStatus { get; protected set; } = string.Empty;
}

