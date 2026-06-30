using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.PricingTax.Entities;

public class TaxRate : AuditableEntity
{
    public string Name { get; protected set; } = string.Empty;
    public decimal RatePercent { get; protected set; }
    public Guid TaxJurisdictionId { get; protected set; }
    public string TaxRateCode { get; protected set; } = string.Empty;
}
