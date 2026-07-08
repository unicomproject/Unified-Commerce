using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.Orders.Entities;

public class SalesOrderCharge : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid SalesOrderId { get; protected set; }
    public Guid? SalesOrderLineId { get; protected set; }
    public string ChargeScope { get; protected set; } = string.Empty;
    public string ChargeType { get; protected set; } = string.Empty;
    public string ChargeNameSnapshot { get; protected set; } = string.Empty;
    public decimal ChargeAmount { get; protected set; }
    public bool IsTaxable { get; protected set; }
    public Guid? AppliedByTenantUserId { get; protected set; }
    public DateTimeOffset AppliedAt { get; protected set; }
}

