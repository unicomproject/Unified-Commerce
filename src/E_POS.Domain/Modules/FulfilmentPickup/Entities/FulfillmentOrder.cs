using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.FulfilmentPickup.Entities;

public class FulfillmentOrder : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
    public Guid FulfillmentMethodOutletId { get; protected set; }
    public string FulfillmentNumber { get; protected set; } = string.Empty;
    public Guid? SalesOrderId { get; protected set; }
}
