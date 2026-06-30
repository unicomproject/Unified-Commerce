using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Customer.Entities;

public class CustomerConsent : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid? CustomerId { get; protected set; }
    public string ConsentType { get; protected set; } = string.Empty;
    public Guid SalesChannelId { get; protected set; }
}
