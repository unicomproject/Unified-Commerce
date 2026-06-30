using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.POSOperations.Entities;

public class PosOrderHold : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public string HoldNumber { get; protected set; } = string.Empty;
    public Guid? SalesOrderId { get; protected set; }
}
