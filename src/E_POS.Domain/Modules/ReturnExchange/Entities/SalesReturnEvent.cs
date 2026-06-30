using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.ReturnExchange.Entities;

public class SalesReturnEvent : AuditableEntity
{
    public Guid? SalesReturnId { get; protected set; }
    public int SequenceNumber { get; protected set; }
}
