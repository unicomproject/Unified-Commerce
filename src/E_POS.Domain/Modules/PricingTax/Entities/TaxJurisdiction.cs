using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.PricingTax.Entities;

public class TaxJurisdiction : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid? ParentJurisdictionId { get; protected set; }
    public string JurisdictionCode { get; protected set; } = string.Empty;
    public string JurisdictionName { get; protected set; } = string.Empty;
    public string JurisdictionType { get; protected set; } = string.Empty;
    public string CountryCode { get; protected set; } = string.Empty;
    public string? RegionCode { get; protected set; }
    public string? LocalityName { get; protected set; }
    public string? PostalCodePattern { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
    public Guid? CreatedByTenantUserId { get; protected set; }
    public Guid? UpdatedByTenantUserId { get; protected set; }
}
