using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.PricingTax.Entities;

public class TaxClassRate : AuditableEntity
{
    protected TaxClassRate() { }

    public Guid TenantId { get; protected set; }
    public Guid TaxClassId { get; protected set; }
    public Guid TaxRateId { get; protected set; }
    public int SortOrder { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
    public Guid? CreatedByTenantUserId { get; protected set; }
    public Guid? UpdatedByTenantUserId { get; protected set; }

    public static TaxClassRate Create(Guid tenantId, Guid taxClassId, Guid taxRateId, int sortOrder, Guid? createdByTenantUserId, DateTimeOffset now)
    {
        return new TaxClassRate
        {
            TenantId = tenantId,
            TaxClassId = taxClassId,
            TaxRateId = taxRateId,
            SortOrder = sortOrder,
            Status = "ACTIVE",
            CreatedByTenantUserId = createdByTenantUserId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}
