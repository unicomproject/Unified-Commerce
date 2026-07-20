using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.Payment.Entities;

public class SalesPaymentEvent : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid SalesPaymentId { get; protected set; }
    public int SequenceNumber { get; protected set; }
    public string EventType { get; protected set; } = string.Empty;
    public string? OldStatus { get; protected set; }
    public string? NewStatus { get; protected set; }
    public string? EventNote { get; protected set; }
    public string? EventPayloadJson { get; protected set; }
    public DateTimeOffset EventAt { get; protected set; }
    public Guid? EventByTenantUserId { get; protected set; }

    public static SalesPaymentEvent RecordPaid(
        Guid id,
        Guid tenantId,
        Guid salesPaymentId,
        Guid? eventByTenantUserId,
        DateTimeOffset now,
        string? eventNote = null) => new()
        {
            Id = id,
            TenantId = tenantId,
            SalesPaymentId = salesPaymentId,
            SequenceNumber = 1,
            EventType = "PAYMENT_COMPLETED",
            OldStatus = "PENDING",
            NewStatus = "PAID",
            EventNote = string.IsNullOrWhiteSpace(eventNote)
                ? "Cash payment completed at POS."
                : eventNote.Trim(),
            EventAt = now,
            EventByTenantUserId = eventByTenantUserId,
            CreatedAt = now,
            UpdatedAt = now
        };
}

