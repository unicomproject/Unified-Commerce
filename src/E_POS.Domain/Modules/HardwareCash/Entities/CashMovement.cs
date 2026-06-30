using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.HardwareCash.Entities;

public class CashMovement : AuditableEntity
{
    public decimal Amount { get; protected set; }
    public Guid CashMovementTypeId { get; protected set; }
    public Guid TillSessionId { get; protected set; }
}
