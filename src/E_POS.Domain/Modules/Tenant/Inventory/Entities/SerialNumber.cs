using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.Inventory.Entities;

public class SerialNumber : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid ProductId { get; protected set; }
    public Guid? ProductVariantId { get; protected set; }
    public Guid? ProductBatchId { get; protected set; }
    public Guid? CurrentInventoryBalanceId { get; protected set; }
    public string SerialNumberValue { get; protected set; } = string.Empty;
    public string SerialStatus { get; protected set; } = string.Empty;
    public DateTimeOffset? ReceivedAt { get; protected set; }
}
