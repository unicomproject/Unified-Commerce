using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Payment.Entities;

public class SalesPaymentEvent : AuditableEntity
{
    public Guid SalesPaymentId { get; protected set; }
    public int SequenceNumber { get; protected set; }
}
