using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.ECommerce.FulfilmentPickup.Entities;

public class PickupOrderEvent : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid PickupOrderId { get; protected set; }
    public int SequenceNumber { get; protected set; }
    public string EventType { get; protected set; } = string.Empty;
    public string? OldStatus { get; protected set; }
    public string? NewStatus { get; protected set; }
    public string? EventNote { get; protected set; }
    public string? EventPayloadJson { get; protected set; }
    public DateTimeOffset EventAt { get; protected set; }
    public Guid? EventByTenantUserId { get; protected set; }

    public static PickupOrderEvent Create(
        Guid id,
        Guid tenantId,
        Guid pickupOrderId,
        int sequenceNumber,
        string eventType,
        string? oldStatus,
        string? newStatus,
        DateTimeOffset eventAt,
        Guid actorTenantUserId,
        string? eventNote = null,
        string? eventPayloadJson = null)
    {
        return new PickupOrderEvent
        {
            Id = id,
            TenantId = tenantId,
            PickupOrderId = pickupOrderId,
            SequenceNumber = sequenceNumber,
            EventType = eventType,
            OldStatus = oldStatus,
            NewStatus = newStatus,
            EventNote = eventNote,
            EventPayloadJson = eventPayloadJson,
            EventAt = eventAt,
            EventByTenantUserId = actorTenantUserId,
            CreatedAt = eventAt,
            UpdatedAt = eventAt
        };
    }
}

