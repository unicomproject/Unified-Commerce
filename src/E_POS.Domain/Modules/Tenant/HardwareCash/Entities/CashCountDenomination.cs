using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.HardwareCash.Entities;

public class CashCountDenomination : AuditableEntity
{
    public Guid CashReconciliationId { get; protected set; }
    public decimal DenominationValue { get; protected set; }
    public decimal Quantity { get; protected set; }
}

