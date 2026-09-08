using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.PricingTax.Entities;

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
    public string? Notes { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
    public Guid? CreatedByTenantUserId { get; protected set; }
    public Guid? UpdatedByTenantUserId { get; protected set; }

    public static TaxRate Create(
        Guid tenantId,
        Guid taxJurisdictionId,
        string taxRateCode,
        string taxRateName,
        decimal ratePercent,
        bool isCompound,
        DateOnly? validFrom,
        DateOnly? validUntil,
        Guid? createdByTenantUserId,
        DateTimeOffset now,
        string? notes = null)
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
            Notes = notes?.Trim(),
            Status = "ACTIVE",
            CreatedByTenantUserId = createdByTenantUserId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void UpdateProfile(
        string taxRateName,
        decimal ratePercent,
        bool isCompound,
        DateOnly? validFrom,
        DateOnly? validUntil,
        string status,
        Guid? updatedByTenantUserId,
        string? notes = null)
    {
        TaxRateName = taxRateName.Trim();
        RatePercent = ratePercent;
        IsCompound = isCompound;
        ValidFrom = validFrom;
        ValidUntil = validUntil;
        Status = status.Trim().ToUpperInvariant();
        if (notes is not null)
            Notes = notes.Trim();
        UpdatedByTenantUserId = updatedByTenantUserId;
    }

    public void EndOn(DateOnly exclusiveEndDate, Guid? updatedByTenantUserId, DateTimeOffset now)
    {
        // Previous period ends the day before the next EffectiveFrom (date-only model).
        ValidUntil = exclusiveEndDate.AddDays(-1);
        UpdatedByTenantUserId = updatedByTenantUserId;
        UpdatedAt = now;
    }

    public void UpdateFutureSchedule(
        decimal ratePercent,
        DateOnly validFrom,
        string? notes,
        Guid? updatedByTenantUserId,
        DateTimeOffset now)
    {
        RatePercent = ratePercent;
        ValidFrom = validFrom;
        Notes = notes?.Trim();
        UpdatedByTenantUserId = updatedByTenantUserId;
        UpdatedAt = now;
    }

    public void SoftDelete(Guid? updatedByTenantUserId)
    {
        Status = "DELETED";
        UpdatedByTenantUserId = updatedByTenantUserId;
    }

    public bool IsApplicableOn(DateOnly businessDate)
    {
        if (Status is "DELETED" or "INACTIVE")
            return false;

        if (ValidFrom.HasValue && ValidFrom.Value > businessDate)
            return false;

        if (ValidUntil.HasValue && ValidUntil.Value < businessDate)
            return false;

        return true;
    }

    public bool IsFutureRelativeTo(DateOnly businessDate) =>
        Status != "DELETED" && ValidFrom.HasValue && ValidFrom.Value > businessDate;

    public bool IsHistoricalRelativeTo(DateOnly businessDate) =>
        Status != "DELETED" && ValidUntil.HasValue && ValidUntil.Value < businessDate;
}
