using E_POS.Domain.Common.Entities;
using E_POS.Domain.Modules.Shared.ReturnExchange.Constants;

namespace E_POS.Domain.Modules.Shared.ReturnExchange.Entities;

public class SalesExchangeEvent : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid SalesExchangeId { get; protected set; }
    public string EventType { get; protected set; } = string.Empty;
    public string? OldStatus { get; protected set; }
    public string? NewStatus { get; protected set; }
    public string? EventNotes { get; protected set; }
    public Guid? CreatedByTenantUserId { get; protected set; }

    public static SalesExchangeEvent Record(
        Guid id,
        Guid tenantId,
        Guid salesExchangeId,
        string eventType,
        string? oldStatus,
        string? newStatus,
        string? notes,
        Guid? tenantUserId,
        DateTimeOffset now) => new()
        {
            Id = id,
            TenantId = tenantId,
            SalesExchangeId = salesExchangeId,
            EventType = eventType.Trim().ToUpperInvariant(),
            OldStatus = oldStatus,
            NewStatus = newStatus,
            EventNotes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            CreatedByTenantUserId = tenantUserId,
            CreatedAt = now,
            UpdatedAt = now,
        };

    public static SalesExchangeEvent RecordCompleted(
        Guid id,
        Guid tenantId,
        Guid salesExchangeId,
        Guid tenantUserId,
        string? notes,
        DateTimeOffset now) =>
        Record(
            id,
            tenantId,
            salesExchangeId,
            SalesExchangeConstants.EventType.Completed,
            null,
            SalesExchangeConstants.Status.Completed,
            notes,
            tenantUserId,
            now);
}

