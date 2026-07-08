using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;

public class UnitOfMeasure : AuditableEntity
{
    public Guid? TenantId { get; protected set; }
    public string UomCode { get; protected set; } = string.Empty;
    public string UomName { get; protected set; } = string.Empty;
    public string UomType { get; protected set; } = string.Empty;
    public string? Symbol { get; protected set; }
    public Guid? BaseUomId { get; protected set; }
    public decimal ConversionFactor { get; protected set; }
    public string Status { get; protected set; } = string.Empty;

    public static UnitOfMeasure Create(
        Guid id, 
        Guid? tenantId, 
        string uomCode, 
        string uomName, 
        string uomType,
        string? symbol,
        Guid? baseUomId,
        decimal conversionFactor,
        string status,
        DateTimeOffset now)
    {
        return new UnitOfMeasure
        {
            Id = id,
            TenantId = tenantId,
            UomCode = uomCode.Trim().ToUpperInvariant(),
            UomName = uomName.Trim(),
            UomType = uomType.Trim().ToUpperInvariant(),
            Symbol = symbol?.Trim(),
            BaseUomId = baseUomId,
            ConversionFactor = conversionFactor,
            Status = status.Trim().ToUpperInvariant(),
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}
