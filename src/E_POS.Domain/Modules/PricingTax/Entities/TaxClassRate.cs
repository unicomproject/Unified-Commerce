using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.PricingTax.Entities;

public class TaxClassRate : AuditableEntity
{
    public Guid TaxClassId { get; protected set; }
    public Guid TaxRateId { get; protected set; }
}
