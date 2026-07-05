using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.PricingTax.Entities;

public class TaxRate : AuditableEntity
{
    protected TaxRate() { }

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

    public static TaxRate Create(Guid tenantId, Guid taxJurisdictionId, string taxRateCode, string taxRateName, decimal ratePercent, bool isCompound, DateOnly? validFrom, DateOnly? validUntil, Guid? createdByTenantUserId, DateTimeOffset now)
    {
        return new TaxRate
        {
            TenantId = tenantId,
            TaxJurisdictionId = taxJurisdictionId,
            TaxRateCode = taxRateCode.Trim().ToUpperInvariant(),
            TaxRateName = taxRateName.Trim(),
            RatePercent = ratePercent,
            IsCompound = isCompound,
            ValidFrom = validFrom,
            ValidUntil = validUntil,
            Status = "ACTIVE",
            CreatedByTenantUserId = createdByTenantUserId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void UpdateProfile(string taxRateName, decimal ratePercent, bool isCompound, DateOnly? validFrom, DateOnly? validUntil, string status, Guid? updatedByTenantUserId)
    {
        TaxRateName = taxRateName.Trim();
        RatePercent = ratePercent;
        IsCompound = isCompound;
        ValidFrom = validFrom;
        ValidUntil = validUntil;
        Status = status.Trim().ToUpperInvariant();
        UpdatedByTenantUserId = updatedByTenantUserId;
    }

    public void SoftDelete(Guid? updatedByTenantUserId)
    {
        Status = "DELETED";
        UpdatedByTenantUserId = updatedByTenantUserId;
    }
}
