using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.HardwareCash.Entities;

public class CashCountDenomination : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid CashReconciliationId { get; protected set; }
    public string CountType { get; protected set; } = string.Empty;
    public string CurrencyCode { get; protected set; } = string.Empty;
    public decimal DenominationValue { get; protected set; }
    public int Quantity { get; protected set; }
    public decimal LineTotal { get; protected set; }
    public Guid? CountedByTenantUserId { get; protected set; }
    public DateTimeOffset CountedAt { get; protected set; }
    
    // AuditableEntity has UpdatedBy and UpdatedAt which we will ignore in EF mapping since ERD doesn't have it.
    // Also ignore CreatedBy since ERD only has CreatedAt.

    public static CashCountDenomination Create(
        Guid id,
        Guid tenantId,
        Guid cashReconciliationId,
        string countType,
        string currencyCode,
        decimal denominationValue,
        int quantity,
        decimal lineTotal,
        Guid? countedByTenantUserId,
        DateTimeOffset now)
    {
        return new CashCountDenomination
        {
            Id = id,
            TenantId = tenantId,
            CashReconciliationId = cashReconciliationId,
            CountType = countType.Trim().ToUpperInvariant(),
            CurrencyCode = currencyCode.Trim().ToUpperInvariant(),
            DenominationValue = denominationValue,
            Quantity = quantity,
            LineTotal = lineTotal,
            CountedByTenantUserId = countedByTenantUserId,
            CountedAt = now,
            CreatedAt = now,
            UpdatedAt = now // Required by base class, will ignore in mapping
        };
    }
}

