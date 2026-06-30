using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.POSOperations.Entities;

public class TillSessionSummary : AuditableEntity
{
    public int OrderCount { get; protected set; }
    public int RefundCount { get; protected set; }
    public Guid TillSessionId { get; protected set; }
}
