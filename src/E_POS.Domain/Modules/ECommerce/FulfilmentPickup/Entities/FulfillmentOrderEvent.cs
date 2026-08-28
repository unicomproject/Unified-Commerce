using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.ECommerce.FulfilmentPickup.Entities;

public class FulfillmentOrderEvent : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid FulfillmentOrderId { get; protected set; }
    public int SequenceNumber { get; protected set; }
    public string EventType { get; protected set; } = string.Empty;
    public string? OldStatus { get; protected set; }
    public string? NewStatus { get; protected set; }
    public string? EventNote { get; protected set; }
    public string? EventPayloadJson { get; protected set; }
    public DateTimeOffset EventAt { get; protected set; }
    public Guid? EventByTenantUserId { get; protected set; }

    protected FulfillmentOrderEvent() { }

    public static FulfillmentOrderEvent Record(
        Guid id,
        Guid tenantId,
        Guid fulfillmentOrderId,
        int sequenceNumber,
        string eventType,
        string? oldStatus,
        string? newStatus,
        Guid eventByTenantUserId,
        DateTimeOffset now,
        string? eventNote = null,
        string? eventPayloadJson = null) => new()
    {
        Id = id,
        TenantId = tenantId,
        FulfillmentOrderId = fulfillmentOrderId,
        SequenceNumber = sequenceNumber,
        EventType = eventType.Trim(),
        OldStatus = oldStatus,
        NewStatus = newStatus,
        EventNote = eventNote,
        EventPayloadJson = eventPayloadJson,
        EventAt = now,
        EventByTenantUserId = eventByTenantUserId,
        CreatedAt = now,
        UpdatedAt = now
    };
}

