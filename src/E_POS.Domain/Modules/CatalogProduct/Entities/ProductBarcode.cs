using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.CatalogProduct.Entities;

public class ProductBarcode : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public string BarcodeValue { get; protected set; } = string.Empty;
    public Guid ProductId { get; protected set; }
    public Guid? ProductVariantId { get; protected set; }
}
