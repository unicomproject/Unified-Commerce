using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.FulfilmentPickup.Entities;

public class FulfillmentMethod : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public string MethodCode { get; protected set; } = string.Empty;
    public string Name { get; protected set; } = string.Empty;
    public string? Description { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
    public string MethodType { get; protected set; } = string.Empty;
}
