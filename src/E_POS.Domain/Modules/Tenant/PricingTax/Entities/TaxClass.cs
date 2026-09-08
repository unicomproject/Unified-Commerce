using E_POS.Domain.Common.Entities;
using E_POS.Domain.Modules.Tenant.PricingTax.Constants;

namespace E_POS.Domain.Modules.Tenant.PricingTax.Entities;

/// <summary>
/// Persistence entity for canonical Tax Setup (domain name). Table: tax_classes.
/// </summary>
public class TaxClass : AuditableEntity
{
    protected TaxClass() { }

    public Guid TenantId { get; protected set; }
    public string TaxClassCode { get; protected set; } = string.Empty;
    public string TaxClassName { get; protected set; } = string.Empty;

    /// <summary>Canonical treatment: TAXABLE | ZERO_RATED | EXEMPT.</summary>
    public string TaxTreatment { get; protected set; } = TaxTreatments.Taxable;

    /// <summary>Legacy metadata column; kept for compatibility; no domain effect. Synced from TaxTreatment.</summary>
    public string TaxType { get; protected set; } = TaxTreatments.Taxable;

    public string? Description { get; protected set; }
    public bool IsDefaultTaxClass { get; protected set; }
    public bool IsSeeded { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
    public Guid? CreatedByTenantUserId { get; protected set; }
    public Guid? UpdatedByTenantUserId { get; protected set; }

    public static TaxClass Create(
        Guid tenantId,
        string taxClassCode,
        string taxClassName,
        string taxTreatment,
        string? description,
        bool isDefaultTaxClass,
        Guid? createdByTenantUserId,
        DateTimeOffset now,
        bool isSeeded = false)
    {
        var treatment = TaxTreatments.IsValid(taxTreatment)
            ? TaxTreatments.Normalize(taxTreatment)
            : TaxTreatments.MapFromLegacyTaxType(taxTreatment);
        return new TaxClass
        {
            TenantId = tenantId,
            TaxClassCode = taxClassCode.Trim().ToUpperInvariant(),
            TaxClassName = taxClassName.Trim(),
            TaxTreatment = treatment,
            TaxType = treatment,
            Description = description?.Trim(),
            IsDefaultTaxClass = isDefaultTaxClass,
            IsSeeded = isSeeded,
            Status = "ACTIVE",
            CreatedByTenantUserId = createdByTenantUserId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void UpdateProfile(
        string taxClassName,
        string? description,
        Guid? updatedByTenantUserId,
        DateTimeOffset now)
    {
        TaxClassName = taxClassName.Trim();
        Description = description?.Trim();
        UpdatedByTenantUserId = updatedByTenantUserId;
        UpdatedAt = now;
    }

    public void ChangeTreatment(string taxTreatment, Guid? updatedByTenantUserId, DateTimeOffset now)
    {
        var treatment = TaxTreatments.Normalize(taxTreatment);
        TaxTreatment = treatment;
        TaxType = treatment;
        UpdatedByTenantUserId = updatedByTenantUserId;
        UpdatedAt = now;
    }

    public void SetStatus(string status, Guid? updatedByTenantUserId, DateTimeOffset now)
    {
        Status = status.Trim().ToUpperInvariant();
        UpdatedByTenantUserId = updatedByTenantUserId;
        UpdatedAt = now;
    }

    public void SetDefault(bool isDefault, Guid? updatedByTenantUserId)
    {
        IsDefaultTaxClass = isDefault;
        UpdatedByTenantUserId = updatedByTenantUserId;
    }

    public void SoftDelete(Guid? updatedByTenantUserId)
    {
        Status = "DELETED";
        UpdatedByTenantUserId = updatedByTenantUserId;
    }

    /// <summary>Legacy overload used by fine-grained TaxSetupService until fully migrated.</summary>
    public void UpdateProfile(string taxClassName, string taxTypeOrTreatment, string? description, string status, Guid? updatedByTenantUserId)
    {
        TaxClassName = taxClassName.Trim();
        var treatment = TaxTreatments.IsValid(taxTypeOrTreatment)
            ? TaxTreatments.Normalize(taxTypeOrTreatment)
            : TaxTreatments.MapFromLegacyTaxType(taxTypeOrTreatment);
        TaxTreatment = treatment;
        TaxType = treatment;
        Description = description?.Trim();
        Status = status.Trim().ToUpperInvariant();
        UpdatedByTenantUserId = updatedByTenantUserId;
    }
}
