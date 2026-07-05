using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.PricingTax.Entities;

public class TaxRate : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid TaxJurisdictionId { get; protected set; }
    public string TaxRateCode { get; protected set; } = string.Empty;
    public string TaxRateName { get; protected set; } = string.Empty;
    public decimal RatePercent { get; protected set; }
    public bool IsCompound { get; protected set; }
    public DateOnly? ValidFrom { get; protected set; }
    public DateOnly? ValidUntil { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
    public Guid? CreatedByTenantUserId { get; protected set; }
    public Guid? UpdatedByTenantUserId { get; protected set; }
}
