using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.TenantFoundation.Entities;

public class TenantProfile : AuditableEntity
{
    public Guid TenantId { get; protected set; }
}
