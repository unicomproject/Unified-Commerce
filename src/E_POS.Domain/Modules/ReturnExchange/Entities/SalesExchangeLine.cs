using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.ReturnExchange.Entities;

public class SalesExchangeLine : AuditableEntity
{
    public decimal ReplacementQuantity { get; protected set; }
    public decimal ReturnedQuantity { get; protected set; }
    public Guid SalesExchangeId { get; protected set; }
    public Guid SalesReturnLineId { get; protected set; }
}
