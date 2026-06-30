using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Customer.Entities;

public class CustomerAuthSession : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
    public Guid CustomerAuthAccountId { get; protected set; }
    public string SessionTokenHash { get; protected set; } = string.Empty;
}
