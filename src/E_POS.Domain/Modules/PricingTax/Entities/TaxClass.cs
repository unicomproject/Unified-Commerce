using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.PricingTax.Entities;

public class TaxClass : AuditableEntity
{
    protected TaxClass() { }

    public Guid TenantId { get; protected set; }
    public string TaxClassCode { get; protected set; } = string.Empty;
    public string TaxClassName { get; protected set; } = string.Empty;
    public string? Description { get; protected set; }
    public bool IsDefaultTaxClass { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
    public Guid? CreatedByTenantUserId { get; protected set; }
    public Guid? UpdatedByTenantUserId { get; protected set; }

    public static TaxClass Create(Guid tenantId, string taxClassCode, string taxClassName, string? description, bool isDefaultTaxClass, Guid? createdByTenantUserId, DateTimeOffset now)
    {
        return new TaxClass
        {
            TenantId = tenantId,
            TaxClassCode = taxClassCode.Trim().ToUpperInvariant(),
            TaxClassName = taxClassName.Trim(),
            Description = description?.Trim(),
            IsDefaultTaxClass = isDefaultTaxClass,
            Status = "ACTIVE",
            CreatedByTenantUserId = createdByTenantUserId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void UpdateProfile(string taxClassName, string? description, Guid? updatedByTenantUserId)
    {
        TaxClassName = taxClassName.Trim();
        Description = description?.Trim();
        UpdatedByTenantUserId = updatedByTenantUserId;
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
}
