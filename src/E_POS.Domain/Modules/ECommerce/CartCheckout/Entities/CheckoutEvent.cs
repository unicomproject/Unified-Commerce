using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.ECommerce.CartCheckout.Entities;

public class CheckoutEvent : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid CheckoutSessionId { get; protected set; }
    public string EventType { get; protected set; } = string.Empty;
    public string? EventStatus { get; protected set; }
    public string? EventPayloadJson { get; protected set; }
    public DateTimeOffset EventAt { get; protected set; }
}

