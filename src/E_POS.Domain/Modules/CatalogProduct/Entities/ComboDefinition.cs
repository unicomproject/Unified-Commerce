using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.CatalogProduct.Entities;

public class ComboDefinition : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid ProductId { get; protected set; }
    public Guid? ProductVariantId { get; protected set; }
    public string ComboCode { get; protected set; } = string.Empty;
    public string ComboName { get; protected set; } = string.Empty;
    public string PricingMode { get; protected set; } = string.Empty;
    public string InventoryDeductionMode { get; protected set; } = string.Empty;
    public string Status { get; protected set; } = string.Empty;
    public Guid? CreatedByTenantUserId { get; protected set; }
    public Guid? UpdatedByTenantUserId { get; protected set; }

    public static ComboDefinition Create(
        Guid id,
        Guid tenantId,
        Guid productId,
        Guid? productVariantId,
        string comboCode,
        string comboName,
        string pricingMode,
        string inventoryDeductionMode,
        string status,
        Guid? createdByTenantUserId,
        DateTimeOffset now)
    {
        return new ComboDefinition
        {
            Id = id,
            TenantId = tenantId,
            ProductId = productId,
            ProductVariantId = productVariantId,
            ComboCode = comboCode.Trim(),
            ComboName = comboName.Trim(),
            PricingMode = pricingMode.Trim().ToUpperInvariant(),
            InventoryDeductionMode = inventoryDeductionMode.Trim().ToUpperInvariant(),
            Status = status.Trim().ToUpperInvariant(),
            CreatedByTenantUserId = createdByTenantUserId,
            UpdatedByTenantUserId = createdByTenantUserId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}
