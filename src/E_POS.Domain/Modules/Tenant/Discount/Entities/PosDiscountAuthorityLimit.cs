using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.Discount.Entities;

public sealed class PosDiscountAuthorityLimit : AuditableEntity
{
    public Guid TenantId { get; private set; }
    public Guid TenantUserId { get; private set; }
    public decimal MaxPercentage { get; private set; }
    public decimal MaxFixedAmount { get; private set; }
    public string CurrencyCode { get; private set; } = string.Empty;
    public string Status { get; private set; } = string.Empty;
    public Guid? CreatedByTenantUserId { get; private set; }
    public Guid? UpdatedByTenantUserId { get; private set; }

    public static PosDiscountAuthorityLimit Create(
        Guid id,
        Guid tenantId,
        Guid tenantUserId,
        decimal maxPercentage,
        decimal maxFixedAmount,
        string currencyCode,
        Guid? createdByTenantUserId,
        DateTimeOffset now) =>
        new()
        {
            Id = id,
            TenantId = tenantId,
            TenantUserId = tenantUserId,
            MaxPercentage = maxPercentage,
            MaxFixedAmount = maxFixedAmount,
            CurrencyCode = currencyCode.Trim().ToUpperInvariant(),
            Status = "ACTIVE",
            CreatedByTenantUserId = createdByTenantUserId,
            UpdatedByTenantUserId = createdByTenantUserId,
            CreatedAt = now,
            UpdatedAt = now
        };
}
