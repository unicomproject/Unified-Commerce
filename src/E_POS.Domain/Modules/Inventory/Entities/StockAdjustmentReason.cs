using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Inventory.Entities;

public class StockAdjustmentReason : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public string ReasonCode { get; protected set; } = string.Empty;
    public string ReasonName { get; protected set; } = string.Empty;
    public string Direction { get; protected set; } = string.Empty;
    public bool RequiresManagerApproval { get; protected set; }
    public bool IsSystemReason { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
}