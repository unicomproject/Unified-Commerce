using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.PricingTax.Entities;

public class TaxJurisdiction : AuditableEntity
{
    protected TaxJurisdiction() { }

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

    public static TaxJurisdiction Create(Guid tenantId, string jurisdictionCode, string jurisdictionName, string jurisdictionType, string countryCode, string? regionCode, string? localityName, string? postalCodePattern, Guid? createdByTenantUserId, DateTimeOffset now)
    {
        return new TaxJurisdiction
        {
            TenantId = tenantId,
            JurisdictionCode = jurisdictionCode.Trim().ToUpperInvariant(),
            JurisdictionName = jurisdictionName.Trim(),
            JurisdictionType = jurisdictionType.Trim().ToUpperInvariant(),
            CountryCode = countryCode.Trim().ToUpperInvariant(),
            RegionCode = regionCode?.Trim().ToUpperInvariant(),
            LocalityName = localityName?.Trim(),
            PostalCodePattern = postalCodePattern?.Trim(),
            Status = "ACTIVE",
            CreatedByTenantUserId = createdByTenantUserId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}
