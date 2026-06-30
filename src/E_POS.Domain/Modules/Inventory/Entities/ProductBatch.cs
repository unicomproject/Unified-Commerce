using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Inventory.Entities;

public class ProductBatch : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
    public string BatchNumber { get; protected set; } = string.Empty;
    public DateOnly? ManufacturedDate { get; protected set; }
    public DateOnly? ExpiryDate { get; protected set; }
    public Guid ProductId { get; protected set; }
    public Guid? ProductVariantId { get; protected set; }
}
