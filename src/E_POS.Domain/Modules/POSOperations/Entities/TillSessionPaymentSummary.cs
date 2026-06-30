using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.POSOperations.Entities;

public class TillSessionPaymentSummary : AuditableEntity
{
    public Guid PaymentMethodId { get; protected set; }
    public Guid TillSessionSummaryId { get; protected set; }
}
