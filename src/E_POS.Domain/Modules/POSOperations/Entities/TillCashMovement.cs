using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.POSOperations.Entities;

public class TillCashMovement : AuditableEntity
{
    public decimal Amount { get; protected set; }
    public Guid TillSessionId { get; protected set; }
}
