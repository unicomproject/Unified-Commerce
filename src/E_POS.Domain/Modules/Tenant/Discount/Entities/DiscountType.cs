using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.Discount.Entities;

public class DiscountType : AuditableEntity
{
    public string DiscountTypeCode { get; protected set; } = string.Empty;
    public string DiscountTypeName { get; protected set; } = string.Empty;
    public string CalculationMethod { get; protected set; } = string.Empty;
    public bool IsSystemType { get; protected set; }
    public string Status { get; protected set; } = string.Empty;

    public static DiscountType Create(
        Guid id,
        string discountTypeCode,
        string discountTypeName,
        string calculationMethod,
        bool isSystemType,
        string status,
        DateTimeOffset now)
    {
        return new DiscountType
        {
            Id = id,
            DiscountTypeCode = discountTypeCode.Trim().ToUpperInvariant(),
            DiscountTypeName = discountTypeName.Trim(),
            CalculationMethod = calculationMethod.Trim().ToUpperInvariant(),
            IsSystemType = isSystemType,
            Status = status.Trim().ToUpperInvariant(),
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void UpdateProfile(
        string discountTypeCode,
        string discountTypeName,
        string calculationMethod,
        bool isSystemType,
        string status,
        DateTimeOffset now)
    {
        DiscountTypeCode = discountTypeCode.Trim().ToUpperInvariant();
        DiscountTypeName = discountTypeName.Trim();
        CalculationMethod = calculationMethod.Trim().ToUpperInvariant();
        IsSystemType = isSystemType;
        Status = status.Trim().ToUpperInvariant();
        UpdatedAt = now;
    }

    public void SoftDelete(DateTimeOffset now)
    {
        Status = "DELETED";
        UpdatedAt = now;
    }
}
