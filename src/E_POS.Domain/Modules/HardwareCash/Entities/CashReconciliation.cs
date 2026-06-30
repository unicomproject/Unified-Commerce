using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.HardwareCash.Entities;

public class CashReconciliation : AuditableEntity
{
    public decimal CountedCashAmount { get; protected set; }
    public decimal ExpectedCashAmount { get; protected set; }
    public Guid TillSessionId { get; protected set; }
}
