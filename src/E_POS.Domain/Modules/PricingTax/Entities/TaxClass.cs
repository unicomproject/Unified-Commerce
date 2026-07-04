using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.PricingTax.Entities;

public class TaxClass : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public string TaxClassCode { get; protected set; } = string.Empty;
    public string TaxClassName { get; protected set; } = string.Empty;
    public string? Description { get; protected set; }
    public bool IsDefaultTaxClass { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
}
