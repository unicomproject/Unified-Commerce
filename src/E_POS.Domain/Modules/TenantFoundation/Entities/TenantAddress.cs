using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.TenantFoundation.Entities;

public class TenantAddress : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public string AddressType { get; protected set; } = string.Empty;
}
