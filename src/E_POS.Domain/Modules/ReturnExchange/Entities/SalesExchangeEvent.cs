using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.ReturnExchange.Entities;

public class SalesExchangeEvent : AuditableEntity
{
    public Guid SalesExchangeId { get; protected set; }
    public int SequenceNumber { get; protected set; }
}
