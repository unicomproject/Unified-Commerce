using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.Orders.Entities;

public class SalesOrderNumberSequence : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public string SalesChannel { get; protected set; } = string.Empty;
    public string OrderType { get; protected set; } = string.Empty;
    public string Prefix { get; protected set; } = string.Empty;
    public long CurrentValue { get; protected set; }
    public string ResetRule { get; protected set; } = string.Empty;
    public DateTimeOffset? LastGeneratedAt { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
}
