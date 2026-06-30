using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Payment.Entities;

public class SalesPaymentTransaction : AuditableEntity
{
    public decimal Amount { get; protected set; }
    public string IdempotencyKey { get; protected set; } = string.Empty;
    public Guid SalesPaymentId { get; protected set; }
}
