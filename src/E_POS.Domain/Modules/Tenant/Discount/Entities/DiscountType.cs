using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.Discount.Entities;

public class DiscountType : AuditableEntity
{
    public string DiscountTypeCode { get; protected set; } = string.Empty;
    public string DiscountTypeName { get; protected set; } = string.Empty;
    public string CalculationMethod { get; protected set; } = string.Empty;
    public bool IsSystemType { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
}
