using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.CatalogProduct.Entities;

public class UnitOfMeasure : AuditableEntity
{
    public Guid? TenantId { get; protected set; }
    public string Name { get; protected set; } = string.Empty;
    public decimal? ConversionFactor { get; protected set; }
    public string UomCode { get; protected set; } = string.Empty;
}