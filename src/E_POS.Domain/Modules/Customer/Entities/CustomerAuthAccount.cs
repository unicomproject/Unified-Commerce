using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Customer.Entities;

public class CustomerAuthAccount : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid? CustomerId { get; protected set; }
    public int FailedLoginCount { get; protected set; }
}
