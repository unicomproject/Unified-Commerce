using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.POSOperations.Entities;

/// <summary>Lifecycle audit row for Park / Recall / Cancel / Expire.</summary>
public class PosOrderHoldEvent : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid HoldId { get; protected set; }
    public string EventType { get; protected set; } = string.Empty;
    public DateTimeOffset EventAt { get; protected set; }
    public Guid? EventByTenantUserId { get; protected set; }
    public Guid? OutletId { get; protected set; }
    public Guid? TillId { get; protected set; }
    public Guid? TillSessionId { get; protected set; }
    public Guid? PosDeviceId { get; protected set; }
    public string? HoldNumber { get; protected set; }
    public Guid? SalesOrderId { get; protected set; }
    public string? PreviousStatus { get; protected set; }
    public string? NewStatus { get; protected set; }
    public string? CorrelationId { get; protected set; }
    public string? Notes { get; protected set; }

    public static PosOrderHoldEvent Create(
        Guid id,
        Guid tenantId,
        Guid holdId,
        string eventType,
        DateTimeOffset eventAt,
        Guid? eventByTenantUserId,
        Guid? outletId,
        Guid? tillId,
        Guid? tillSessionId,
        Guid? posDeviceId,
        string? holdNumber,
        Guid? salesOrderId,
        string? previousStatus,
        string? newStatus,
        string? correlationId,
        string? notes = null)
    {
        return new PosOrderHoldEvent
        {
            Id = id,
            TenantId = tenantId,
            HoldId = holdId,
            EventType = eventType.Trim().ToUpperInvariant(),
            EventAt = eventAt,
            EventByTenantUserId = eventByTenantUserId,
            OutletId = outletId,
            TillId = tillId,
            TillSessionId = tillSessionId,
            PosDeviceId = posDeviceId,
            HoldNumber = holdNumber?.Trim(),
            SalesOrderId = salesOrderId,
            PreviousStatus = previousStatus,
            NewStatus = newStatus,
            CorrelationId = correlationId?.Trim(),
            Notes = notes?.Trim(),
            CreatedAt = eventAt,
            UpdatedAt = eventAt
        };
    }
}
