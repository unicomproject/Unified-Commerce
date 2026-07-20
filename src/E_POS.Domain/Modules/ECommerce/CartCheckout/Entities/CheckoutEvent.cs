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

    protected CheckoutEvent() { }

    public static CheckoutEvent Record(
        Guid id,
        Guid tenantId,
        Guid checkoutSessionId,
        string eventType,
        string? eventStatus,
        string? eventPayloadJson,
        DateTimeOffset now)
    {
        return new CheckoutEvent
        {
            Id = id,
            TenantId = tenantId,
            CheckoutSessionId = checkoutSessionId,
            EventType = eventType.Trim().ToUpperInvariant(),
            EventStatus = string.IsNullOrWhiteSpace(eventStatus) ? null : eventStatus.Trim().ToUpperInvariant(),
            EventPayloadJson = string.IsNullOrWhiteSpace(eventPayloadJson) ? null : eventPayloadJson,
            EventAt = now,
            CreatedAt = now
        };
    }
}

